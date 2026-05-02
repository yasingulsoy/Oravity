using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Filters;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Controllers;

/// <summary>
/// Hekim online randevu takvimi, ayarları, nöbet günleri ve blok aralıkları yönetimi.
/// </summary>
[ApiController]
[Route("api/doctor-online")]
[Authorize]
[Produces("application/json")]
public class DoctorOnlineController : ControllerBase
{
    private readonly AppDbContext  _db;
    private readonly ITenantContext _tenant;

    public DoctorOnlineController(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ONLINE PROGRAM (doctor_online_schedule)
    // ════════════════════════════════════════════════════════════════════════

    [HttpGet("schedule/{doctorPublicId:guid}")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(IReadOnlyList<OnlineScheduleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOnlineSchedule(Guid doctorPublicId, CancellationToken ct)
    {
        var items = await _db.DoctorOnlineSchedules
            .Where(s => s.Doctor.PublicId == doctorPublicId)
            .OrderBy(s => s.BranchId).ThenBy(s => s.DayOfWeek)
            .Select(s => new OnlineScheduleResponse(
                s.Id,
                s.Doctor.PublicId,
                s.Branch.PublicId,
                s.Branch.Name,
                s.DayOfWeek, s.IsWorking,
                s.StartTime.ToString("HH:mm"), s.EndTime.ToString("HH:mm"),
                s.BreakStart != null ? s.BreakStart.Value.ToString("HH:mm") : null,
                s.BreakEnd   != null ? s.BreakEnd.Value.ToString("HH:mm")   : null))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("schedule")]
    [RequirePermission("appointment:edit")]
    [ProducesResponseType(typeof(OnlineScheduleResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> UpsertOnlineSchedule([FromBody] UpsertOnlineScheduleRequest req, CancellationToken ct)
    {
        var doctor = await _db.Users
            .Where(u => u.PublicId == req.DoctorPublicId)
            .Select(u => new { u.Id, u.PublicId })
            .FirstOrDefaultAsync(ct);
        if (doctor is null) return NotFound("Hekim bulunamadı.");

        var branch = await _db.Branches
            .Where(b => b.PublicId == req.BranchPublicId)
            .Select(b => new { b.Id, b.Name, b.PublicId })
            .FirstOrDefaultAsync(ct);
        if (branch is null) return NotFound("Şube bulunamadı.");

        var start = TimeOnly.Parse(req.StartTime);
        var end   = TimeOnly.Parse(req.EndTime);

        if (start >= end)
            return BadRequest("Başlangıç saati bitiş saatinden önce olmalıdır.");

        var existing = await _db.DoctorOnlineSchedules
            .FirstOrDefaultAsync(s => s.DoctorId == doctor.Id
                                   && s.BranchId == branch.Id
                                   && s.DayOfWeek == req.DayOfWeek, ct);

        if (existing is not null)
        {
            existing.Update(req.IsWorking, start, end,
                req.BreakStart != null ? TimeOnly.Parse(req.BreakStart) : null,
                req.BreakEnd   != null ? TimeOnly.Parse(req.BreakEnd)   : null);
        }
        else
        {
            existing = DoctorOnlineSchedule.Create(doctor.Id, branch.Id, req.DayOfWeek);
            existing.Update(req.IsWorking, start, end,
                req.BreakStart != null ? TimeOnly.Parse(req.BreakStart) : null,
                req.BreakEnd   != null ? TimeOnly.Parse(req.BreakEnd)   : null);
            _db.DoctorOnlineSchedules.Add(existing);
        }

        await _db.SaveChangesAsync(ct);

        return StatusCode(201, new OnlineScheduleResponse(
            existing.Id, doctor.PublicId, branch.PublicId, branch.Name,
            existing.DayOfWeek, existing.IsWorking,
            existing.StartTime.ToString("HH:mm"), existing.EndTime.ToString("HH:mm"),
            existing.BreakStart?.ToString("HH:mm"), existing.BreakEnd?.ToString("HH:mm")));
    }

    // ════════════════════════════════════════════════════════════════════════
    // ONLİNE RANDEVU AYARLARI (doctor_online_booking_settings)
    // ════════════════════════════════════════════════════════════════════════

    [HttpGet("booking-settings/{doctorPublicId:guid}")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(IReadOnlyList<OnlineBookingSettingsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBookingSettings(Guid doctorPublicId, [FromQuery] Guid? branchPublicId, CancellationToken ct)
    {
        var q = _db.DoctorOnlineBookingSettings.AsNoTracking()
            .Where(s => s.Doctor.PublicId == doctorPublicId);

        if (branchPublicId.HasValue)
            q = q.Where(s => s.Branch.PublicId == branchPublicId.Value);

        var items = await q
            .Select(s => new OnlineBookingSettingsResponse(
                s.Id, s.Doctor.PublicId, s.Branch.PublicId, s.Branch.Name,
                s.IsOnlineVisible, s.SlotDurationMinutes, s.AutoApprove,
                s.MaxAdvanceDays, s.BookingNote, s.PatientTypeFilter))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("booking-settings")]
    [RequirePermission("appointment:edit")]
    [ProducesResponseType(typeof(OnlineBookingSettingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertBookingSettings([FromBody] UpsertOnlineBookingSettingsRequest req, CancellationToken ct)
    {
        var doctor = await _db.Users
            .Where(u => u.PublicId == req.DoctorPublicId)
            .Select(u => new { u.Id, u.PublicId })
            .FirstOrDefaultAsync(ct);
        if (doctor is null) return NotFound("Hekim bulunamadı.");

        var branch = await _db.Branches
            .Where(b => b.PublicId == req.BranchPublicId)
            .Select(b => new { b.Id, b.Name, b.PublicId })
            .FirstOrDefaultAsync(ct);
        if (branch is null) return NotFound("Şube bulunamadı.");

        var existing = await _db.DoctorOnlineBookingSettings
            .FirstOrDefaultAsync(s => s.DoctorId == doctor.Id && s.BranchId == branch.Id, ct);

        if (existing is not null)
        {
            existing.Update(req.IsOnlineVisible, req.SlotDurationMinutes, req.AutoApprove,
                req.MaxAdvanceDays, req.BookingNote, req.PatientTypeFilter, null);
        }
        else
        {
            existing = DoctorOnlineBookingSettings.Create(doctor.Id, branch.Id);
            existing.Update(req.IsOnlineVisible, req.SlotDurationMinutes, req.AutoApprove,
                req.MaxAdvanceDays, req.BookingNote, req.PatientTypeFilter, null);
            _db.DoctorOnlineBookingSettings.Add(existing);
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new OnlineBookingSettingsResponse(
            existing.Id, doctor.PublicId, branch.PublicId, branch.Name,
            existing.IsOnlineVisible, existing.SlotDurationMinutes, existing.AutoApprove,
            existing.MaxAdvanceDays, existing.BookingNote, existing.PatientTypeFilter));
    }

    // ════════════════════════════════════════════════════════════════════════
    // NÖBET AYARLARI (doctor_on_call_settings)
    // ════════════════════════════════════════════════════════════════════════

    [HttpGet("on-call/{doctorPublicId:guid}")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(IReadOnlyList<OnCallSettingsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOnCallSettings(Guid doctorPublicId, [FromQuery] Guid? branchPublicId, CancellationToken ct)
    {
        var q = _db.DoctorOnCallSettings.AsNoTracking()
            .Where(s => s.Doctor.PublicId == doctorPublicId && s.IsActive);

        if (branchPublicId.HasValue)
            q = q.Where(s => s.Branch.PublicId == branchPublicId.Value);

        var items = await q
            .Select(s => new OnCallSettingsResponse(
                s.Id, s.Doctor.PublicId, s.Branch.PublicId, s.Branch.Name,
                s.Monday, s.Tuesday, s.Wednesday, s.Thursday, s.Friday, s.Saturday, s.Sunday,
                (int)s.PeriodType, s.PeriodStart, s.PeriodEnd))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("on-call")]
    [RequirePermission("appointment:edit")]
    [ProducesResponseType(typeof(OnCallSettingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertOnCallSettings([FromBody] UpsertOnCallSettingsRequest req, CancellationToken ct)
    {
        var doctor = await _db.Users
            .Where(u => u.PublicId == req.DoctorPublicId)
            .Select(u => new { u.Id, u.PublicId })
            .FirstOrDefaultAsync(ct);
        if (doctor is null) return NotFound("Hekim bulunamadı.");

        var branch = await _db.Branches
            .Where(b => b.PublicId == req.BranchPublicId)
            .Select(b => new { b.Id, b.Name, b.PublicId })
            .FirstOrDefaultAsync(ct);
        if (branch is null) return NotFound("Şube bulunamadı.");

        var existing = await _db.DoctorOnCallSettings
            .FirstOrDefaultAsync(s => s.DoctorId == doctor.Id && s.BranchId == branch.Id, ct);

        if (existing is not null)
        {
            existing.Update(
                req.Monday, req.Tuesday, req.Wednesday, req.Thursday,
                req.Friday, req.Saturday, req.Sunday,
                (OnCallPeriodType)req.PeriodType, req.PeriodStart, req.PeriodEnd);
        }
        else
        {
            existing = DoctorOnCallSettings.Create(doctor.Id, branch.Id);
            existing.Update(
                req.Monday, req.Tuesday, req.Wednesday, req.Thursday,
                req.Friday, req.Saturday, req.Sunday,
                (OnCallPeriodType)req.PeriodType, req.PeriodStart, req.PeriodEnd);
            _db.DoctorOnCallSettings.Add(existing);
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new OnCallSettingsResponse(
            existing.Id, doctor.PublicId, branch.PublicId, branch.Name,
            existing.Monday, existing.Tuesday, existing.Wednesday, existing.Thursday,
            existing.Friday, existing.Saturday, existing.Sunday,
            (int)existing.PeriodType, existing.PeriodStart, existing.PeriodEnd));
    }

    // ════════════════════════════════════════════════════════════════════════
    // ONLİNE BLOKLAR (doctor_online_blocks)
    // ════════════════════════════════════════════════════════════════════════

    [HttpGet("blocks/{doctorPublicId:guid}")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(IReadOnlyList<OnlineBlockResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBlocks(Guid doctorPublicId, [FromQuery] Guid? branchPublicId, CancellationToken ct)
    {
        var q = _db.DoctorOnlineBlocks.AsNoTracking()
            .Where(b => b.Doctor.PublicId == doctorPublicId);

        if (branchPublicId.HasValue)
            q = q.Where(b => b.Branch.PublicId == branchPublicId.Value);

        var items = await q
            .OrderBy(b => b.StartDatetime)
            .Select(b => new OnlineBlockResponse(
                b.Id, b.Doctor.PublicId, b.Branch.PublicId, b.Branch.Name,
                b.StartDatetime, b.EndDatetime, b.Reason))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("blocks")]
    [RequirePermission("appointment:edit")]
    [ProducesResponseType(typeof(OnlineBlockResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBlock([FromBody] CreateOnlineBlockRequest req, CancellationToken ct)
    {
        var doctor = await _db.Users
            .Where(u => u.PublicId == req.DoctorPublicId)
            .Select(u => new { u.Id, u.PublicId })
            .FirstOrDefaultAsync(ct);
        if (doctor is null) return NotFound("Hekim bulunamadı.");

        var branch = await _db.Branches
            .Where(b => b.PublicId == req.BranchPublicId)
            .Select(b => new { b.Id, b.Name, b.PublicId })
            .FirstOrDefaultAsync(ct);
        if (branch is null) return NotFound("Şube bulunamadı.");

        if (req.EndDatetime <= req.StartDatetime)
            return BadRequest("Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");

        var block = DoctorOnlineBlock.Create(
            doctor.Id, branch.Id,
            req.StartDatetime.ToUniversalTime(),
            req.EndDatetime.ToUniversalTime(),
            _tenant.UserId, req.Reason);

        _db.DoctorOnlineBlocks.Add(block);
        await _db.SaveChangesAsync(ct);

        return StatusCode(201, new OnlineBlockResponse(
            block.Id, doctor.PublicId, branch.PublicId, branch.Name,
            block.StartDatetime, block.EndDatetime, block.Reason));
    }

    [HttpDelete("blocks/{id:long}")]
    [RequirePermission("appointment:edit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBlock(long id, CancellationToken ct)
    {
        var block = await _db.DoctorOnlineBlocks.FindAsync(new object[] { id }, ct);
        if (block is null) return NotFound();
        _db.DoctorOnlineBlocks.Remove(block);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record OnlineScheduleResponse(
    long    Id,
    Guid    DoctorPublicId,
    Guid    BranchPublicId,
    string  BranchName,
    int     DayOfWeek,
    bool    IsWorking,
    string  StartTime,
    string  EndTime,
    string? BreakStart,
    string? BreakEnd
);

public record UpsertOnlineScheduleRequest(
    Guid    DoctorPublicId,
    Guid    BranchPublicId,
    int     DayOfWeek,
    bool    IsWorking,
    string  StartTime,
    string  EndTime,
    string? BreakStart,
    string? BreakEnd
);

public record OnlineBookingSettingsResponse(
    long    Id,
    Guid    DoctorPublicId,
    Guid    BranchPublicId,
    string  BranchName,
    bool    IsOnlineVisible,
    int     SlotDurationMinutes,
    bool    AutoApprove,
    int     MaxAdvanceDays,
    string? BookingNote,
    int     PatientTypeFilter
);

public record UpsertOnlineBookingSettingsRequest(
    Guid    DoctorPublicId,
    Guid    BranchPublicId,
    bool    IsOnlineVisible,
    int     SlotDurationMinutes,
    bool    AutoApprove,
    int     MaxAdvanceDays,
    string? BookingNote,
    int     PatientTypeFilter
);

public record OnCallSettingsResponse(
    long      Id,
    Guid      DoctorPublicId,
    Guid      BranchPublicId,
    string    BranchName,
    bool      Monday,
    bool      Tuesday,
    bool      Wednesday,
    bool      Thursday,
    bool      Friday,
    bool      Saturday,
    bool      Sunday,
    int       PeriodType,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd
);

public record UpsertOnCallSettingsRequest(
    Guid      DoctorPublicId,
    Guid      BranchPublicId,
    bool      Monday,
    bool      Tuesday,
    bool      Wednesday,
    bool      Thursday,
    bool      Friday,
    bool      Saturday,
    bool      Sunday,
    int       PeriodType,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd
);

public record OnlineBlockResponse(
    long      Id,
    Guid      DoctorPublicId,
    Guid      BranchPublicId,
    string    BranchName,
    DateTime  StartDatetime,
    DateTime  EndDatetime,
    string?   Reason
);

public record CreateOnlineBlockRequest(
    Guid      DoctorPublicId,
    Guid      BranchPublicId,
    DateTime  StartDatetime,
    DateTime  EndDatetime,
    string?   Reason
);
