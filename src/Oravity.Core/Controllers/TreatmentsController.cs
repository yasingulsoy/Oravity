using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Core.Modules.Treatment.Application.Commands;
using Oravity.Core.Modules.Treatment.Application.Queries;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Controllers;

/// <summary>
/// Tedavi kataloğu yönetimi.
/// Tüm endpoint'ler JWT + permission koruması altındadır.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class TreatmentsController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ITenantContext _tenant;

    public TreatmentsController(IMediator mediator, ITenantContext tenant)
    {
        _mediator = mediator;
        _tenant   = tenant;
    }

    /// <summary>Tedavi kategorilerini hiyerarşik olarak listeler.</summary>
    [HttpGet("api/treatment-categories")]
    [RequirePermission("treatment:view")]
    [ProducesResponseType(typeof(IReadOnlyList<TreatmentCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _mediator.Send(new GetTreatmentCategoriesQuery());
        return Ok(result);
    }

    /// <summary>Şirketin tedavi kataloğunu sayfalı olarak listeler.</summary>
    [HttpGet("api/treatments")]
    [RequirePermission("treatment:view")]
    [ProducesResponseType(typeof(PagedTreatmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid?   categoryId = null,
        [FromQuery] string? search     = null,
        [FromQuery] bool    activeOnly = true,
        [FromQuery] int     page       = 1,
        [FromQuery] int     pageSize   = 20)
    {
        var result = await _mediator.Send(new GetTreatmentsQuery(
            _tenant.CompanyId, categoryId, search, activeOnly, page, pageSize));

        return Ok(result);
    }

    /// <summary>Tedaviyi public_id ile getirir.</summary>
    [HttpGet("api/treatments/{publicId:guid}")]
    [RequirePermission("treatment:view")]
    [ProducesResponseType(typeof(TreatmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid publicId)
    {
        var result = await _mediator.Send(new GetTreatmentByIdQuery(publicId));
        return Ok(result);
    }

    /// <summary>Yeni tedavi oluşturur.</summary>
    [HttpPost("api/treatments")]
    [RequirePermission("treatment:create")]
    [ProducesResponseType(typeof(TreatmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateTreatmentRequest request)
    {
        var result = await _mediator.Send(new CreateTreatmentCommand(
            request.Code,
            request.Name,
            request.CategoryPublicId,
            request.KdvRate,
            request.RequiresSurfaceSelection,
            request.RequiresLaboratory,
            request.AllowedScopes,
            request.Tags));

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Tedaviyi günceller.</summary>
    [HttpPut("api/treatments/{publicId:guid}")]
    [RequirePermission("treatment:edit")]
    [ProducesResponseType(typeof(TreatmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid publicId, [FromBody] UpdateTreatmentRequest request)
    {
        var result = await _mediator.Send(new UpdateTreatmentCommand(
            publicId,
            request.Code,
            request.Name,
            request.CategoryPublicId,
            request.KdvRate,
            request.RequiresSurfaceSelection,
            request.RequiresLaboratory,
            request.AllowedScopes,
            request.Tags,
            request.IsActive));

        return Ok(result);
    }

    /// <summary>Tedaviyi soft-delete ile siler (IsActive=false yapar).</summary>
    [HttpDelete("api/treatments/{publicId:guid}")]
    [RequirePermission("treatment:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid publicId)
    {
        // Mevcut kaydı çek, IsActive=false olarak güncelle
        var current = await _mediator.Send(new GetTreatmentByIdQuery(publicId));

        await _mediator.Send(new UpdateTreatmentCommand(
            publicId,
            current.Code,
            current.Name,
            current.Category?.PublicId,
            current.KdvRate,
            current.RequiresSurfaceSelection,
            current.RequiresLaboratory,
            current.AllowedScopes,
            current.Tags,
            IsActive: false));

        return NoContent();
    }
}

// ─── Request DTO'lar ───────────────────────────────────────────────────────

public record CreateTreatmentRequest(
    string   Code,
    string   Name,
    Guid?    CategoryPublicId,
    decimal  KdvRate,
    bool     RequiresSurfaceSelection,
    bool     RequiresLaboratory,
    int[]?   AllowedScopes,
    string?  Tags
);

public record UpdateTreatmentRequest(
    string   Code,
    string   Name,
    Guid?    CategoryPublicId,
    decimal  KdvRate,
    bool     RequiresSurfaceSelection,
    bool     RequiresLaboratory,
    int[]?   AllowedScopes,
    string?  Tags,
    bool     IsActive
);
