using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Filters;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Controllers;

[ApiController]
[Route("api/doctor-special-days")]
[Authorize]
[Produces("application/json")]
public class DoctorSpecialDaysController : ControllerBase
{
    private readonly AppDbContext _db;

    public DoctorSpecialDaysController(AppDbContext db) => _db = db;

    [HttpGet]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(IReadOnlyList<SpecialDayResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpecialDays(
        [FromQuery] Guid doctorPublicId,
        [FromQuery] Guid? branchPublicId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken ct)
    {
        var q = _db.DoctorSpecialDays.AsNoTracking()
            .Where(d => d.Doctor.PublicId == doctorPublicId && d.IsActive);

        if (branchPublicId.HasValue)
            q = q.Where(d => d.Branch.PublicId == branchPublicId.Value);
        if (fromDate.HasValue)
            q = q.Where(d => d.SpecificDate >= fromDate.Value);
        if (toDate.HasValue)
            q = q.Where(d => d.SpecificDate <= toDate.Value);

        var items = await q
            .OrderBy(d => d.SpecificDate)
            .Select(d => new SpecialDayResponse(
                d.Id,
                d.Doctor.PublicId,
                d.Branch.PublicId,
                d.Branch.Name,
                d.SpecificDate,
                (int)d.Type,
                d.StartTime != null ? d.StartTime.Value.ToString("HH:mm") : null,
                d.EndTime   != null ? d.EndTime.Value.ToString("HH:mm")   : null,
                d.Reason))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost]
    [RequirePermission("appointment:edit")]
    [ProducesResponseType(typeof(SpecialDayResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] UpsertSpecialDayRequest req, CancellationToken ct)
    {
        var doctorId = await _db.Users
            .Where(u => u.PublicId == req.DoctorPublicId)
            .Select(u => (long?)u.Id)
            .FirstOrDefaultAsync(ct);
        if (doctorId is null) return NotFound("Hekim bulunamadı.");

        var branch = await _db.Branches
            .Where(b => b.PublicId == req.BranchPublicId && !b.IsDeleted)
            .Select(b => new { b.Id, b.Name })
            .FirstOrDefaultAsync(ct);
        if (branch is null) return NotFound("Şube bulunamadı.");

        TimeOnly? start = req.StartTime != null ? TimeOnly.Parse(req.StartTime) : null;
        TimeOnly? end   = req.EndTime   != null ? TimeOnly.Parse(req.EndTime)   : null;
        var type = (DoctorSpecialDayType)req.Type;

        var existing = await _db.DoctorSpecialDays
            .FirstOrDefaultAsync(d => d.DoctorId == doctorId.Value
                                   && d.BranchId == branch.Id
                                   && d.SpecificDate == req.SpecificDate, ct);

        if (existing is not null)
        {
            existing.Update(type, start, end, req.Reason);
            existing.SetActive(true);
        }
        else
        {
            existing = DoctorSpecialDay.Create(doctorId.Value, branch.Id, req.SpecificDate, type, start, end, req.Reason);
            _db.DoctorSpecialDays.Add(existing);
        }

        await _db.SaveChangesAsync(ct);
        return StatusCode(201, new SpecialDayResponse(
            existing.Id, req.DoctorPublicId, req.BranchPublicId, branch.Name,
            existing.SpecificDate, (int)existing.Type,
            existing.StartTime?.ToString("HH:mm"), existing.EndTime?.ToString("HH:mm"), existing.Reason));
    }

    [HttpPut("{id:long}")]
    [RequirePermission("appointment:edit")]
    [ProducesResponseType(typeof(SpecialDayResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSpecialDayRequest req, CancellationToken ct)
    {
        var entity = await _db.DoctorSpecialDays
            .Include(d => d.Doctor)
            .Include(d => d.Branch)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null) return NotFound();

        TimeOnly? start = req.StartTime != null ? TimeOnly.Parse(req.StartTime) : null;
        TimeOnly? end   = req.EndTime   != null ? TimeOnly.Parse(req.EndTime)   : null;
        entity.Update((DoctorSpecialDayType)req.Type, start, end, req.Reason);
        await _db.SaveChangesAsync(ct);

        return Ok(new SpecialDayResponse(
            entity.Id, entity.Doctor.PublicId, entity.Branch.PublicId, entity.Branch.Name,
            entity.SpecificDate, (int)entity.Type,
            entity.StartTime?.ToString("HH:mm"), entity.EndTime?.ToString("HH:mm"), entity.Reason));
    }

    [HttpDelete("{id:long}")]
    [RequirePermission("appointment:edit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await _db.DoctorSpecialDays.FindAsync(new object[] { id }, ct);
        if (entity is null) return NotFound();
        entity.SetActive(false);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record SpecialDayResponse(
    long     Id,
    Guid     DoctorPublicId,
    Guid     BranchPublicId,
    string   BranchName,
    DateOnly SpecificDate,
    int      Type,
    string?  StartTime,
    string?  EndTime,
    string?  Reason);

public record UpsertSpecialDayRequest(
    Guid     DoctorPublicId,
    Guid     BranchPublicId,
    DateOnly SpecificDate,
    int      Type,
    string?  StartTime,
    string?  EndTime,
    string?  Reason);

public record UpdateSpecialDayRequest(
    int      Type,
    string?  StartTime,
    string?  EndTime,
    string?  Reason);
