using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Core.Pricing.Application;
using Oravity.Core.Modules.Core.Pricing.Application.Commands;
using Oravity.Core.Modules.Core.Pricing.Application.Queries;

namespace Oravity.Core.Controllers;

/// <summary>
/// Fiyatlandırma kuralları yönetimi.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class PricingController : ControllerBase
{
    private readonly IMediator _mediator;

    public PricingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Şirketin fiyatlandırma kurallarını listeler (öncelik sırasına göre).</summary>
    [HttpGet("api/pricing/rules")]
    [RequirePermission("pricing:view")]
    [ProducesResponseType(typeof(IReadOnlyList<PricingRuleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRules([FromQuery] bool activeOnly = true)
    {
        var result = await _mediator.Send(new GetPricingRulesQuery(activeOnly));
        return Ok(result);
    }

    /// <summary>Yeni fiyatlandırma kuralı oluşturur.</summary>
    [HttpPost("api/pricing/rules")]
    [RequirePermission("pricing:create")]
    [ProducesResponseType(typeof(PricingRuleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateRule([FromBody] CreatePricingRuleRequest request)
    {
        var result = await _mediator.Send(new CreatePricingRuleCommand(
            request.BranchId,
            request.Name,
            request.Description,
            request.RuleType,
            request.Priority,
            request.IncludeFilters,
            request.ExcludeFilters,
            request.Formula,
            request.OutputCurrency,
            request.ValidFrom,
            request.ValidUntil,
            request.StopProcessing));

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Fiyatlandırma kuralını günceller.</summary>
    [HttpPut("api/pricing/rules/{publicId:guid}")]
    [RequirePermission("pricing:edit")]
    [ProducesResponseType(typeof(PricingRuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRule(Guid publicId, [FromBody] UpdatePricingRuleRequest request)
    {
        var result = await _mediator.Send(new UpdatePricingRuleCommand(
            publicId,
            request.Name,
            request.Description,
            request.RuleType,
            request.Priority,
            request.IncludeFilters,
            request.ExcludeFilters,
            request.Formula,
            request.OutputCurrency,
            request.ValidFrom,
            request.ValidUntil,
            request.StopProcessing,
            request.IsActive));

        return Ok(result);
    }

    /// <summary>Tedavi için fiyat hesaplar.</summary>
    [HttpPost("api/pricing/calculate")]
    [RequirePermission("pricing:view")]
    [ProducesResponseType(typeof(CalculatePriceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Calculate([FromBody] CalculatePriceRequest request)
    {
        var result = await _mediator.Send(new CalculateTreatmentPriceQuery(
            request.TreatmentPublicId,
            request.BasePrice,
            request.InstitutionPrice,
            request.CampaignDiscountRate,
            request.IsInstitutionAgreement));

        return Ok(result);
    }
}

// ─── Request DTO'lar ───────────────────────────────────────────────────────

public record CreatePricingRuleRequest(
    long?     BranchId,
    string    Name,
    string?   Description,
    string    RuleType,
    int       Priority,
    string?   IncludeFilters,
    string?   ExcludeFilters,
    string?   Formula,
    string    OutputCurrency,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    bool      StopProcessing
);

public record UpdatePricingRuleRequest(
    string    Name,
    string?   Description,
    string    RuleType,
    int       Priority,
    string?   IncludeFilters,
    string?   ExcludeFilters,
    string?   Formula,
    string    OutputCurrency,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    bool      StopProcessing,
    bool      IsActive
);

public record CalculatePriceRequest(
    Guid    TreatmentPublicId,
    decimal BasePrice,
    decimal? InstitutionPrice       = null,
    decimal? CampaignDiscountRate   = null,
    bool     IsInstitutionAgreement = false
);
