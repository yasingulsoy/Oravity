using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Visit.Application.Commands;

public record CheckOutVisitCommand(Guid VisitPublicId) : IRequest<VisitResponse>;

public class CheckOutVisitCommandHandler : IRequestHandler<CheckOutVisitCommand, VisitResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICalendarBroadcastService _broadcast;

    public CheckOutVisitCommandHandler(AppDbContext db, ITenantContext tenant, ICalendarBroadcastService broadcast)
    {
        _db        = db;
        _tenant    = tenant;
        _broadcast = broadcast;
    }

    public async Task<VisitResponse> Handle(CheckOutVisitCommand request, CancellationToken ct)
    {
        var visit = await _db.Visits
            .Include(v => v.Patient)
            .Include(v => v.Protocols)
            .FirstOrDefaultAsync(v => v.PublicId == request.VisitPublicId && !v.IsDeleted, ct)
            ?? throw new NotFoundException("Vizite bulunamadı.");

        visit.CheckOut();

        // Bağlı randevu varsa durumunu "Tamamlandı" yap
        if (visit.AppointmentId.HasValue)
        {
            var apt = await _db.Appointments.FindAsync([visit.AppointmentId.Value], ct);
            apt?.SetStatus(AppointmentStatus.WellKnownIds.Completed);
        }

        await _db.SaveChangesAsync(ct);

        var patientName = visit.Patient is { } vp2
            ? $"{vp2.FirstName} {vp2.LastName}".Trim()
            : "";

        await _broadcast.BroadcastVisitAsync(
            visit.BranchId,
            new VisitBroadcastDto(
                visit.PublicId,
                visit.BranchId,
                visit.PatientId,
                patientName,
                visit.IsWalkIn,
                (int)visit.Status),
            CalendarEventType.VisitCheckedOut, ct);

        var protocols = visit.Protocols.Select(p => new ProtocolSummaryResponse(
            p.PublicId, p.ProtocolNo,
            (int)p.ProtocolType, VisitLabels.ProtocolType((int)p.ProtocolType),
            (int)p.Status, VisitLabels.ProtocolStatus((int)p.Status),
            p.DoctorId, "", p.StartedAt, p.CompletedAt)).ToList();

        return new VisitResponse(
            visit.PublicId, visit.PatientId, patientName,
            visit.BranchId, visit.CheckInAt, visit.CheckOutAt, visit.IsWalkIn,
            (int)visit.Status, VisitLabels.VisitStatus((int)visit.Status),
            visit.Notes, protocols);
    }
}
