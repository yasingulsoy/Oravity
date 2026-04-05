using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Core.Pricing.Application;
using Oravity.Core.Modules.Core.Pricing.Application.Commands;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Controllers;

/// <summary>
/// Tedavi ↔ referans fiyat listesi eşleştirme yönetimi.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class TreatmentMappingsController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public TreatmentMappingsController(IMediator mediator, AppDbContext db, ITenantContext tenant)
    {
        _mediator = mediator;
        _db       = db;
        _tenant   = tenant;
    }

    /// <summary>Tedavinin tüm referans eşleştirmelerini listeler.</summary>
    [HttpGet("api/treatments/{treatmentId:guid}/mappings")]
    [RequirePermission("pricing:view")]
    [ProducesResponseType(typeof(IReadOnlyList<TreatmentMappingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMappings(Guid treatmentId)
    {
        var companyId = _tenant.CompanyId
            ?? throw new ForbiddenException("Şirket bağlamı gereklidir.");

        var treatment = await _db.Treatments
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.PublicId == treatmentId && t.CompanyId == companyId)
            ?? throw new NotFoundException("Tedavi bulunamadı.");

        var mappings = await _db.TreatmentMappings
            .AsNoTracking()
            .Include(m => m.InternalTreatment)
            .Include(m => m.ReferenceList)
            .Where(m => m.InternalTreatmentId == treatment.Id)
            .ToListAsync();

        return Ok(mappings.Select(PricingMappings.ToResponse).ToList());
    }

    /// <summary>Yeni tedavi eşleştirmesi oluşturur.</summary>
    [HttpPost("api/treatments/{treatmentId:guid}/mappings")]
    [RequirePermission("pricing:create")]
    [ProducesResponseType(typeof(TreatmentMappingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateMapping(
        Guid treatmentId,
        [FromBody] CreateTreatmentMappingRequest request)
    {
        var result = await _mediator.Send(new CreateTreatmentMappingCommand(
            treatmentId,
            request.ReferenceListId,
            request.ReferenceCode,
            request.MappingQuality,
            request.Notes));

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Tedavi eşleştirmesini siler.</summary>
    [HttpDelete("api/treatments/{treatmentId:guid}/mappings/{mappingId:long}")]
    [RequirePermission("pricing:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMapping(Guid treatmentId, long mappingId)
    {
        var companyId = _tenant.CompanyId
            ?? throw new ForbiddenException("Şirket bağlamı gereklidir.");

        var mapping = await _db.TreatmentMappings
            .Include(m => m.InternalTreatment)
            .FirstOrDefaultAsync(m => m.Id == mappingId
                                   && m.InternalTreatment.CompanyId == companyId
                                   && m.InternalTreatment.PublicId == treatmentId)
            ?? throw new NotFoundException("Eşleştirme bulunamadı.");

        _db.TreatmentMappings.Remove(mapping);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

// ─── Request DTO'lar ───────────────────────────────────────────────────────

public record CreateTreatmentMappingRequest(
    long    ReferenceListId,
    string  ReferenceCode,
    string? MappingQuality,
    string? Notes
);
