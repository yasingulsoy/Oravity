using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Commission.Application;
using Oravity.Core.Modules.Commission.Application.Commands;
using Oravity.Core.Modules.Commission.Application.Queries;
using Oravity.Core.Modules.Finance.Application;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Controllers;

/// <summary>
/// Hakediş şablonları, atamalar ve hedefler.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class CommissionTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommissionTemplatesController(IMediator mediator) => _mediator = mediator;

    // ── Templates ─────────────────────────────────────────────────────────

    [HttpGet("api/commission-templates")]
    [RequirePermission("commission:view")]
    [ProducesResponseType(typeof(IReadOnlyList<CommissionTemplateResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplates([FromQuery] bool activeOnly = false)
    {
        var result = await _mediator.Send(new GetCommissionTemplatesQuery(activeOnly));
        return Ok(result);
    }

    [HttpGet("api/commission-templates/{publicId:guid}")]
    [RequirePermission("commission:view")]
    [ProducesResponseType(typeof(CommissionTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate(Guid publicId)
    {
        var result = await _mediator.Send(new GetCommissionTemplateByIdQuery(publicId));
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("api/commission-templates")]
    [RequirePermission("commission:manage")]
    [ProducesResponseType(typeof(CommissionTemplateResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCommissionTemplateRequest r)
    {
        var result = await _mediator.Send(new CreateCommissionTemplateCommand(
            r.Name, r.WorkingStyle, r.PaymentType, r.FixedFee, r.PrimRate,
            r.InstitutionPayOnInvoice, r.JobStartCalculation,
            r.ClinicTargetEnabled, r.ClinicTargetBonusRate,
            r.DoctorTargetEnabled, r.DoctorTargetBonusRate,
            r.DeductTreatmentPlanCommission, r.DeductLabCost, r.DeductTreatmentCost,
            r.KdvEnabled, r.KdvRate, r.KdvAppliedPaymentTypes,
            r.ExtraExpenseEnabled, r.ExtraExpenseRate,
            r.WithholdingTaxEnabled, r.WithholdingTaxRate,
            r.JobStartPrices));
        return Created($"api/commission-templates/{result.PublicId}", result);
    }

    [HttpPut("api/commission-templates/{publicId:guid}")]
    [RequirePermission("commission:manage")]
    [ProducesResponseType(typeof(CommissionTemplateResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid publicId, [FromBody] UpdateCommissionTemplateRequest r)
    {
        var result = await _mediator.Send(new UpdateCommissionTemplateCommand(
            publicId, r.Name, r.WorkingStyle, r.PaymentType, r.FixedFee, r.PrimRate,
            r.InstitutionPayOnInvoice, r.JobStartCalculation,
            r.ClinicTargetEnabled, r.ClinicTargetBonusRate,
            r.DoctorTargetEnabled, r.DoctorTargetBonusRate,
            r.DeductTreatmentPlanCommission, r.DeductLabCost, r.DeductTreatmentCost,
            r.KdvEnabled, r.KdvRate, r.KdvAppliedPaymentTypes,
            r.ExtraExpenseEnabled, r.ExtraExpenseRate,
            r.WithholdingTaxEnabled, r.WithholdingTaxRate, r.IsActive,
            r.JobStartPrices));
        return Ok(result);
    }

    [HttpDelete("api/commission-templates/{publicId:guid}")]
    [RequirePermission("commission:manage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid publicId)
    {
        await _mediator.Send(new DeleteCommissionTemplateCommand(publicId));
        return NoContent();
    }

    // ── Assignments ───────────────────────────────────────────────────────

    [HttpGet("api/commission-assignments")]
    [RequirePermission("commission:view")]
    [ProducesResponseType(typeof(IReadOnlyList<TemplateAssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssignments(
        [FromQuery] long? doctorId = null,
        [FromQuery] bool activeOnly = true)
    {
        var result = await _mediator.Send(new GetTemplateAssignmentsQuery(doctorId, activeOnly));
        return Ok(result);
    }

    [HttpPost("api/commission-assignments")]
    [RequirePermission("commission:manage")]
    [ProducesResponseType(typeof(TemplateAssignmentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Assign([FromBody] AssignTemplateRequest r)
    {
        var result = await _mediator.Send(new AssignTemplateCommand(
            r.DoctorId, r.TemplatePublicId, r.EffectiveDate, r.ExpiryDate));
        return Created($"api/commission-assignments/{result.PublicId}", result);
    }

    [HttpDelete("api/commission-assignments/{publicId:guid}")]
    [RequirePermission("commission:manage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unassign(Guid publicId)
    {
        await _mediator.Send(new UnassignTemplateCommand(publicId));
        return NoContent();
    }

    // ── Doctor Targets ────────────────────────────────────────────────────

    [HttpGet("api/commission-targets/doctors")]
    [RequirePermission("commission:view")]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorTargetResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctorTargets(
        [FromQuery] long? doctorId = null,
        [FromQuery] long? branchId = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var result = await _mediator.Send(new GetDoctorTargetsQuery(doctorId, branchId, year, month));
        return Ok(result);
    }

    [HttpPut("api/commission-targets/doctors")]
    [RequirePermission("commission:manage")]
    [ProducesResponseType(typeof(DoctorTargetResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertDoctorTarget([FromBody] UpsertDoctorTargetRequest r)
    {
        var result = await _mediator.Send(new UpsertDoctorTargetCommand(
            r.DoctorId, r.BranchId, r.Year, r.Month, r.TargetAmount));
        return Ok(result);
    }

    // ── Branch Targets ────────────────────────────────────────────────────

    [HttpGet("api/commission-targets/branches")]
    [RequirePermission("commission:view")]
    [ProducesResponseType(typeof(IReadOnlyList<BranchTargetResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBranchTargets(
        [FromQuery] long? branchId = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var result = await _mediator.Send(new GetBranchTargetsQuery(branchId, year, month));
        return Ok(result);
    }

    [HttpPut("api/commission-targets/branches")]
    [RequirePermission("commission:manage")]
    [ProducesResponseType(typeof(BranchTargetResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertBranchTarget([FromBody] UpsertBranchTargetRequest r)
    {
        var result = await _mediator.Send(new UpsertBranchTargetCommand(
            r.BranchId, r.Year, r.Month, r.TargetAmount));
        return Ok(result);
    }

    // ── Pending commissions & Batch distribute ───────────────────────────

    [HttpGet("api/commissions/pending")]
    [RequirePermission("commission:view")]
    [ProducesResponseType(typeof(PendingCommissionsSummary), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending(
        [FromQuery] long? doctorId = null,
        [FromQuery] long? branchId = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var result = await _mediator.Send(new GetPendingCommissionsQuery(doctorId, branchId, year, month));
        return Ok(result);
    }

    [HttpPost("api/commissions/calculate")]
    [RequirePermission("commission:manage")]
    [ProducesResponseType(typeof(Oravity.Core.Modules.Finance.Application.DoctorCommissionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Calculate([FromBody] CalculateCommissionRequest r)
    {
        var result = await _mediator.Send(new CalculateCommissionCommand(r.TreatmentPlanItemId));
        return Ok(result);
    }

    [HttpPost("api/commissions/distribute-batch")]
    [RequirePermission("commission:approve_dist")]
    [ProducesResponseType(typeof(BatchDistributionResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DistributeBatch([FromBody] DistributeCommissionsBatchRequest r)
    {
        var result = await _mediator.Send(new DistributeCommissionsBatchCommand(r.CommissionIds));
        return Ok(result);
    }

    // ── Hekim cari hesap ─────────────────────────────────────────────────

    [HttpGet("api/doctors/{doctorId:long}/account")]
    [RequirePermission("commission:view")]
    [ProducesResponseType(typeof(DoctorAccountResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctorAccount(
        long doctorId,
        [FromQuery] long? branchId = null,
        [FromQuery] int? year = null)
    {
        var result = await _mediator.Send(new GetDoctorAccountQuery(doctorId, branchId, year));
        return Ok(result);
    }
}

// ── Request DTOs ───────────────────────────────────────────────────────────

public record CreateCommissionTemplateRequest(
    string Name,
    CommissionWorkingStyle WorkingStyle,
    CommissionPaymentType PaymentType,
    decimal FixedFee,
    decimal PrimRate,
    bool InstitutionPayOnInvoice,
    JobStartCalculation? JobStartCalculation,
    bool ClinicTargetEnabled,
    decimal? ClinicTargetBonusRate,
    bool DoctorTargetEnabled,
    decimal? DoctorTargetBonusRate,
    bool DeductTreatmentPlanCommission,
    bool DeductLabCost,
    bool DeductTreatmentCost,
    bool KdvEnabled,
    decimal? KdvRate,
    string? KdvAppliedPaymentTypes,
    bool ExtraExpenseEnabled,
    decimal? ExtraExpenseRate,
    bool WithholdingTaxEnabled,
    decimal? WithholdingTaxRate,
    IReadOnlyList<JobStartPriceRequest>? JobStartPrices
);

public record UpdateCommissionTemplateRequest(
    string Name,
    CommissionWorkingStyle WorkingStyle,
    CommissionPaymentType PaymentType,
    decimal FixedFee,
    decimal PrimRate,
    bool InstitutionPayOnInvoice,
    JobStartCalculation? JobStartCalculation,
    bool ClinicTargetEnabled,
    decimal? ClinicTargetBonusRate,
    bool DoctorTargetEnabled,
    decimal? DoctorTargetBonusRate,
    bool DeductTreatmentPlanCommission,
    bool DeductLabCost,
    bool DeductTreatmentCost,
    bool KdvEnabled,
    decimal? KdvRate,
    string? KdvAppliedPaymentTypes,
    bool ExtraExpenseEnabled,
    decimal? ExtraExpenseRate,
    bool WithholdingTaxEnabled,
    decimal? WithholdingTaxRate,
    bool IsActive,
    IReadOnlyList<JobStartPriceRequest>? JobStartPrices
);

public record AssignTemplateRequest(
    long DoctorId,
    Guid TemplatePublicId,
    DateOnly EffectiveDate,
    DateOnly? ExpiryDate
);

public record UpsertDoctorTargetRequest(
    long DoctorId,
    long BranchId,
    int Year,
    int Month,
    decimal TargetAmount
);

public record UpsertBranchTargetRequest(
    long BranchId,
    int Year,
    int Month,
    decimal TargetAmount
);

public record CalculateCommissionRequest(long TreatmentPlanItemId);
public record DistributeCommissionsBatchRequest(IReadOnlyList<long> CommissionIds);
