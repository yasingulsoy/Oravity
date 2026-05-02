using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Core.Modules.Visit.Application.Queries;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Visit.Application.Commands;

/// <summary>Hekim hastayı odaya çağırdığında protokolü başlatır (started_at = now).</summary>
public record StartProtocolCommand(Guid ProtocolPublicId) : IRequest<ProtocolDetailResponse>;

public class StartProtocolCommandHandler : IRequestHandler<StartProtocolCommand, ProtocolDetailResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICalendarBroadcastService _broadcast;

    public StartProtocolCommandHandler(AppDbContext db, ITenantContext tenant, ICalendarBroadcastService broadcast)
    {
        _db        = db;
        _tenant    = tenant;
        _broadcast = broadcast;
    }

    public async Task<ProtocolDetailResponse> Handle(StartProtocolCommand request, CancellationToken ct)
    {
        var protocol = await _db.Protocols
            .Include(p => p.Doctor)
            .Include(p => p.Patient)
            .Include(p => p.Visit)
            .FirstOrDefaultAsync(p => p.PublicId == request.ProtocolPublicId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Protokol bulunamadı.");

        protocol.Start();

        // Bağlı randevuyu "Odaya Alındı" yap
        Oravity.SharedKernel.Entities.Appointment? updatedApt = null;
        if (protocol.Visit.AppointmentId.HasValue)
        {
            updatedApt = await _db.Appointments.FindAsync([protocol.Visit.AppointmentId.Value], ct);
            updatedApt?.SetStatus(AppointmentStatus.WellKnownIds.InRoom);
        }

        await _db.SaveChangesAsync(ct);

        var patientName = protocol.Patient is { } p2
            ? $"{p2.FirstName} {p2.LastName}".Trim() : "";

        await _broadcast.BroadcastProtocolAsync(
            protocol.BranchId,
            new ProtocolBroadcastDto(
                protocol.PublicId, protocol.BranchId, protocol.VisitId,
                protocol.PatientId, patientName,
                protocol.DoctorId, protocol.Doctor.FullName,
                protocol.ProtocolNo, (int)protocol.ProtocolType, (int)protocol.Status),
            CalendarEventType.ProtocolUpdated, ct);

        // Randevu durumu (InRoom) değişimini calendar'a bildir
        if (updatedApt is not null)
        {
            await _broadcast.BroadcastAsync(
                protocol.BranchId,
                AppointmentMappings.ToBroadcast(updatedApt),
                CalendarEventType.StatusChanged,
                ct);
        }

        return await new GetProtocolDetailQueryHandler(_db)
            .Handle(new GetProtocolDetailQuery(request.ProtocolPublicId), ct);
    }
}
