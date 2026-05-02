using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Filters;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
public class BranchInvoiceSettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public BranchInvoiceSettingsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    /// <summary>Şubenin e-fatura entegratör ve sayaç ayarlarını döner.</summary>
    [HttpGet("api/settings/invoice/{branchId:long}")]
    [RequirePermission("settings:view")]
    [ProducesResponseType(typeof(BranchInvoiceSettingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(long branchId, CancellationToken ct)
    {
        var settings = await _db.BranchInvoiceSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BranchId == branchId, ct);

        if (settings == null)
            return Ok(BranchInvoiceSettingsResponse.Empty(branchId));

        return Ok(BranchInvoiceSettingsResponse.From(settings));
    }

    /// <summary>Entegratör ve sayaç ayarlarını kaydeder.</summary>
    [HttpPut("api/settings/invoice/{branchId:long}")]
    [RequirePermission("settings:edit")]
    [ProducesResponseType(typeof(BranchInvoiceSettingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Put(long branchId, [FromBody] UpdateBranchInvoiceSettingsRequest r, CancellationToken ct)
    {
        var settings = await _db.BranchInvoiceSettings
            .FirstOrDefaultAsync(s => s.BranchId == branchId, ct);

        if (settings == null)
        {
            settings = BranchInvoiceSettings.Create(branchId);
            _db.BranchInvoiceSettings.Add(settings);
        }

        settings.UpdateIntegrator(
            r.IntegratorType,
            r.CompanyVkn,
            r.IntegratorEndpoint,
            r.IntegratorCompanyCode,
            r.IntegratorUsername,
            r.IntegratorPassword);

        settings.UpdatePrefixes(
            r.NormalPrefix,   null,
            r.EArchivePrefix, null,
            r.EInvoicePrefix, null);

        await _db.SaveChangesAsync(ct);
        return Ok(BranchInvoiceSettingsResponse.From(settings));
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record BranchInvoiceSettingsResponse(
    long BranchId,
    InvoiceIntegratorType IntegratorType,
    string? CompanyVkn,
    string? IntegratorEndpoint,
    string? IntegratorCompanyCode,
    string? IntegratorUsername,
    bool HasPassword,       // şifreyi gösterme, sadece var mı yok mu
    string? NormalPrefix,
    long NormalCounter,
    string? EArchivePrefix,
    long EArchiveCounter,
    string? EInvoicePrefix,
    long EInvoiceCounter
)
{
    public static BranchInvoiceSettingsResponse Empty(long branchId) => new(
        branchId, InvoiceIntegratorType.None,
        null, null, null, null, false,
        null, 0, null, 0, null, 0);

    public static BranchInvoiceSettingsResponse From(BranchInvoiceSettings s) => new(
        s.BranchId, s.IntegratorType,
        s.CompanyVkn, s.IntegratorEndpoint, s.IntegratorCompanyCode, s.IntegratorUsername,
        !string.IsNullOrEmpty(s.IntegratorPassword),
        s.NormalPrefix,   s.NormalCounter,
        s.EArchivePrefix, s.EArchiveCounter,
        s.EInvoicePrefix, s.EInvoiceCounter);
}

public record UpdateBranchInvoiceSettingsRequest(
    InvoiceIntegratorType IntegratorType,
    string? CompanyVkn,
    string? IntegratorEndpoint,
    string? IntegratorCompanyCode,
    string? IntegratorUsername,
    string? IntegratorPassword,  // null → mevcut şifreyi değiştirme
    string? NormalPrefix,
    string? EArchivePrefix,
    string? EInvoicePrefix
);
