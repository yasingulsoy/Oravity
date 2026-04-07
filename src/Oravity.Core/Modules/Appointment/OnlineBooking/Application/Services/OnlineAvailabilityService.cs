using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Appointment.OnlineBooking.Application.Services;

public record TimeSlotDto(DateOnly Date, TimeOnly Start, TimeOnly End, bool IsAvailable = true);

public interface IOnlineAvailabilityService
{
    Task<List<TimeSlotDto>> GetAvailableSlots(
        long doctorId, long branchId, DateOnly date,
        CancellationToken ct = default);
}

/// <summary>
/// Online randevu slot motoru (SPEC §ONLİNE RANDEVU SİSTEMİ §3).
/// Adımlar:
///   1. DoctorOnlineBookingSettings → is_online_visible + max_advance_days
///   2. DoctorOnlineSchedule → gün bazlı çalışma saatleri
///   3. Slotları üret (slot_duration_minutes aralıklı)
///   4. Öğle arası varsa çıkar
///   5. Mevcut randevuları çıkar (çakışma kontrolü)
///   6. DoctorOnlineBlock aralıklarını çıkar
///   7. Geçmiş slotları çıkar (bugün ise)
///   8. max_advance_days sınırı
/// </summary>
public class OnlineAvailabilityService : IOnlineAvailabilityService
{
    private readonly AppDbContext _db;

    public OnlineAvailabilityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<TimeSlotDto>> GetAvailableSlots(
        long doctorId, long branchId, DateOnly date,
        CancellationToken ct = default)
    {
        // 1. Hekim ayarları
        var settings = await _db.DoctorOnlineBookingSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.DoctorId == doctorId && s.BranchId == branchId, ct);

        if (settings is null || !settings.IsOnlineVisible)
            return [];

        // max_advance_days kontrolü
        var maxDate = DateOnly.FromDateTime(DateTime.Today.AddDays(settings.MaxAdvanceDays));
        if (date > maxDate || date < DateOnly.FromDateTime(DateTime.Today))
            return [];

        // 2. Gün bazlı program — DayOfWeek: Mon=1, Sun=7 (SPEC FDI mapping)
        var dayOfWeek = date.DayOfWeek == System.DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;

        var schedule = await _db.DoctorOnlineSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.DoctorId == doctorId && s.BranchId == branchId &&
                     s.DayOfWeek == dayOfWeek, ct);

        if (schedule is null || !schedule.IsWorking)
            return [];

        // 3. Slotları üret
        var slots = GenerateSlots(
            date,
            schedule.StartTime,
            schedule.EndTime,
            schedule.BreakStart,
            schedule.BreakEnd,
            settings.SlotDurationMinutes);

        // 4. Mevcut randevuları çıkar
        var dateAsDateTime = date.ToDateTime(TimeOnly.MinValue);
        var existing = await _db.Appointments
            .AsNoTracking()
            .Where(a =>
                a.DoctorId == doctorId &&
                a.BranchId == branchId &&
                a.StartTime >= dateAsDateTime &&
                a.StartTime < dateAsDateTime.AddDays(1) &&
                a.StatusId != AppointmentStatus.WellKnownIds.Cancelled &&
                a.StatusId != AppointmentStatus.WellKnownIds.NoShow)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync(ct);

        foreach (var appt in existing)
        {
            var apptStart = TimeOnly.FromDateTime(appt.StartTime);
            var apptEnd   = TimeOnly.FromDateTime(appt.EndTime);
            slots.RemoveAll(s => s.Start < apptEnd && s.End > apptStart);
        }

        // 5. Bloke aralıklarını çıkar
        var blocks = await _db.DoctorOnlineBlocks
            .AsNoTracking()
            .Where(b =>
                b.DoctorId == doctorId &&
                b.BranchId == branchId &&
                b.StartDatetime.Date <= date.ToDateTime(TimeOnly.MinValue) &&
                b.EndDatetime.Date >= date.ToDateTime(TimeOnly.MinValue))
            .ToListAsync(ct);

        foreach (var block in blocks)
        {
            var blockStart = TimeOnly.FromDateTime(block.StartDatetime);
            var blockEnd   = TimeOnly.FromDateTime(block.EndDatetime);
            // Tüm günü kaplayan blok
            if (block.StartDatetime.Date < date.ToDateTime(TimeOnly.MinValue))
                blockStart = TimeOnly.MinValue;
            if (block.EndDatetime.Date > date.ToDateTime(TimeOnly.MinValue))
                blockEnd = new TimeOnly(23, 59);
            slots.RemoveAll(s => s.Start < blockEnd && s.End > blockStart);
        }

        // 6. Geçmiş slotları çıkar (bugün ise)
        if (date == DateOnly.FromDateTime(DateTime.Today))
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);
            slots.RemoveAll(s => s.Start <= now);
        }

        return slots;
    }

    private static List<TimeSlotDto> GenerateSlots(
        DateOnly date, TimeOnly start, TimeOnly end,
        TimeOnly? breakStart, TimeOnly? breakEnd, int duration)
    {
        var slots   = new List<TimeSlotDto>();
        var current = start;

        while (current.AddMinutes(duration) <= end)
        {
            // Öğle arası varsa atla
            if (breakStart.HasValue && breakEnd.HasValue &&
                current >= breakStart && current < breakEnd)
            {
                current = breakEnd.Value;
                continue;
            }

            slots.Add(new TimeSlotDto(date, current, current.AddMinutes(duration)));
            current = current.AddMinutes(duration);
        }

        return slots;
    }
}
