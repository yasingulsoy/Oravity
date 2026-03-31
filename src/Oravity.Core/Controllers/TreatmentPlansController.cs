using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Core.Modules.Treatment.Application.Commands;
using Oravity.Core.Modules.Treatment.Application.Queries;

namespace Oravity.Core.Controllers;

/// <summary>
/// Tedavi planı yönetimi.
/// Tüm endpoint'ler JWT + permission koruması altındadır.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class TreatmentPlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public TreatmentPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Sorgular ─────────────────────────────────────────────────────────

    /// <summary>Hastanın tüm tedavi planlarını listeler (item'larla birlikte).</summary>
    [HttpGet("api/patients/{patientId:long}/treatment-plans")]
    [RequirePermission("treatment_plan:view")]
    [ProducesResponseType(typeof(IReadOnlyList<TreatmentPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByPatient(long patientId)
    {
        var result = await _mediator.Send(new GetPatientTreatmentPlansQuery(patientId));
        return Ok(result);
    }

    /// <summary>Tedavi planını public_id ile getirir.</summary>
    [HttpGet("api/treatment-plans/{id:guid}")]
    [RequirePermission("treatment_plan:view")]
    [ProducesResponseType(typeof(TreatmentPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetTreatmentPlanByIdQuery(id));
        return Ok(result);
    }

    // ── Plan Komutları ────────────────────────────────────────────────────

    /// <summary>Yeni tedavi planı oluşturur (Taslak).</summary>
    [HttpPost("api/treatment-plans")]
    [RequirePermission("treatment_plan:create")]
    [ProducesResponseType(typeof(TreatmentPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateTreatmentPlanRequest request)
    {
        var result = await _mediator.Send(new CreateTreatmentPlanCommand(
            request.PatientId,
            request.DoctorId,
            request.Name,
            request.Notes));

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Taslak planı onaylar. Plan ve tüm kalemleri Onaylandı (2) olur.
    /// </summary>
    [HttpPut("api/treatment-plans/{id:guid}/approve")]
    [RequirePermission("treatment_plan:edit")]
    [ProducesResponseType(typeof(TreatmentPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _mediator.Send(new ApproveTreatmentPlanCommand(id));
        return Ok(result);
    }

    // ── Kalem Komutları ───────────────────────────────────────────────────

    /// <summary>
    /// Plana yeni tedavi kalemi ekler.
    /// Onaylı/Tamamlanmış plana kalem eklenemez.
    /// </summary>
    [HttpPost("api/treatment-plans/{id:guid}/items")]
    [RequirePermission("treatment_plan:create")]
    [ProducesResponseType(typeof(TreatmentPlanItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddTreatmentPlanItemRequest request)
    {
        var result = await _mediator.Send(new AddTreatmentPlanItemCommand(
            id,
            request.TreatmentId,
            request.UnitPrice,
            request.DiscountRate,
            request.ToothNumber,
            request.ToothSurfaces,
            request.BodyRegionCode,
            request.DoctorId,
            request.Notes));

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Tedavi kalemini tamamlandı olarak işaretler, completed_at set edilir.
    /// İzin: treatment_plan:complete
    /// </summary>
    [HttpPut("api/treatment-plans/{id:guid}/items/{itemId:guid}/complete")]
    [RequirePermission("treatment_plan:complete")]
    [ProducesResponseType(typeof(TreatmentPlanItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteItem(Guid id, Guid itemId)
    {
        var result = await _mediator.Send(new CompleteTreatmentPlanItemCommand(itemId));
        return Ok(result);
    }

    /// <summary>
    /// Planlanmış (status=1) tedavi kalemini siler.
    /// Tamamlanmış kalem silinemez.
    /// İzin: treatment_plan:delete_planned
    /// </summary>
    [HttpDelete("api/treatment-plans/{id:guid}/items/{itemId:guid}")]
    [RequirePermission("treatment_plan:delete_planned")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(Guid id, Guid itemId)
    {
        await _mediator.Send(new DeletePlannedTreatmentCommand(itemId));
        return NoContent();
    }
}

// ─── Request DTO'lar ───────────────────────────────────────────────────────

public record CreateTreatmentPlanRequest(
    long PatientId,
    long DoctorId,
    string Name,
    string? Notes
);

public record AddTreatmentPlanItemRequest(
    long TreatmentId,
    decimal UnitPrice,
    decimal DiscountRate,
    string? ToothNumber,
    string? ToothSurfaces,
    string? BodyRegionCode,
    long? DoctorId,
    string? Notes
);
