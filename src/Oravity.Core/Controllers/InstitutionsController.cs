using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Filters;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Controllers;

[ApiController]
[Route("api/institutions")]
[Authorize]
[Produces("application/json")]
public class InstitutionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public InstitutionsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    /// <summary>Kurumları listele — platform geneli + şirkete özel</summary>
    [HttpGet]
    [RequirePermission("institution:view")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        IQueryable<Institution> query = _db.Institutions.AsNoTracking().Where(x => x.IsActive);

        // Platform admin → tüm kurumları göster; normal kullanıcı → kendi şirketi + globaller
        if (!_tenant.IsPlatformAdmin)
        {
            var companyId = _tenant.CompanyId;
            query = query.Where(x => x.CompanyId == null || x.CompanyId == companyId);
        }

        var items = await query
            .OrderBy(x => x.Name)
            .Select(x => new InstitutionResponse(
                x.Id, x.PublicId, x.Name, x.Code, x.Type, x.MarketSegment,
                x.Phone, x.Email, x.Website,
                x.Country, x.City, x.District, x.Address,
                x.ContactPerson, x.ContactPhone,
                x.TaxNumber, x.TaxOffice,
                x.DiscountRate, x.PaymentDays, x.PaymentTerms,
                x.Notes,
                x.CompanyId == null, x.IsActive))
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>Kurum oluştur (şirkete özel)</summary>
    [HttpPost]
    [RequirePermission("institution:manage")]
    public async Task<IActionResult> Create([FromBody] CreateInstitutionRequest req, CancellationToken ct)
    {
        if (_tenant.CompanyId == null && !_tenant.IsPlatformAdmin)
            return Forbid();

        var companyId = _tenant.IsPlatformAdmin ? (long?)null : _tenant.CompanyId;
        var entity = Institution.Create(
            req.Name, req.Code, req.Type, companyId, req.MarketSegment,
            req.Phone, req.Email, req.Website,
            req.Country, req.City, req.District, req.Address,
            req.ContactPerson, req.ContactPhone,
            req.TaxNumber, req.TaxOffice,
            req.DiscountRate, req.PaymentDays ?? 30, req.PaymentTerms,
            req.Notes);
        _db.Institutions.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, ToResponse(entity));
    }

    /// <summary>Kurum güncelle (sadece şirkete özgü olanı)</summary>
    [HttpPut("{publicId:guid}")]
    [RequirePermission("institution:manage")]
    public async Task<IActionResult> Update(Guid publicId, [FromBody] UpdateInstitutionRequest req, CancellationToken ct)
    {
        var entity = await _db.Institutions.FirstOrDefaultAsync(x => x.PublicId == publicId, ct);
        if (entity == null) return NotFound();
        if (entity.CompanyId == null && !_tenant.IsPlatformAdmin) return Forbid();
        if (entity.CompanyId != null && entity.CompanyId != _tenant.CompanyId) return Forbid();

        entity.Update(
            req.Name, req.Code, req.Type, req.IsActive, req.MarketSegment,
            req.Phone, req.Email, req.Website,
            req.Country, req.City, req.District, req.Address,
            req.ContactPerson, req.ContactPhone,
            req.TaxNumber, req.TaxOffice,
            req.DiscountRate, req.PaymentDays ?? 30, req.PaymentTerms,
            req.Notes);
        await _db.SaveChangesAsync(ct);
        return Ok(ToResponse(entity));
    }

    /// <summary>Kurum sil (sadece şirkete özgü olanı)</summary>
    [HttpDelete("{publicId:guid}")]
    [RequirePermission("institution:manage")]
    public async Task<IActionResult> Delete(Guid publicId, CancellationToken ct)
    {
        var entity = await _db.Institutions.FirstOrDefaultAsync(x => x.PublicId == publicId, ct);
        if (entity == null) return NotFound();
        if (entity.CompanyId == null && !_tenant.IsPlatformAdmin) return Forbid();
        if (entity.CompanyId != null && entity.CompanyId != _tenant.CompanyId) return Forbid();

        entity.SoftDelete();
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static InstitutionResponse ToResponse(Institution x) => new(
        x.Id, x.PublicId, x.Name, x.Code, x.Type, x.MarketSegment,
        x.Phone, x.Email, x.Website,
        x.Country, x.City, x.District, x.Address,
        x.ContactPerson, x.ContactPhone,
        x.TaxNumber, x.TaxOffice,
        x.DiscountRate, x.PaymentDays, x.PaymentTerms,
        x.Notes,
        x.CompanyId == null, x.IsActive);
}

public record InstitutionResponse(
    long Id, Guid PublicId, string Name, string? Code, string? Type, string? MarketSegment,
    string? Phone, string? Email, string? Website,
    string? Country, string? City, string? District, string? Address,
    string? ContactPerson, string? ContactPhone,
    string? TaxNumber, string? TaxOffice,
    decimal? DiscountRate, int PaymentDays, string? PaymentTerms,
    string? Notes,
    bool IsGlobal, bool IsActive);

public record CreateInstitutionRequest(
    string Name, string? Code, string? Type, string? MarketSegment,
    string? Phone, string? Email, string? Website,
    string? Country, string? City, string? District, string? Address,
    string? ContactPerson, string? ContactPhone,
    string? TaxNumber, string? TaxOffice,
    decimal? DiscountRate, int? PaymentDays, string? PaymentTerms,
    string? Notes);

public record UpdateInstitutionRequest(
    string Name, string? Code, string? Type, bool IsActive, string? MarketSegment,
    string? Phone, string? Email, string? Website,
    string? Country, string? City, string? District, string? Address,
    string? ContactPerson, string? ContactPhone,
    string? TaxNumber, string? TaxOffice,
    decimal? DiscountRate, int? PaymentDays, string? PaymentTerms,
    string? Notes);
