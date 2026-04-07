using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Filters;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Controllers;

/// <summary>
/// Hekim haftalık çalışma takvimi yönetimi.
/// Çakışma kuralı: aynı hekim aynı gün birden fazla şubede örtüşen saatlerde çalışamaz.
/// </summary>
[ApiController]
[Route("api/doctor-schedules")]
[Authorize]
[Produces("application/json")]
public class DoctorSchedulesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public DoctorSchedulesController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    /// <summary>Hekimin tüm şubelerdeki haftalık programını döner.</summary>
    [HttpGet("{doctorId:long}")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorScheduleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedules(long doctorId)
    {
        var items = await _db.DoctorSchedules
            .Where(s => s.DoctorId == doctorId && s.IsActive)
            .OrderBy(s => s.BranchId).ThenBy(s => s.DayOfWeek)
            .Select(s => new DoctorScheduleResponse(
                s.Id, s.DoctorId, s.BranchId, s.Branch.Name,
                s.DayOfWeek, s.IsWorking,
                s.StartTime.ToString("HH:mm"), s.EndTime.ToString("HH:mm"),
                s.BreakStart != null ? s.BreakStart.Value.ToString("HH:mm") : null,
                s.BreakEnd   != null ? s.BreakEnd.Value.ToString("HH:mm")   : null))
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>
    /// Yeni haftalık program kaydı oluşturur.
    /// Aynı hekim aynı gün başka şubede çakışan saat varsa 409 döner.
    /// </summary>
    [HttpPost]
    [RequirePermission("appointment:edit")]
    [ProducesResponseType(typeof(DoctorScheduleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSchedule([FromBody] UpsertDoctorScheduleRequest request)
    {
        var start = TimeOnly.Parse(request.StartTime);
        var end   = TimeOnly.Parse(request.EndTime);

        if (start >= end)
            return BadRequest("Başlangıç saati bitiş saatinden önce olmalıdır.");

        var conflict = await FindConflictAsync(
            request.DoctorId, request.BranchId, request.DayOfWeek,
            start, end, excludeScheduleId: null);

        if (conflict is not null)
            return Conflict(new
            {
                message = $"Hekim bu gün {conflict.BranchName} şubesinde " +
                          $"{conflict.WorkStart}-{conflict.WorkEnd} saatleri arasında çalışmaktadır.",
                conflictingBranch = conflict.BranchName,
                conflictStart = conflict.WorkStart,
                conflictEnd   = conflict.WorkEnd,
            });

        var existing = await _db.DoctorSchedules
            .FirstOrDefaultAsync(s => s.DoctorId == request.DoctorId
                                   && s.BranchId == request.BranchId
                                   && s.DayOfWeek == request.DayOfWeek);

        if (existing is not null)
        {
            existing.Update(request.IsWorking, start, end,
                request.BreakStart != null ? TimeOnly.Parse(request.BreakStart) : null,
                request.BreakEnd   != null ? TimeOnly.Parse(request.BreakEnd)   : null);
        }
        else
        {
            existing = DoctorSchedule.Create(request.DoctorId, request.BranchId, request.DayOfWeek);
            existing.Update(request.IsWorking, start, end,
                request.BreakStart != null ? TimeOnly.Parse(request.BreakStart) : null,
                request.BreakEnd   != null ? TimeOnly.Parse(request.BreakEnd)   : null);
            _db.DoctorSchedules.Add(existing);
        }

        await _db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, ToResponse(existing, request.BranchId));
    }

    /// <summary>
    /// Mevcut program kaydını günceller.
    /// Çakışma kontrolü yapılır.
    /// </summary>
    [HttpPut("{id:long}")]
    [RequirePermission("appointment:edit")]
    [ProducesResponseType(typeof(DoctorScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSchedule(long id, [FromBody] UpsertDoctorScheduleRequest request)
    {
        var schedule = await _db.DoctorSchedules.FindAsync(id);
        if (schedule is null) return NotFound();

        var start = TimeOnly.Parse(request.StartTime);
        var end   = TimeOnly.Parse(request.EndTime);

        if (start >= end)
            return BadRequest("Başlangıç saati bitiş saatinden önce olmalıdır.");

        var conflict = await FindConflictAsync(
            schedule.DoctorId, request.BranchId, request.DayOfWeek,
            start, end, excludeScheduleId: id);

        if (conflict is not null)
            return Conflict(new
            {
                message = $"Hekim bu gün {conflict.BranchName} şubesinde " +
                          $"{conflict.WorkStart}-{conflict.WorkEnd} saatleri arasında çalışmaktadır.",
                conflictingBranch = conflict.BranchName,
                conflictStart = conflict.WorkStart,
                conflictEnd   = conflict.WorkEnd,
            });

        schedule.Update(request.IsWorking, start, end,
            request.BreakStart != null ? TimeOnly.Parse(request.BreakStart) : null,
            request.BreakEnd   != null ? TimeOnly.Parse(request.BreakEnd)   : null);

        await _db.SaveChangesAsync();
        return Ok(ToResponse(schedule, schedule.BranchId));
    }

    // ─── Çakışma kontrolü ─────────────────────────────────────────────────────

    private async Task<ScheduleConflictInfo?> FindConflictAsync(
        long doctorId, long branchId, int dayOfWeek,
        TimeOnly start, TimeOnly end,
        long? excludeScheduleId)
    {
        var query = _db.DoctorSchedules
            .Where(s => s.DoctorId  == doctorId
                     && s.BranchId  != branchId      // farklı şube
                     && s.DayOfWeek == dayOfWeek
                     && s.IsActive
                     && s.IsWorking);

        if (excludeScheduleId.HasValue)
            query = query.Where(s => s.Id != excludeScheduleId.Value);

        var sameDay = await query
            .Select(s => new { s.Id, s.BranchId, BranchName = s.Branch.Name, s.StartTime, s.EndTime })
            .ToListAsync();

        foreach (var existing in sameDay)
        {
            // Zaman çakışması: start < existing.End && existing.Start < end
            if (start < existing.EndTime && existing.StartTime < end)
            {
                return new ScheduleConflictInfo(
                    existing.BranchName,
                    existing.StartTime.ToString("HH:mm"),
                    existing.EndTime.ToString("HH:mm"));
            }
        }

        return null;
    }

    /// <summary>
    /// Verilen hekim + gün kombinasyonu için çakışma olup olmadığını kontrol eder.
    /// Schedule kaydetmeden önce UI'dan sorgulanabilir.
    /// </summary>
    [HttpGet("conflict-check")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(ConflictCheckResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckConflict(
        [FromQuery] long doctorId,
        [FromQuery] long branchId,
        [FromQuery] int dayOfWeek,
        [FromQuery] string startTime,
        [FromQuery] string endTime,
        [FromQuery] long? excludeScheduleId = null)
    {
        if (!TimeOnly.TryParse(startTime, out var start) || !TimeOnly.TryParse(endTime, out var end))
            return BadRequest("Geçersiz saat formatı.");

        var conflict = await FindConflictAsync(doctorId, branchId, dayOfWeek, start, end, excludeScheduleId);

        return Ok(new ConflictCheckResponse(
            HasConflict: conflict is not null,
            ConflictingBranch: conflict?.BranchName,
            ConflictStart: conflict?.WorkStart,
            ConflictEnd: conflict?.WorkEnd));
    }

    private static DoctorScheduleResponse ToResponse(DoctorSchedule s, long branchId) =>
        new(s.Id, s.DoctorId, branchId, string.Empty,
            s.DayOfWeek, s.IsWorking,
            s.StartTime.ToString("HH:mm"), s.EndTime.ToString("HH:mm"),
            s.BreakStart?.ToString("HH:mm"), s.BreakEnd?.ToString("HH:mm"));
}

// ─── DTOs ──────────────────────────────────────────────────────────────────────

public record DoctorScheduleResponse(
    long    Id,
    long    DoctorId,
    long    BranchId,
    string  BranchName,
    int     DayOfWeek,
    bool    IsWorking,
    string  StartTime,
    string  EndTime,
    string? BreakStart,
    string? BreakEnd
);

public record UpsertDoctorScheduleRequest(
    long    DoctorId,
    long    BranchId,
    int     DayOfWeek,
    bool    IsWorking,
    string  StartTime,
    string  EndTime,
    string? BreakStart,
    string? BreakEnd
);

public record ConflictCheckResponse(
    bool    HasConflict,
    string? ConflictingBranch,
    string? ConflictStart,
    string? ConflictEnd
);

internal record ScheduleConflictInfo(string BranchName, string WorkStart, string WorkEnd);
