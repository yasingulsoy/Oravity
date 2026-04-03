using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Appointment.Application.Commands;

public record UpdateAppointmentStatusCommand(
    Guid PublicId,
    AppointmentStatus NewStatus
) : IRequest<AppointmentResponse>;

public class UpdateAppointmentStatusCommandHandler
    : IRequestHandler<UpdateAppointmentStatusCommand, AppointmentResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICalendarBroadcastService _broadcast;

    public UpdateAppointmentStatusCommandHandler(
        AppDbContext db,
        ITenantContext tenant,
        ICalendarBroadcastService broadcast)
    {
        _db = db;
        _tenant = tenant;
        _broadcast = broadcast;
    }

    public async Task<AppointmentResponse> Handle(
        UpdateAppointmentStatusCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Branch)
            .FirstOrDefaultAsync(a => a.PublicId == request.PublicId, cancellationToken)
            ?? throw new NotFoundException($"Randevu bulunamadı: {request.PublicId}");

        EnsureTenantAccess(appointment);

        // Geçiş kontrolü entity içinde yapılır (InvalidOperationException → 400 dönmeli)
        appointment.UpdateStatus(request.NewStatus);

        // Outbox: AppointmentCompleted → hakediş hesaplama + anket planlaması
        if (request.NewStatus == AppointmentStatus.Completed)
        {
            var payload = JsonSerializer.Serialize(new
            {
                AppointmentId = appointment.Id,
                appointment.PublicId,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.BranchId,
                CompanyId   = appointment.Branch?.CompanyId ?? 0L,
                CompletedAt = DateTime.UtcNow
            });
            _db.OutboxMessages.Add(OutboxMessage.Create("AppointmentCompleted", payload));
        }

        await _db.SaveChangesAsync(cancellationToken);

        var eventType = AppointmentMappings.StatusToEventType(request.NewStatus);
        await _broadcast.BroadcastAsync(
            appointment.BranchId,
            AppointmentMappings.ToBroadcast(appointment),
            eventType,
            cancellationToken);

        return AppointmentMappings.ToResponse(appointment);
    }

    private void EnsureTenantAccess(SharedKernel.Entities.Appointment appt)
    {
        if (_tenant.IsPlatformAdmin) return;
        if (_tenant.IsBranchLevel && appt.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu randevuya erişim yetkiniz bulunmuyor.");
    }
}
