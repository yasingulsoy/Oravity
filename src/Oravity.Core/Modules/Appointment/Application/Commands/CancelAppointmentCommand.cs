using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Appointment.Application.Commands;

public record CancelAppointmentCommand(Guid PublicId, string? Reason = null) : IRequest;

public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICalendarBroadcastService _broadcast;

    public CancelAppointmentCommandHandler(
        AppDbContext db,
        ITenantContext tenant,
        ICalendarBroadcastService broadcast)
    {
        _db = db;
        _tenant = tenant;
        _broadcast = broadcast;
    }

    public async Task Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.PublicId == request.PublicId, cancellationToken)
            ?? throw new NotFoundException($"Randevu bulunamadı: {request.PublicId}");

        if (_tenant.IsBranchLevel && appointment.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu randevuya erişim yetkiniz bulunmuyor.");

        appointment.SetStatus(AppointmentStatus.WellKnownIds.Cancelled);
        if (!string.IsNullOrWhiteSpace(request.Reason))
            appointment.AddNote(request.Reason);
        await _db.SaveChangesAsync(cancellationToken);

        await _broadcast.BroadcastAsync(
            appointment.BranchId,
            AppointmentMappings.ToBroadcast(appointment),
            CalendarEventType.Cancelled,
            cancellationToken);
    }
}
