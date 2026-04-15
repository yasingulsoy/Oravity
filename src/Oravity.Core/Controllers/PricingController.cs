using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Core.Pricing.Application;
using Oravity.Core.Modules.Core.Pricing.Application.Commands;
using Oravity.Core.Modules.Core.Pricing.Application.Queries;
using System.Linq;

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

    /// <summary>Şirkete ait şubelerin fiyat ayarlarını listeler.</summary>
    [HttpGet("api/pricing/branches")]
    [RequirePermission("pricing:view")]
    [ProducesResponseType(typeof(IReadOnlyList<BranchPricingResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBranchPricingSettings()
    {
        var result = await _mediator.Send(new GetBranchPricingQuery());
        return Ok(result);
    }

    /// <summary>Şubenin fiyat çarpanını günceller.</summary>
    [HttpPatch("api/pricing/branches/{branchId:long}/multiplier")]
    [RequirePermission("pricing:edit")]
    [ProducesResponseType(typeof(BranchPricingResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBranchMultiplier(
        long branchId,
        [FromBody] UpdateBranchMultiplierRequest request)
    {
        var result = await _mediator.Send(new UpdateBranchPricingMultiplierCommand(branchId, request.PricingMultiplier));
        return Ok(result);
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

    /// <summary>Tedavi için kural motoruyla fiyat hesaplar (plan builder kullanır).</summary>
    [HttpGet("api/pricing/treatment/{publicId:guid}/price")]
    [ProducesResponseType(typeof(TreatmentPriceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTreatmentPrice(
        Guid publicId,
        [FromQuery] long? branchId      = null,
        [FromQuery] long? institutionId = null,
        [FromQuery] bool  isOss         = false)
    {
        var result = await _mediator.Send(new GetTreatmentPriceQuery(publicId, branchId, institutionId, isOss));
        return Ok(result);
    }

    /// <summary>Yeni referans fiyat listesi oluşturur.</summary>
    [HttpPost("api/pricing/reference-lists")]
    [RequirePermission("pricing:edit")]
    [ProducesResponseType(typeof(ReferencePriceListResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateReferenceList([FromBody] CreateReferenceListRequest request)
    {
        var result = await _mediator.Send(new CreateReferencePriceListCommand(
            request.Code, request.Name, request.SourceType, request.Year));
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Tüm referans fiyat listelerini döner.</summary>
    [HttpGet("api/pricing/reference-lists")]
    [RequirePermission("pricing:view")]
    [ProducesResponseType(typeof(IReadOnlyList<ReferencePriceListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReferenceLists()
    {
        var result = await _mediator.Send(new GetReferencePriceListsQuery());
        return Ok(result);
    }

    /// <summary>Referans fiyat listesinin kalemlerini döner (sayfalı).</summary>
    [HttpGet("api/pricing/reference-lists/{listId:long}/items")]
    [RequirePermission("pricing:view")]
    [ProducesResponseType(typeof(ReferencePriceItemsPagedResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReferenceItems(
        long listId,
        [FromQuery] string? search  = null,
        [FromQuery] int page        = 1,
        [FromQuery] int pageSize    = 50)
    {
        var result = await _mediator.Send(new GetReferencePriceItemsQuery(listId, search, page, pageSize));
        return Ok(result);
    }

    /// <summary>Referans fiyat kalemi ekler veya günceller (upsert).</summary>
    [HttpPut("api/pricing/reference-lists/{listId:long}/items/{code}")]
    [RequirePermission("pricing:edit")]
    [ProducesResponseType(typeof(ReferencePriceItemResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertReferenceItem(
        long listId,
        string code,
        [FromBody] UpsertReferenceItemRequest request)
    {
        var result = await _mediator.Send(new UpsertReferencePriceItemCommand(
            listId,
            code,
            request.TreatmentName,
            request.Price,
            request.PriceKdv,
            request.Currency,
            request.ValidFrom,
            request.ValidUntil));
        return Ok(result);
    }

    /// <summary>Fiyatlandırma kuralını siler.</summary>
    [HttpDelete("api/pricing/rules/{publicId:guid}")]
    [RequirePermission("pricing:edit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRule(Guid publicId)
    {
        await _mediator.Send(new DeletePricingRuleCommand(publicId));
        return NoContent();
    }

    /// <summary>Referans fiyat kalemini siler.</summary>
    [HttpDelete("api/pricing/reference-lists/{listId:long}/items/{code}")]
    [RequirePermission("pricing:edit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReferenceItem(long listId, string code)
    {
        await _mediator.Send(new DeleteReferencePriceItemCommand(listId, code));
        return NoContent();
    }

    /// <summary>Referans fiyat listesine toplu kalem ekler/günceller (CSV/Excel import).</summary>
    [HttpPost("api/pricing/reference-lists/{listId:long}/items/bulk")]
    [RequirePermission("pricing:edit")]
    [ProducesResponseType(typeof(BulkUpsertResultResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkUpsertReferenceItems(
        long listId,
        [FromBody] BulkUpsertRequest request)
    {
        var count = await _mediator.Send(new BulkUpsertReferencePriceItemsCommand(
            listId,
            request.Items.Select(i => new BulkUpsertItem(i.Code, i.Name, i.Price, i.PriceKdv, i.Currency ?? "TRY")).ToArray()));
        return Ok(new BulkUpsertResultResponse(count));
    }

    /// <summary>Tedavi için fiyat hesaplar (manuel bağlam).</summary>
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

public record UpdateBranchMultiplierRequest(decimal PricingMultiplier);

public record CreateReferenceListRequest(
    string Code,
    string Name,
    string SourceType = "private",
    int    Year       = 2026
);

public record UpsertReferenceItemRequest(
    string    TreatmentName,
    decimal   Price,
    decimal   PriceKdv      = 0,
    string    Currency      = "TRY",
    DateTime? ValidFrom     = null,
    DateTime? ValidUntil    = null
);

public record CalculatePriceRequest(
    Guid    TreatmentPublicId,
    decimal BasePrice,
    decimal? InstitutionPrice       = null,
    decimal? CampaignDiscountRate   = null,
    bool     IsInstitutionAgreement = false
);

public record BulkUpsertItemRequest(
    string  Code,
    string  Name,
    decimal Price,
    decimal PriceKdv  = 0,
    string? Currency  = "TRY"
);

public record BulkUpsertRequest(BulkUpsertItemRequest[] Items);

public record BulkUpsertResultResponse(int Count);
