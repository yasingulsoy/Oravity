using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Visit.Application.Commands;

public record ReassignAppointmentDoctorCommand(Guid VisitPublicId, long NewDoctorId) : IRequest<Unit>;

public class ReassignAppointmentDoctorCommandHandler
    : IRequestHandler<ReassignAppointmentDoctorCommand, Unit>
{
    private readonly AppDbContext              _db;
    private readonly ICalendarBroadcastService _broadcast;

    public ReassignAppointmentDoctorCommandHandler(AppDbContext db, ICalendarBroadcastService broadcast)
    {
        _db        = db;
        _broadcast = broadcast;
    }

    public async Task<Unit> Handle(ReassignAppointmentDoctorCommand request, CancellationToken ct)
    {
        var visit = await _db.Visits
            .Include(v => v.Protocols)
            .Include(v => v.Appointment)
            .FirstOrDefaultAsync(v => v.PublicId == request.VisitPublicId && !v.IsDeleted, ct)
            ?? throw new NotFoundException("Vizite bulunamadı.");

        if (visit.IsWalkIn || visit.Appointment is null)
            throw new InvalidOperationException("Walk-in hastalarda randevu hekimi değiştirilemez.");

        var apt = visit.Appointment;

        var terminalIds = new[] {
            AppointmentStatus.WellKnownIds.Left,
            AppointmentStatus.WellKnownIds.Cancelled,
            AppointmentStatus.WellKnownIds.NoShow,
        };
        if (terminalIds.Contains(apt.StatusId))
            throw new InvalidOperationException("Tamamlanmış veya iptal edilmiş randevuda hekim değiştirilemez.");

        var hasActiveProtocol = visit.Protocols.Any(p => p.Status != ProtocolStatus.Cancelled && !p.IsDeleted);
        if (hasActiveProtocol)
            throw new InvalidOperationException("Protokol açılmış randevuda hekim değiştirilemez. Önce protokolü iptal edin.");

        var newDoctor = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.NewDoctorId && u.IsActive, ct)
            ?? throw new NotFoundException("Hekim bulunamadı.");

        apt.ReassignDoctor(request.NewDoctorId);
        await _db.SaveChangesAsync(ct);

        await _broadcast.BroadcastAsync(
            apt.BranchId,
            AppointmentMappings.ToBroadcast(apt),
            CalendarEventType.Updated,
            ct);

        return Unit.Value;
    }
}
