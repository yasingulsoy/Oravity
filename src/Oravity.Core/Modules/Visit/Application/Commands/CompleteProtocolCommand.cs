using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Core.Modules.Visit.Application.Queries;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using AppointmentEntity = Oravity.SharedKernel.Entities.Appointment;

namespace Oravity.Core.Modules.Visit.Application.Commands;

public record CompleteProtocolCommand(Guid ProtocolPublicId) : IRequest<ProtocolDetailResponse>;

public class CompleteProtocolCommandHandler : IRequestHandler<CompleteProtocolCommand, ProtocolDetailResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICalendarBroadcastService _broadcast;

    public CompleteProtocolCommandHandler(AppDbContext db, ITenantContext tenant, ICalendarBroadcastService broadcast)
    {
        _db        = db;
        _tenant    = tenant;
        _broadcast = broadcast;
    }

    public async Task<ProtocolDetailResponse> Handle(CompleteProtocolCommand request, CancellationToken ct)
    {
        var protocol = await _db.Protocols
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Visit)
            .FirstOrDefaultAsync(p => p.PublicId == request.ProtocolPublicId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Protokol bulunamadı.");

        var patientName = protocol.Patient is { } pp
            ? $"{pp.FirstName} {pp.LastName}".Trim()
            : "";

        // Sadece protokolü oluşturan hekim veya yetkili tamamlayabilir
        if (protocol.DoctorId != _tenant.UserId && !_tenant.IsCompanyAdmin && !_tenant.IsPlatformAdmin)
            throw new ForbiddenException("Bu protokolü tamamlama yetkiniz yok.");

        protocol.Complete();

        // Tüm protokoller tamamlandıysa visit'i auto-checkout yap
        var visit = protocol.Visit;
        AppointmentEntity? updatedApt = null;
        bool autoCheckedOut = false;

        if (visit is not null)
        {
            // protocol.Complete() henüz SaveChanges öncesi olduğu için in-memory kontrol yap
            var remainingOpen = visit.Protocols
                .Count(p => p.Status == ProtocolStatus.Open && !p.IsDeleted
                            && p.PublicId != protocol.PublicId);

            if (remainingOpen == 0)
            {
                // Son protokol tamamlandı — hasta otomatik çıkış
                visit.CheckOut();
                autoCheckedOut = true;

                if (visit.AppointmentId is { } aptId)
                {
                    var apt = await _db.Appointments.FindAsync([aptId], ct);
                    if (apt is not null)
                    {
                        apt.SetStatus(AppointmentStatus.WellKnownIds.Left);
                        updatedApt = apt;
                    }
                }
            }
            else if (visit.Appointment?.StatusId == AppointmentStatus.WellKnownIds.InRoom)
            {
                // Hâlâ açık protokol var ama hasta odadan çıkıyor olabilir — Arrived'a al
                var apt = await _db.Appointments.FindAsync([visit.AppointmentId!.Value], ct);
                if (apt is not null)
                {
                    apt.SetStatus(AppointmentStatus.WellKnownIds.Arrived);
                    updatedApt = apt;
                }
            }
        }

        await _db.SaveChangesAsync(ct);

        await _broadcast.BroadcastProtocolAsync(
            protocol.BranchId,
            new ProtocolBroadcastDto(
                protocol.PublicId,
                protocol.BranchId,
                protocol.VisitId,
                protocol.PatientId,
                patientName,
                protocol.DoctorId,
                protocol.Doctor?.FullName ?? "",
                protocol.ProtocolNo,
                (int)protocol.ProtocolType,
                (int)protocol.Status),
            CalendarEventType.ProtocolCompleted, ct);

        // Visit'i waiting list'ten kaldırmak için yayınla
        if (visit is not null)
        {
            var eventType = autoCheckedOut ? CalendarEventType.VisitCheckedOut : CalendarEventType.ProtocolUpdated;
            await _broadcast.BroadcastVisitAsync(
                visit.BranchId,
                new VisitBroadcastDto(visit.PublicId, visit.BranchId, visit.PatientId, patientName, visit.IsWalkIn, (int)visit.Status),
                eventType, ct);
        }

        // Randevu durumu değişimini broadcast et
        if (updatedApt is not null)
        {
            await _broadcast.BroadcastAsync(
                protocol.BranchId,
                AppointmentMappings.ToBroadcast(updatedApt),
                autoCheckedOut ? CalendarEventType.PatientLeaving : CalendarEventType.StatusChanged,
                ct);
        }

        return await new GetProtocolDetailQueryHandler(_db)
            .Handle(new GetProtocolDetailQuery(request.ProtocolPublicId), ct);
    }
}
