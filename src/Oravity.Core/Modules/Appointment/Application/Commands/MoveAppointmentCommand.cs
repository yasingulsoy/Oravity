using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Appointment.Application.Commands;

public record MoveAppointmentCommand(
    Guid PublicId,
    DateTime NewStartTime,
    DateTime NewEndTime,
    long? NewDoctorId,
    /// <summary>
    /// Client'ın okuduğu RowVersion değeri.
    /// Başka biri bu arada değiştirdiyse 409 Conflict döner.
    /// </summary>
    int ExpectedRowVersion
) : IRequest<AppointmentResponse>;

public class MoveAppointmentCommandHandler : IRequestHandler<MoveAppointmentCommand, AppointmentResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICalendarBroadcastService _broadcast;

    public MoveAppointmentCommandHandler(
        AppDbContext db,
        ITenantContext tenant,
        ICalendarBroadcastService broadcast)
    {
        _db = db;
        _tenant = tenant;
        _broadcast = broadcast;
    }

    public async Task<AppointmentResponse> Handle(
        MoveAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.PublicId == request.PublicId, cancellationToken)
            ?? throw new NotFoundException($"Randevu bulunamadı: {request.PublicId}");

        if (_tenant.IsBranchLevel && appointment.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu randevuya erişim yetkiniz bulunmuyor.");

        // ── Optimistic lock: erken kontrol ─────────────────────────────────
        if (appointment.RowVersion != request.ExpectedRowVersion)
            throw new SlotConflictException(
                "Randevu başka bir kullanıcı tarafından değiştirildi. " +
                "Sayfayı yenileyip tekrar deneyin.");

        var targetDoctorId = request.NewDoctorId ?? appointment.DoctorId;

        // ── Slot çakışma kontrolü (yeni slot için) ──────────────────────────
        var conflict = await _db.Appointments
            .AnyAsync(a =>
                a.Id        != appointment.Id &&
                a.DoctorId  == targetDoctorId &&
                a.BranchId  == appointment.BranchId &&
                a.StartTime <  request.NewEndTime.ToUniversalTime() &&
                a.EndTime   >  request.NewStartTime.ToUniversalTime() &&
                a.StatusId  != AppointmentStatus.WellKnownIds.Cancelled &&
                a.StatusId  != AppointmentStatus.WellKnownIds.NoShow,
                cancellationToken);

        if (conflict)
            throw new SlotConflictException("Hedef slot dolu. Lütfen başka bir zaman seçin.");

        appointment.MoveTo(
            DateTime.SpecifyKind(request.NewStartTime, DateTimeKind.Utc),
            DateTime.SpecifyKind(request.NewEndTime, DateTimeKind.Utc),
            request.NewDoctorId);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // EF Core concurrency token: başka biri bu arada kayıt yaptı
            throw new SlotConflictException(
                "Randevu kaydedilirken çakışma oluştu. Sayfayı yenileyip tekrar deneyin.");
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            throw new SlotConflictException("Hedef slot az önce başka biri tarafından alındı.");
        }

        await _broadcast.BroadcastAsync(
            appointment.BranchId,
            AppointmentMappings.ToBroadcast(appointment),
            CalendarEventType.Moved,
            cancellationToken);

        return AppointmentMappings.ToResponse(appointment);
    }
}
