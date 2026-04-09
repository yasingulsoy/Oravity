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
    int NewStatusId
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

        // Geçiş kontrolü uygulama katmanında yapılır (AllowedNextStatusIds JSON'u okunarak).
        appointment.SetStatus(request.NewStatusId);

        // "Geldi" statüsüne geçince otomatik Visit (check-in) oluştur — yoksa
        if (request.NewStatusId == AppointmentStatus.WellKnownIds.Arrived)
        {
            var alreadyCheckedIn = await _db.Visits
                .AnyAsync(v => v.AppointmentId == appointment.Id && !v.IsDeleted, cancellationToken);

            var companyId = appointment.Branch?.CompanyId ?? _tenant.CompanyId ?? 0;
            if (!alreadyCheckedIn && companyId > 0)
            {
                var visit = SharedKernel.Entities.Visit.Create(
                    branchId:      appointment.BranchId,
                    companyId:     companyId,
                    patientId:     appointment.PatientId!.Value,
                    appointmentId: appointment.Id,
                    isWalkIn:      false,
                    notes:         null,
                    createdBy:     _tenant.UserId);
                _db.Visits.Add(visit);
            }
        }

        // Outbox: AppointmentCompleted → hakediş hesaplama + anket planlaması
        if (request.NewStatusId == AppointmentStatus.WellKnownIds.Completed)
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

        var eventType = AppointmentMappings.StatusToEventType(request.NewStatusId);
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
