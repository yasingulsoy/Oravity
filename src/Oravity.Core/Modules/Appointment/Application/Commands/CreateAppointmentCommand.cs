using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using AppointmentEntity = Oravity.SharedKernel.Entities.Appointment;

namespace Oravity.Core.Modules.Appointment.Application.Commands;

public record CreateAppointmentCommand(
    long PatientId,
    long DoctorId,
    long? ExplicitBranchId,
    int? AppointmentTypeId,
    DateTime StartTime,
    DateTime EndTime,
    string? Notes,
    bool IsUrgent = false,
    bool IsEarlierRequest = false
) : IRequest<AppointmentResponse>;

public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, AppointmentResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICalendarBroadcastService _broadcast;

    public CreateAppointmentCommandHandler(
        AppDbContext db,
        ITenantContext tenant,
        ICalendarBroadcastService broadcast)
    {
        _db = db;
        _tenant = tenant;
        _broadcast = broadcast;
    }

    public async Task<AppointmentResponse> Handle(
        CreateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var branchId = request.ExplicitBranchId ?? _tenant.BranchId
            ?? throw new ForbiddenException("Randevu kaydı için şube bağlamı gereklidir.");

        // ── Geçmiş zaman kontrolü ──────────────────────────────────────────
        if (request.StartTime.ToUniversalTime() < DateTime.UtcNow)
            throw new InvalidOperationException("Geçmiş bir saate randevu oluşturulamaz.");

        // ── Katman 1: Slot çakışma kontrolü (uygulama seviyesi) ────────────
        var conflict = await _db.Appointments
            .AnyAsync(a =>
                a.DoctorId  == request.DoctorId &&
                a.BranchId  == branchId &&
                a.StartTime <  request.EndTime.ToUniversalTime() &&
                a.EndTime   >  request.StartTime.ToUniversalTime() &&
                a.StatusId  != AppointmentStatus.WellKnownIds.Cancelled &&
                a.StatusId  != AppointmentStatus.WellKnownIds.NoShow,
                cancellationToken);

        if (conflict)
            throw new SlotConflictException("Bu slot dolu. Lütfen başka bir zaman seçin.");

        var appointment = AppointmentEntity.Create(
            branchId:          branchId,
            patientId:         request.PatientId,
            doctorId:          request.DoctorId,
            statusId:          AppointmentStatus.WellKnownIds.Planned,
            startTime:         request.StartTime,
            endTime:           request.EndTime,
            appointmentTypeId: request.AppointmentTypeId,
            notes:             request.Notes,
            isUrgent:          request.IsUrgent,
            isEarlierRequest:  request.IsEarlierRequest);

        _db.Appointments.Add(appointment);

        // Outbox: AppointmentCreated event
        var payload = JsonSerializer.Serialize(new
        {
            appointment.PublicId,
            appointment.BranchId,
            appointment.PatientId,
            appointment.DoctorId,
            appointment.StartTime,
            appointment.EndTime
        });
        _db.OutboxMessages.Add(OutboxMessage.Create("AppointmentCreated", payload));

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            // Katman 2: Unique index race condition'ı yakaladı
            throw new SlotConflictException("Bu slot az önce başka biri tarafından alındı.");
        }

        // SignalR broadcast
        await _broadcast.BroadcastAsync(
            branchId,
            AppointmentMappings.ToBroadcast(appointment),
            CalendarEventType.Created,
            cancellationToken);

        return AppointmentMappings.ToResponse(appointment);
    }
}
