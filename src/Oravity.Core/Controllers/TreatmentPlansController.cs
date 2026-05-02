using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Core.Modules.Treatment.Application.Commands;
using Oravity.Core.Modules.Treatment.Application.Queries;
using Oravity.Core.Services;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

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
    private readonly IMediator               _mediator;
    private readonly AppDbContext            _db;
    private readonly ITenantContext          _tenant;
    private readonly TreatmentPlanPdfService _pdfService;

    public TreatmentPlansController(IMediator mediator, AppDbContext db, ITenantContext tenant, TreatmentPlanPdfService pdfService)
    {
        _mediator   = mediator;
        _db         = db;
        _tenant     = tenant;
        _pdfService = pdfService;
    }

    // ── Sorgular ─────────────────────────────────────────────────────────

    /// <summary>Hastanın tüm tedavi planlarını listeler (item'larla birlikte).</summary>
    [HttpGet("api/patients/{patientPublicId:guid}/treatment-plans")]
    [RequirePermission("treatment_plan:view")]
    [ProducesResponseType(typeof(IReadOnlyList<TreatmentPlanResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPatient(Guid patientPublicId)
    {
        var patient = await _db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PublicId == patientPublicId)
            ?? throw new NotFoundException("Hasta bulunamadı.");

        var result = await _mediator.Send(new GetPatientTreatmentPlansQuery(patient.Id));
        return Ok(result);
    }

    /// <summary>Tedavi planını public_id ile getirir.</summary>
    [HttpGet("api/treatment-plans/{id:guid}")]
    [RequirePermission("treatment_plan:view")]
    [ProducesResponseType(typeof(TreatmentPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetTreatmentPlanByIdQuery(id));
        return Ok(result);
    }

    /// <summary>Tedavi planını PDF olarak indirir. currency parametresi ile döviz seçilebilir (TRY, USD, EUR, CHF, GBP).</summary>
    [HttpGet("api/treatment-plans/{id:guid}/pdf")]
    [RequirePermission("treatment_plan:view")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid id, [FromQuery] string? currency = null)
    {
        try
        {
            var bytes = await _pdfService.GenerateAsync(id, currency);
            return File(bytes, "application/pdf", $"tedavi-plani-{id:N}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.GetType().Name, message = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    // ── Plan Komutları ────────────────────────────────────────────────────

    /// <summary>Yeni tedavi planı oluşturur (Taslak).</summary>
    [HttpPost("api/treatment-plans")]
    [RequirePermission("treatment_plan:create")]
    [ProducesResponseType(typeof(TreatmentPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateTreatmentPlanRequest request)
    {
        var patient = await _db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PublicId == request.PatientPublicId)
            ?? throw new NotFoundException("Hasta bulunamadı.");

        var doctor = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.PublicId == request.DoctorPublicId)
            ?? throw new NotFoundException("Hekim bulunamadı.");

        // Şube: istekte geliyorsa onu kullan; yoksa JWT'den; yoksa hekimin atamasından çöz
        long branchId;
        if (request.BranchPublicId.HasValue)
        {
            var branch = await _db.Branches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.PublicId == request.BranchPublicId.Value)
                ?? throw new NotFoundException("Şube bulunamadı.");
            branchId = branch.Id;
        }
        else if (_tenant.BranchId.HasValue)
        {
            branchId = _tenant.BranchId.Value;
        }
        else
        {
            var assignment = await _db.UserRoleAssignments.AsNoTracking()
                .Where(a => a.UserId == doctor.Id && a.IsActive && a.BranchId != null)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (assignment?.BranchId != null)
            {
                branchId = assignment.BranchId.Value;
            }
            else
            {
                // Son çare: şirkete ait ilk aktif şube
                var firstBranch = await _db.Branches.AsNoTracking()
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Id)
                    .FirstOrDefaultAsync()
                    ?? throw new NotFoundException("Sistem aktif şube içermiyor.");
                branchId = firstBranch.Id;
            }
        }

        // Kurum: istekte geliyorsa onu kullan; yoksa hastanın anlaşmalı kurumunu otomatik ata
        long? institutionId = null;
        if (request.InstitutionPublicId.HasValue)
        {
            var inst = await _db.Institutions.AsNoTracking()
                .FirstOrDefaultAsync(i => i.PublicId == request.InstitutionPublicId.Value)
                ?? throw new NotFoundException("Kurum bulunamadı.");
            institutionId = inst.Id;
        }
        else if (patient.AgreementInstitutionId.HasValue)
        {
            institutionId = patient.AgreementInstitutionId;
        }

        var result = await _mediator.Send(new CreateTreatmentPlanCommand(
            patient.Id,
            branchId,
            doctor.Id,
            request.Name,
            request.Notes,
            institutionId));

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Taslak planı onaylar. Plan ve tüm kalemleri Onaylandı olur.</summary>
    [HttpPut("api/treatment-plans/{id:guid}/approve")]
    [RequirePermission("treatment_plan:edit")]
    [ProducesResponseType(typeof(TreatmentPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _mediator.Send(new ApproveTreatmentPlanCommand(id));
        return Ok(result);
    }

    /// <summary>Seçili kalemleri onaylar. Plan durumunu değiştirmez.</summary>
    [HttpPut("api/treatment-plans/{id:guid}/items/approve")]
    [RequirePermission("treatment_plan:edit")]
    [ProducesResponseType(typeof(TreatmentPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveItems(Guid id, [FromBody] ApproveItemsRequest request)
    {
        var result = await _mediator.Send(new ApproveItemsCommand(id, request.ItemPublicIds));
        return Ok(result);
    }

    // ── Kalem Komutları ───────────────────────────────────────────────────

    /// <summary>Plana yeni tedavi kalemi ekler.</summary>
    [HttpPost("api/treatment-plans/{id:guid}/items")]
    [RequirePermission("treatment_plan:create")]
    [ProducesResponseType(typeof(TreatmentPlanItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddTreatmentPlanItemRequest request)
    {
        var treatment = await _db.Treatments.AsNoTracking()
            .FirstOrDefaultAsync(t => t.PublicId == request.TreatmentPublicId)
            ?? throw new NotFoundException("Tedavi bulunamadı.");

        var result = await _mediator.Send(new AddTreatmentPlanItemCommand(
            id,
            treatment.Id,
            request.UnitPrice,
            request.DiscountRate,
            request.ToothNumber,
            null,
            null,
            null,
            request.Notes,
            request.PriceCurrency,
            request.PriceExchangeRate,
            request.ListPrice));

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Tedavi kalemini tamamlandı olarak işaretler.</summary>
    [HttpPut("api/treatment-plans/{id:guid}/items/{itemId:guid}/complete")]
    [RequirePermission("treatment_plan:complete")]
    [ProducesResponseType(typeof(TreatmentPlanItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteItem(Guid id, Guid itemId)
    {
        var result = await _mediator.Send(new CompleteTreatmentPlanItemCommand(itemId));
        return Ok(result);
    }

    /// <summary>
    /// Onaylanmış tedavi kalemini 'Planlandı' durumuna geri alır.
    /// Kural: İmzalı onam yoksa serbest. İzin: treatment_plan.edit
    /// </summary>
    [HttpPut("api/treatment-plans/{id:guid}/items/{itemId:guid}/revert-to-planned")]
    [RequirePermission("treatment_plan.edit")]
    [ProducesResponseType(typeof(TreatmentPlanItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevertToPlanned(Guid id, Guid itemId)
    {
        var result = await _mediator.Send(new RevertApprovedTreatmentPlanItemCommand(itemId));
        return Ok(result);
    }

    /// <summary>
    /// Tamamlanmış tedavi kalemini 'Onaylandı' durumuna geri alır.
    /// Kural: Ödeme tahsisi yoksa ve izin varsa geri alınabilir. Reason zorunlu.
    /// </summary>
    [HttpPut("api/treatment-plans/{id:guid}/items/{itemId:guid}/revert")]
    [RequirePermission("treatment_plan.revert_completed")]
    [ProducesResponseType(typeof(TreatmentPlanItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevertItem(Guid id, Guid itemId, [FromBody] RevertItemRequest request)
    {
        var result = await _mediator.Send(new RevertTreatmentPlanItemCommand(itemId, request.Reason));
        return Ok(result);
    }

    /// <summary>Planlanmış (status=1) tedavi kalemini siler.</summary>
    [HttpDelete("api/treatment-plans/{id:guid}/items/{itemId:guid}")]
    [RequirePermission("treatment_plan:delete_planned")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(Guid id, Guid itemId)
    {
        await _mediator.Send(new DeletePlannedTreatmentCommand(itemId));
        return NoContent();
    }

    /// <summary>Tedavi planının adını / notlarını günceller.</summary>
    [HttpPut("api/treatment-plans/{id:guid}")]
    [RequirePermission("treatment_plan:edit")]
    [ProducesResponseType(typeof(TreatmentPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTreatmentPlanRequest request)
    {
        long? institutionId = null;
        if (request.InstitutionPublicId.HasValue)
        {
            var inst = await _db.Institutions.AsNoTracking()
                .FirstOrDefaultAsync(i => i.PublicId == request.InstitutionPublicId.Value)
                ?? throw new NotFoundException("Kurum bulunamadı.");
            institutionId = inst.Id;
        }

        var result = await _mediator.Send(new UpdateTreatmentPlanCommand(id, request.Name, request.Notes, institutionId));
        return Ok(result);
    }

    /// <summary>Tedavi planını siler (Taslak → soft-delete, diğerleri → iptal).</summary>
    [HttpDelete("api/treatment-plans/{id:guid}")]
    [RequirePermission("treatment_plan:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteTreatmentPlanCommand(id));
        return NoContent();
    }

    /// <summary>Kalem fiyatını / iskontosunu günceller.</summary>
    [HttpPut("api/treatment-plans/{id:guid}/items/{itemId:guid}")]
    [RequirePermission("treatment_plan:edit")]
    [ProducesResponseType(typeof(TreatmentPlanItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(Guid id, Guid itemId, [FromBody] UpdateTreatmentPlanItemRequest request)
    {
        var result = await _mediator.Send(new UpdateTreatmentPlanItemCommand(itemId, request.UnitPrice, request.DiscountRate, request.ToothNumber));
        return Ok(result);
    }

    /// <summary>Resepsiyonun TZH'den aldığı onaya göre kurum katkı tutarını girer.</summary>
    [HttpPut("api/treatment-plans/{id:guid}/items/{itemId:guid}/contribution")]
    [RequirePermission("treatment_plan:edit")]
    [ProducesResponseType(typeof(TreatmentPlanItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetContribution(Guid id, Guid itemId, [FromBody] SetInstitutionContributionRequest request)
    {
        var result = await _mediator.Send(new SetInstitutionContributionCommand(itemId, request.ContributionAmount, request.InstitutionId));
        return Ok(result);
    }
}

// ─── Request DTO'lar ───────────────────────────────────────────────────────

public record CreateTreatmentPlanRequest(
    Guid    PatientPublicId,
    Guid    DoctorPublicId,
    string  Name,
    string? Notes,
    Guid?   BranchPublicId      = null,
    Guid?   InstitutionPublicId = null
);

public record AddTreatmentPlanItemRequest(
    Guid     TreatmentPublicId,
    decimal  UnitPrice,
    decimal  DiscountRate,
    string?  ToothNumber,
    string?  Notes,
    string   PriceCurrency     = "TRY",
    decimal  PriceExchangeRate = 1m,
    decimal? ListPrice         = null
);

public record UpdateTreatmentPlanRequest(
    string  Name,
    string? Notes,
    Guid?   InstitutionPublicId = null
);

public record UpdateTreatmentPlanItemRequest(
    decimal UnitPrice,
    decimal DiscountRate,
    string? ToothNumber
);

public record ApproveItemsRequest(List<Guid> ItemPublicIds);

public record RevertItemRequest(string Reason);

public record SetInstitutionContributionRequest(decimal? ContributionAmount, long? InstitutionId = null);
