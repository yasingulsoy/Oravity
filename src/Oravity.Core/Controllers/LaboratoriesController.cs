using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Laboratory.Application;
using Oravity.Core.Modules.Laboratory.Application.Commands;
using Oravity.Core.Modules.Laboratory.Application.Queries;

namespace Oravity.Core.Controllers;

/// <summary>
/// Laboratuvar yönetimi: laboratuvarlar, şube atamaları, fiyat listeleri, iş emirleri
/// ve yönetici onay yetkilileri (SPEC §415 Laboratuvar Yönetim Sistemi).
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class LaboratoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public LaboratoriesController(IMediator mediator) => _mediator = mediator;

    // ─── Laboratuvarlar ───────────────────────────────────────────────────
    [HttpGet("api/laboratories")]
    [RequirePermission("laboratory:view")]
    [ProducesResponseType(typeof(IReadOnlyList<LaboratoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLaboratories(
        [FromQuery] bool  activeOnly     = false,
        [FromQuery] Guid? branchPublicId = null)
    {
        var result = await _mediator.Send(new GetLaboratoriesQuery(activeOnly, branchPublicId));
        return Ok(result);
    }

    [HttpGet("api/laboratories/{publicId:guid}")]
    [RequirePermission("laboratory:view")]
    [ProducesResponseType(typeof(LaboratoryDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLaboratory(Guid publicId)
        => Ok(await _mediator.Send(new GetLaboratoryDetailQuery(publicId)));

    [HttpPost("api/laboratories")]
    [RequirePermission("laboratory:manage")]
    [ProducesResponseType(typeof(LaboratoryResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLaboratory([FromBody] CreateLaboratoryRequest request)
    {
        var result = await _mediator.Send(new CreateLaboratoryCommand(
            request.Name, request.Code,
            request.Phone, request.Email, request.Website,
            request.Country, request.City, request.District, request.Address,
            request.ContactPerson, request.ContactPhone,
            request.WorkingDays, request.WorkingHours,
            request.PaymentTerms, request.PaymentDays, request.Notes));
        return Created($"api/laboratories/{result.PublicId}", result);
    }

    [HttpPut("api/laboratories/{publicId:guid}")]
    [RequirePermission("laboratory:manage")]
    [ProducesResponseType(typeof(LaboratoryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLaboratory(Guid publicId, [FromBody] UpdateLaboratoryRequest request)
    {
        var result = await _mediator.Send(new UpdateLaboratoryCommand(
            publicId, request.Name, request.Code,
            request.Phone, request.Email, request.Website,
            request.Country, request.City, request.District, request.Address,
            request.ContactPerson, request.ContactPhone,
            request.WorkingDays, request.WorkingHours,
            request.PaymentTerms, request.PaymentDays, request.Notes, request.IsActive));
        return Ok(result);
    }

    [HttpDelete("api/laboratories/{publicId:guid}")]
    [RequirePermission("laboratory:manage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteLaboratory(Guid publicId)
    {
        await _mediator.Send(new DeleteLaboratoryCommand(publicId));
        return NoContent();
    }

    // ─── Şube atamaları ───────────────────────────────────────────────────
    [HttpPost("api/laboratories/{publicId:guid}/branch-assignments")]
    [RequirePermission("laboratory:manage")]
    [ProducesResponseType(typeof(BranchAssignmentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignBranch(
        Guid publicId,
        [FromBody] AssignBranchRequest request)
    {
        var result = await _mediator.Send(new AssignLaboratoryToBranchCommand(
            publicId, request.BranchPublicId, request.Priority, request.IsActive));
        return Ok(result);
    }

    [HttpDelete("api/laboratories/branch-assignments/{assignmentPublicId:guid}")]
    [RequirePermission("laboratory:manage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveBranchAssignment(Guid assignmentPublicId)
    {
        await _mediator.Send(new RemoveLaboratoryBranchAssignmentCommand(assignmentPublicId));
        return NoContent();
    }

    // ─── Fiyat listesi ────────────────────────────────────────────────────
    [HttpPost("api/laboratories/{publicId:guid}/price-items")]
    [RequirePermission("laboratory:manage")]
    [ProducesResponseType(typeof(LaboratoryPriceItemResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertPriceItem(
        Guid publicId,
        [FromBody] UpsertPriceItemRequest request)
    {
        var result = await _mediator.Send(new UpsertLaboratoryPriceItemCommand(
            publicId, request.PublicId,
            request.ItemName, request.ItemCode, request.Description,
            request.Price, request.Currency, request.PricingType,
            request.EstimatedDeliveryDays, request.Category,
            request.ValidFrom, request.ValidUntil, request.IsActive));
        return Ok(result);
    }

    [HttpDelete("api/laboratories/price-items/{priceItemPublicId:guid}")]
    [RequirePermission("laboratory:manage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePriceItem(Guid priceItemPublicId)
    {
        await _mediator.Send(new DeleteLaboratoryPriceItemCommand(priceItemPublicId));
        return NoContent();
    }

    // ─── Onay yetkilileri ─────────────────────────────────────────────────
    [HttpGet("api/laboratories/approval-authorities")]
    [RequirePermission("laboratory:view")]
    [ProducesResponseType(typeof(IReadOnlyList<ApprovalAuthorityResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApprovalAuthorities()
        => Ok(await _mediator.Send(new GetApprovalAuthoritiesQuery()));

    [HttpPost("api/laboratories/approval-authorities")]
    [RequirePermission("laboratory:manage")]
    [ProducesResponseType(typeof(ApprovalAuthorityResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertApprovalAuthority([FromBody] UpsertApprovalAuthorityRequest request)
    {
        var result = await _mediator.Send(new UpsertApprovalAuthorityCommand(
            request.UserPublicId, request.BranchPublicId,
            request.CanApprove, request.CanReject, request.NotificationEnabled));
        return Ok(result);
    }

    [HttpDelete("api/laboratories/approval-authorities/{authorityPublicId:guid}")]
    [RequirePermission("laboratory:manage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveApprovalAuthority(Guid authorityPublicId)
    {
        await _mediator.Send(new RemoveApprovalAuthorityCommand(authorityPublicId));
        return NoContent();
    }

    // ─── İş emirleri ──────────────────────────────────────────────────────
    [HttpGet("api/laboratory-works")]
    [RequirePermission("laboratory:view")]
    [ProducesResponseType(typeof(LaboratoryWorksPage), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorks(
        [FromQuery] string?   status             = null,
        [FromQuery] Guid?     laboratoryPublicId = null,
        [FromQuery] Guid?     patientPublicId    = null,
        [FromQuery] Guid?     doctorPublicId     = null,
        [FromQuery] Guid?     branchPublicId     = null,
        [FromQuery] DateTime? fromDate           = null,
        [FromQuery] DateTime? toDate             = null,
        [FromQuery] string?   search             = null,
        [FromQuery] int       page               = 1,
        [FromQuery] int       pageSize           = 50)
    {
        var result = await _mediator.Send(new GetLaboratoryWorksQuery(
            status, laboratoryPublicId, patientPublicId, doctorPublicId, branchPublicId,
            fromDate, toDate, search, page, pageSize));
        return Ok(result);
    }

    [HttpGet("api/laboratory-works/{publicId:guid}")]
    [RequirePermission("laboratory:view")]
    [ProducesResponseType(typeof(LaboratoryWorkDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkDetail(Guid publicId)
        => Ok(await _mediator.Send(new GetLaboratoryWorkDetailQuery(publicId)));

    [HttpPost("api/laboratory-works")]
    [RequirePermission("laboratory:work_create")]
    [ProducesResponseType(typeof(LaboratoryWorkDetailResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateWork([FromBody] CreateLabWorkRequest request)
    {
        var result = await _mediator.Send(new CreateLaboratoryWorkCommand(
            request.PatientPublicId, request.LaboratoryPublicId,
            request.TreatmentPlanItemPublicId, request.BranchPublicId, request.DoctorPublicId,
            request.WorkType, request.DeliveryType,
            request.ToothNumbers, request.ShadeColor, request.DoctorNotes,
            request.Items));
        return Created($"api/laboratory-works/{result.PublicId}", result);
    }

    [HttpPut("api/laboratory-works/{publicId:guid}")]
    [RequirePermission("laboratory:work_create")]
    [ProducesResponseType(typeof(LaboratoryWorkDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateWork(Guid publicId, [FromBody] UpdateLabWorkRequest request)
    {
        var result = await _mediator.Send(new UpdateLaboratoryWorkCommand(
            publicId, request.TreatmentPlanItemPublicId,
            request.WorkType, request.DeliveryType,
            request.ToothNumbers, request.ShadeColor, request.DoctorNotes));
        return Ok(result);
    }

    [HttpPost("api/laboratory-works/{publicId:guid}/transition")]
    [ProducesResponseType(typeof(LaboratoryWorkDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Transition(Guid publicId, [FromBody] TransitionWorkRequest request)
    {
        // İzin kontrolü action'a göre dinamik olarak yapılır (attribute'un dışında,
        // çünkü tek endpoint birden çok aksiyon servis ediyor).
        var result = await _mediator.Send(new TransitionLaboratoryWorkCommand(
            publicId, request.Action, request.Notes));
        return Ok(result);
    }
}

// ─── Request DTO'ları ─────────────────────────────────────────────────────
public record CreateLaboratoryRequest(
    string  Name,
    string? Code,
    string? Phone,
    string? Email,
    string? Website,
    string? Country,
    string? City,
    string? District,
    string? Address,
    string? ContactPerson,
    string? ContactPhone,
    string? WorkingDays,
    string? WorkingHours,
    string? PaymentTerms,
    int     PaymentDays,
    string? Notes
);

public record UpdateLaboratoryRequest(
    string  Name,
    string? Code,
    string? Phone,
    string? Email,
    string? Website,
    string? Country,
    string? City,
    string? District,
    string? Address,
    string? ContactPerson,
    string? ContactPhone,
    string? WorkingDays,
    string? WorkingHours,
    string? PaymentTerms,
    int     PaymentDays,
    string? Notes,
    bool    IsActive
);

public record AssignBranchRequest(Guid BranchPublicId, int Priority, bool IsActive);

public record UpsertPriceItemRequest(
    Guid?     PublicId,
    string    ItemName,
    string?   ItemCode,
    string?   Description,
    decimal   Price,
    string    Currency,
    string?   PricingType,
    int?      EstimatedDeliveryDays,
    string?   Category,
    DateOnly? ValidFrom,
    DateOnly? ValidUntil,
    bool      IsActive
);

public record UpsertApprovalAuthorityRequest(
    Guid  UserPublicId,
    Guid? BranchPublicId,
    bool  CanApprove,
    bool  CanReject,
    bool  NotificationEnabled
);

public record CreateLabWorkRequest(
    Guid    PatientPublicId,
    Guid    LaboratoryPublicId,
    Guid?   TreatmentPlanItemPublicId,
    Guid?   BranchPublicId,
    Guid?   DoctorPublicId,
    string  WorkType,
    string  DeliveryType,
    string? ToothNumbers,
    string? ShadeColor,
    string? DoctorNotes,
    IReadOnlyList<LabWorkItemInputDto> Items
);

public record UpdateLabWorkRequest(
    Guid?   TreatmentPlanItemPublicId,
    string  WorkType,
    string  DeliveryType,
    string? ToothNumbers,
    string? ShadeColor,
    string? DoctorNotes
);

public record TransitionWorkRequest(string Action, string? Notes);
