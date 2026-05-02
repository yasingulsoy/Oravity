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
    [HttpGet("{doctorPublicId:guid}")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorScheduleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedules(Guid doctorPublicId, CancellationToken ct)
    {
        var items = await _db.DoctorSchedules
            .Where(s => s.Doctor.PublicId == doctorPublicId && s.IsActive)
            .OrderBy(s => s.BranchId).ThenBy(s => s.DayOfWeek)
            .Select(s => new DoctorScheduleResponse(
                s.Id,
                s.Doctor.PublicId,
                s.Branch.PublicId,
                s.Branch.Name,
                s.DayOfWeek, s.IsWorking,
                s.StartTime.ToString("HH:mm"), s.EndTime.ToString("HH:mm"),
                s.BreakStart != null ? s.BreakStart.Value.ToString("HH:mm") : null,
                s.BreakEnd   != null ? s.BreakEnd.Value.ToString("HH:mm")   : null,
                s.BreakLabel))
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>
    /// Yeni haftalık program kaydı oluşturur (upsert).
    /// Aynı hekim aynı gün başka şubede çakışan saat varsa 409 döner.
    /// </summary>
    [HttpPost]
    [RequirePermission("appointment:edit")]
    [ProducesResponseType(typeof(DoctorScheduleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpsertSchedule([FromBody] UpsertDoctorScheduleRequest request, CancellationToken ct)
    {
        var doctor = await _db.Users
            .Where(u => u.PublicId == request.DoctorPublicId)
            .Select(u => new { u.Id, u.PublicId })
            .FirstOrDefaultAsync(ct);
        if (doctor is null) return NotFound("Hekim bulunamadı.");

        var branch = await _db.Branches
            .Where(b => b.PublicId == request.BranchPublicId && !b.IsDeleted)
            .Select(b => new { b.Id, b.Name, b.PublicId })
            .FirstOrDefaultAsync(ct);
        if (branch is null) return NotFound("Şube bulunamadı.");

        var start = TimeOnly.Parse(request.StartTime);
        var end   = TimeOnly.Parse(request.EndTime);

        if (start >= end)
            return BadRequest("Başlangıç saati bitiş saatinden önce olmalıdır.");

        var conflict = await FindConflictAsync(doctor.Id, branch.Id, request.DayOfWeek, start, end, null, ct);
        if (conflict is not null)
            return Conflict(new
            {
                message          = $"Hekim bu gün {conflict.BranchName} şubesinde {conflict.WorkStart}-{conflict.WorkEnd} saatleri arasında çalışmaktadır.",
                conflictingBranch = conflict.BranchName,
                conflictStart    = conflict.WorkStart,
                conflictEnd      = conflict.WorkEnd,
            });

        var existing = await _db.DoctorSchedules
            .FirstOrDefaultAsync(s => s.DoctorId == doctor.Id
                                   && s.BranchId == branch.Id
                                   && s.DayOfWeek == request.DayOfWeek, ct);

        if (existing is not null)
        {
            existing.Update(request.IsWorking, start, end,
                request.BreakStart != null ? TimeOnly.Parse(request.BreakStart) : null,
                request.BreakEnd   != null ? TimeOnly.Parse(request.BreakEnd)   : null,
                request.BreakLabel);
        }
        else
        {
            existing = DoctorSchedule.Create(doctor.Id, branch.Id, request.DayOfWeek);
            existing.Update(request.IsWorking, start, end,
                request.BreakStart != null ? TimeOnly.Parse(request.BreakStart) : null,
                request.BreakEnd   != null ? TimeOnly.Parse(request.BreakEnd)   : null,
                request.BreakLabel);
            _db.DoctorSchedules.Add(existing);
        }

        await _db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, new DoctorScheduleResponse(
            existing.Id, doctor.PublicId, branch.PublicId, branch.Name,
            existing.DayOfWeek, existing.IsWorking,
            existing.StartTime.ToString("HH:mm"), existing.EndTime.ToString("HH:mm"),
            existing.BreakStart?.ToString("HH:mm"), existing.BreakEnd?.ToString("HH:mm"), existing.BreakLabel));
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
    public async Task<IActionResult> UpdateSchedule(long id, [FromBody] UpdateDoctorScheduleRequest request, CancellationToken ct)
    {
        var schedule = await _db.DoctorSchedules
            .Include(s => s.Doctor)
            .Include(s => s.Branch)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (schedule is null) return NotFound();

        var start = TimeOnly.Parse(request.StartTime);
        var end   = TimeOnly.Parse(request.EndTime);

        if (start >= end)
            return BadRequest("Başlangıç saati bitiş saatinden önce olmalıdır.");

        var conflict = await FindConflictAsync(schedule.DoctorId, schedule.BranchId, schedule.DayOfWeek, start, end, id, ct);
        if (conflict is not null)
            return Conflict(new
            {
                message          = $"Hekim bu gün {conflict.BranchName} şubesinde {conflict.WorkStart}-{conflict.WorkEnd} saatleri arasında çalışmaktadır.",
                conflictingBranch = conflict.BranchName,
                conflictStart    = conflict.WorkStart,
                conflictEnd      = conflict.WorkEnd,
            });

        schedule.Update(request.IsWorking, start, end,
            request.BreakStart != null ? TimeOnly.Parse(request.BreakStart) : null,
            request.BreakEnd   != null ? TimeOnly.Parse(request.BreakEnd)   : null,
            request.BreakLabel);

        await _db.SaveChangesAsync(ct);

        return Ok(new DoctorScheduleResponse(
            schedule.Id, schedule.Doctor.PublicId, schedule.Branch.PublicId, schedule.Branch.Name,
            schedule.DayOfWeek, schedule.IsWorking,
            schedule.StartTime.ToString("HH:mm"), schedule.EndTime.ToString("HH:mm"),
            schedule.BreakStart?.ToString("HH:mm"), schedule.BreakEnd?.ToString("HH:mm"), schedule.BreakLabel));
    }

    // ─── Çakışma kontrolü ─────────────────────────────────────────────────────

    private async Task<ScheduleConflictInfo?> FindConflictAsync(
        long doctorId, long branchId, int dayOfWeek,
        TimeOnly start, TimeOnly end,
        long? excludeScheduleId,
        CancellationToken ct)
    {
        var query = _db.DoctorSchedules
            .Where(s => s.DoctorId  == doctorId
                     && s.BranchId  != branchId
                     && s.DayOfWeek == dayOfWeek
                     && s.IsActive
                     && s.IsWorking);

        if (excludeScheduleId.HasValue)
            query = query.Where(s => s.Id != excludeScheduleId.Value);

        var sameDay = await query
            .Select(s => new { s.Id, s.BranchId, BranchName = s.Branch.Name, s.StartTime, s.EndTime })
            .ToListAsync(ct);

        foreach (var existing in sameDay)
        {
            if (start < existing.EndTime && existing.StartTime < end)
                return new ScheduleConflictInfo(existing.BranchName, existing.StartTime.ToString("HH:mm"), existing.EndTime.ToString("HH:mm"));
        }

        return null;
    }

    /// <summary>Verilen hekim+gün için çakışma kontrolü (UI'dan kullanılır).</summary>
    [HttpGet("conflict-check")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(ConflictCheckResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckConflict(
        [FromQuery] Guid   doctorPublicId,
        [FromQuery] Guid   branchPublicId,
        [FromQuery] int    dayOfWeek,
        [FromQuery] string startTime,
        [FromQuery] string endTime,
        [FromQuery] long?  excludeScheduleId,
        CancellationToken ct)
    {
        if (!TimeOnly.TryParse(startTime, out var start) || !TimeOnly.TryParse(endTime, out var end))
            return BadRequest("Geçersiz saat formatı.");

        var doctorId = await _db.Users.Where(u => u.PublicId == doctorPublicId).Select(u => (long?)u.Id).FirstOrDefaultAsync(ct);
        var branchId = await _db.Branches.Where(b => b.PublicId == branchPublicId).Select(b => (long?)b.Id).FirstOrDefaultAsync(ct);

        if (doctorId is null || branchId is null) return NotFound();

        var conflict = await FindConflictAsync(doctorId.Value, branchId.Value, dayOfWeek, start, end, excludeScheduleId, ct);

        return Ok(new ConflictCheckResponse(
            HasConflict: conflict is not null,
            ConflictingBranch: conflict?.BranchName,
            ConflictStart: conflict?.WorkStart,
            ConflictEnd: conflict?.WorkEnd));
    }
}

// ─── DTOs ──────────────────────────────────────────────────────────────────────

public record DoctorScheduleResponse(
    long    Id,
    Guid    DoctorPublicId,
    Guid    BranchPublicId,
    string  BranchName,
    int     DayOfWeek,
    bool    IsWorking,
    string  StartTime,
    string  EndTime,
    string? BreakStart,
    string? BreakEnd,
    string? BreakLabel
);

public record UpsertDoctorScheduleRequest(
    Guid    DoctorPublicId,
    Guid    BranchPublicId,
    int     DayOfWeek,
    bool    IsWorking,
    string  StartTime,
    string  EndTime,
    string? BreakStart,
    string? BreakEnd,
    string? BreakLabel
);

public record UpdateDoctorScheduleRequest(
    bool    IsWorking,
    string  StartTime,
    string  EndTime,
    string? BreakStart,
    string? BreakEnd,
    string? BreakLabel
);

public record ConflictCheckResponse(
    bool    HasConflict,
    string? ConflictingBranch,
    string? ConflictStart,
    string? ConflictEnd
);

internal record ScheduleConflictInfo(string BranchName, string WorkStart, string WorkEnd);
