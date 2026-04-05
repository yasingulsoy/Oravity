using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Filters;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Controllers;

/// <summary>
/// Şirkete özel lookup tabloları yönetimi: geliş şekilleri, vatandaşlık tipleri.
/// Global defaults (CompanyId=null) salt okunur; şirket kendi ek kayıtlarını ekleyebilir.
/// </summary>
[ApiController]
[Route("api/lookups")]
[Authorize]
[Produces("application/json")]
public class LookupController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public LookupController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // ─── Geliş Şekilleri ─────────────────────────────────────────────────

    /// <summary>Şirkete ait + global geliş şekillerini döner (aktif olanlar)</summary>
    [HttpGet("referral-sources")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> GetReferralSources()
    {
        var items = await _db.ReferralSources
            .Where(x => x.IsActive && (x.CompanyId == null || x.CompanyId == _tenant.CompanyId))
            .OrderBy(x => x.CompanyId == null ? 0 : 1)  // globaller önce
            .ThenBy(x => x.SortOrder)
            .Select(x => new LookupItemResponse(x.Id, x.PublicId, x.Name, x.Code, x.SortOrder, x.CompanyId == null, x.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Şirkete özel geliş şekli ekle</summary>
    [HttpPost("referral-sources")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> CreateReferralSource([FromBody] CreateLookupRequest req)
    {
        if (_tenant.CompanyId is null) return BadRequest("CompanyId gerekli.");

        var code = req.Code ?? req.Name.ToUpperInvariant().Replace(" ", "_");

        if (await _db.ReferralSources.AnyAsync(x => x.CompanyId == _tenant.CompanyId && x.Code == code))
            return Conflict("Bu kod zaten mevcut.");

        var maxSort = await _db.ReferralSources
            .Where(x => x.CompanyId == _tenant.CompanyId)
            .Select(x => (int?)x.SortOrder).MaxAsync() ?? 0;

        var item = ReferralSource.Create(req.Name, code, maxSort + 1, _tenant.CompanyId);
        _db.ReferralSources.Add(item);
        await _db.SaveChangesAsync();

        return Ok(new LookupItemResponse(item.Id, item.PublicId, item.Name, item.Code, item.SortOrder, false, item.IsActive));
    }

    /// <summary>Şirkete özel geliş şekli güncelle (global değiştirilemez)</summary>
    [HttpPut("referral-sources/{id:long}")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> UpdateReferralSource(long id, [FromBody] UpdateLookupRequest req)
    {
        var item = await _db.ReferralSources.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == _tenant.CompanyId);
        if (item is null) return NotFound();

        item.Update(req.Name, req.SortOrder, req.IsActive);
        await _db.SaveChangesAsync();

        return Ok(new LookupItemResponse(item.Id, item.PublicId, item.Name, item.Code, item.SortOrder, false, item.IsActive));
    }

    /// <summary>Şirkete özel geliş şekli sil (global silinemez)</summary>
    [HttpDelete("referral-sources/{id:long}")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> DeleteReferralSource(long id)
    {
        var item = await _db.ReferralSources.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == _tenant.CompanyId);
        if (item is null) return NotFound();

        _db.ReferralSources.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ─── Vatandaşlık Tipleri ─────────────────────────────────────────────

    /// <summary>Şirkete ait + global vatandaşlık tiplerini döner</summary>
    [HttpGet("citizenship-types")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> GetCitizenshipTypes()
    {
        var items = await _db.CitizenshipTypes
            .Where(x => x.IsActive && (x.CompanyId == null || x.CompanyId == _tenant.CompanyId))
            .OrderBy(x => x.CompanyId == null ? 0 : 1)
            .ThenBy(x => x.SortOrder)
            .Select(x => new LookupItemResponse(x.Id, x.PublicId, x.Name, x.Code, x.SortOrder, x.CompanyId == null, x.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Şirkete özel vatandaşlık tipi ekle</summary>
    [HttpPost("citizenship-types")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> CreateCitizenshipType([FromBody] CreateLookupRequest req)
    {
        if (_tenant.CompanyId is null) return BadRequest("CompanyId gerekli.");

        var code = req.Code ?? req.Name.ToUpperInvariant().Replace(" ", "_");

        if (await _db.CitizenshipTypes.AnyAsync(x => x.CompanyId == _tenant.CompanyId && x.Code == code))
            return Conflict("Bu kod zaten mevcut.");

        var maxSort = await _db.CitizenshipTypes
            .Where(x => x.CompanyId == _tenant.CompanyId)
            .Select(x => (int?)x.SortOrder).MaxAsync() ?? 0;

        var item = CitizenshipType.Create(req.Name, code, maxSort + 1, _tenant.CompanyId);
        _db.CitizenshipTypes.Add(item);
        await _db.SaveChangesAsync();

        return Ok(new LookupItemResponse(item.Id, item.PublicId, item.Name, item.Code, item.SortOrder, false, item.IsActive));
    }

    /// <summary>Şirkete özel vatandaşlık tipi güncelle</summary>
    [HttpPut("citizenship-types/{id:long}")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> UpdateCitizenshipType(long id, [FromBody] UpdateLookupRequest req)
    {
        var item = await _db.CitizenshipTypes.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == _tenant.CompanyId);
        if (item is null) return NotFound();

        item.Update(req.Name, req.SortOrder, req.IsActive);
        await _db.SaveChangesAsync();

        return Ok(new LookupItemResponse(item.Id, item.PublicId, item.Name, item.Code, item.SortOrder, false, item.IsActive));
    }

    /// <summary>Şirkete özel vatandaşlık tipi sil</summary>
    [HttpDelete("citizenship-types/{id:long}")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> DeleteCitizenshipType(long id)
    {
        var item = await _db.CitizenshipTypes.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == _tenant.CompanyId);
        if (item is null) return NotFound();

        _db.CitizenshipTypes.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

// ─── DTO'lar ─────────────────────────────────────────────────────────────────

public record LookupItemResponse(
    long Id,
    Guid PublicId,
    string Name,
    string Code,
    int SortOrder,
    bool IsGlobal,    // true = platform varsayılanı, düzenlenemez
    bool IsActive
);

public record CreateLookupRequest(string Name, string? Code);

public record UpdateLookupRequest(string Name, int SortOrder, bool IsActive);
