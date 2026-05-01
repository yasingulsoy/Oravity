using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Filters;
using Oravity.Core.Modules.InstitutionInvoice.Application;
using Oravity.Core.Modules.InstitutionInvoice.Application.Commands;
using Oravity.Core.Modules.InstitutionInvoice.Application.Queries;
using Oravity.Core.Modules.InvoiceIntegration;
using Oravity.Core.Services;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using System.Text.Json;

namespace Oravity.Core.Controllers;

/// <summary>
/// Kurum fatura ve ödemeleri yönetimi.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class InstitutionInvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly InvoiceIntegratorFactory _integratorFactory;
    private readonly InvoicePdfService _pdfService;

    public InstitutionInvoicesController(
        IMediator mediator, AppDbContext db, ITenantContext tenant,
        InvoiceIntegratorFactory integratorFactory,
        InvoicePdfService pdfService)
    {
        _mediator = mediator;
        _db = db;
        _tenant = tenant;
        _integratorFactory = integratorFactory;
        _pdfService = pdfService;
    }

    /// <summary>
    /// Belirtilen hasta + kurum için faturaya eklenebilecek tedavi kalemlerini döner.
    /// Koşullar: tamamlanmış (Completed/Approved), kuruma ait, InstitutionContributionAmount > 0,
    /// daha önce başka faturaya girmemiş.
    /// </summary>
    [HttpGet("api/institution-invoices/billable-items")]
    [RequirePermission("institution_invoice:create")]
    [ProducesResponseType(typeof(List<BillableItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBillableItems(
        [FromQuery] long patientId,
        [FromQuery] long institutionId,
        CancellationToken ct)
    {
        // Mevcut faturalardaki item ID'lerini topla
        var existingInvoices = await _db.InstitutionInvoices
            .AsNoTracking()
            .Where(i => i.PatientId == patientId
                     && i.InstitutionId == institutionId
                     && i.Status != InstitutionInvoiceStatus.Rejected
                     && i.Status != InstitutionInvoiceStatus.Cancelled)
            .Select(i => i.TreatmentItemIdsJson)
            .ToListAsync(ct);

        var alreadyBilled = existingInvoices
            .Where(j => j != null)
            .SelectMany(j => JsonSerializer.Deserialize<List<long>>(j!) ?? [])
            .ToHashSet();

        // Faturaya girebilecek kalemleri getir — kurum kalem seviyesinde tutulur
        var items = await _db.TreatmentPlanItems
            .AsNoTracking()
            .Include(x => x.Plan)
            .Include(x => x.Treatment)
            .Where(x => x.Plan.PatientId == patientId
                     && x.InstitutionId == institutionId
                     && (x.Status == TreatmentItemStatus.Completed || x.Status == TreatmentItemStatus.Approved)
                     && x.InstitutionContributionAmount != null
                     && x.InstitutionContributionAmount > 0
                     && !alreadyBilled.Contains(x.Id))
            .OrderBy(x => x.CompletedAt)
            .Select(x => new BillableItemResponse(
                x.Id,
                x.Treatment != null ? x.Treatment.Name : "Bilinmeyen Tedavi",
                x.ToothNumber,
                x.CompletedAt,
                x.InstitutionContributionAmount!.Value,
                x.PriceCurrency,
                x.Plan.PublicId,
                x.Plan.BranchId))
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>
    /// Şube entegratör ayarlarına göre bir sonraki fatura numarasını üretir.
    /// Entegratör yoksa yerel sayaç kullanır.
    /// </summary>
    [HttpGet("api/institution-invoices/next-number")]
    [RequirePermission("institution_invoice:create")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNextNumber(
        [FromQuery] string type = "EARCHIVE",
        [FromQuery] long? branchId = null,
        CancellationToken ct = default)
    {
        var resolvedBranchId = branchId ?? _tenant.BranchId
            ?? throw new ForbiddenException("Şube bağlamı gereklidir.");

        var normalizedType = type.ToUpperInvariant();
        var integrator = await _integratorFactory.GetForBranchAsync(resolvedBranchId, normalizedType, ct);
        var result = await integrator.GenerateInvoiceNumberAsync(
            new GenerateInvoiceNumberRequest(resolvedBranchId, normalizedType, DateOnly.FromDateTime(DateTime.UtcNow)),
            ct);

        return Ok(new { number = result.InvoiceNo, uuid = result.ExternalUuid });
    }

    [HttpGet("api/institution-invoices")]
    [RequirePermission("institution_invoice:view")]
    [ProducesResponseType(typeof(PagedInvoiceResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] InstitutionInvoiceStatus? status = null,
        [FromQuery] long? institutionId = null,
        [FromQuery] long? patientId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _mediator.Send(new GetInstitutionInvoicesQuery(
            status, institutionId, patientId, from, to, page, pageSize));
        return Ok(result);
    }

    [HttpGet("api/institution-invoices/{publicId:guid}")]
    [RequirePermission("institution_invoice:view")]
    [ProducesResponseType(typeof(InstitutionInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid publicId)
    {
        var result = await _mediator.Send(new GetInstitutionInvoiceByIdQuery(publicId));
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("api/institution-invoices")]
    [RequirePermission("institution_invoice:create")]
    [ProducesResponseType(typeof(InstitutionInvoiceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateInstitutionInvoiceRequest r, CancellationToken ct)
    {
        // Kurumun tevkifat/e-fatura ayarlarını oku
        var institution = await _db.Institutions.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == r.InstitutionId, ct);

        var result = await _mediator.Send(new CreateInstitutionInvoiceCommand(
            r.PatientId, r.InstitutionId, r.InvoiceNo,
            r.InvoiceDate, r.DueDate, r.Amount, r.Currency ?? "TRY",
            r.TreatmentItemIds, r.Notes,
            institution?.IsEInvoiceTaxpayer ?? false,
            institution?.WithholdingApplies ?? false,
            institution?.WithholdingCode,
            institution?.WithholdingNumerator ?? 5,
            institution?.WithholdingDenominator ?? 10));
        return Created($"api/institution-invoices/{result.PublicId}", result);
    }

    [HttpPost("api/institution-invoices/{publicId:guid}/payments")]
    [RequirePermission("institution_invoice:payment")]
    [ProducesResponseType(typeof(InstitutionPaymentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterPayment(Guid publicId, [FromBody] RegisterInstitutionPaymentRequest r)
    {
        var result = await _mediator.Send(new RegisterInstitutionPaymentCommand(
            publicId, r.Amount, r.PaymentDate, r.Method,
            r.ReferenceNo, r.Notes, r.Currency ?? "TRY", r.BankAccountPublicId));
        return Created($"api/institution-invoices/{publicId}/payments/{result.PublicId}", result);
    }

    [HttpPost("api/institution-invoices/{publicId:guid}/cancel")]
    [RequirePermission("institution_invoice:cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid publicId, [FromBody] CancelInvoiceRequest r)
    {
        await _mediator.Send(new CancelInstitutionInvoiceCommand(publicId, r.Reason));
        return NoContent();
    }

    [HttpPost("api/institution-invoices/{publicId:guid}/reject")]
    [RequirePermission("institution_invoice:cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(Guid publicId, [FromBody] RejectInvoiceRequest r)
    {
        await _mediator.Send(new RejectInstitutionInvoiceCommand(publicId, r.Reason));
        return NoContent();
    }

    [HttpPost("api/institution-invoices/{publicId:guid}/follow-up")]
    [RequirePermission("institution_invoice:follow_up")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> FollowUp(Guid publicId, [FromBody] StartFollowUpRequest r)
    {
        await _mediator.Send(new StartFollowUpCommand(publicId, r.Level, r.OnDate, r.NextDate));
        return NoContent();
    }

    [HttpPatch("api/institution-invoices/{publicId:guid}/notes")]
    [RequirePermission("institution_invoice:follow_up")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateNotes(Guid publicId, [FromBody] UpdateInvoiceNotesRequest r)
    {
        await _mediator.Send(new UpdateInvoiceNotesCommand(publicId, r.Notes));
        return NoContent();
    }

    [HttpGet("api/institution-invoices/{publicId:guid}/pdf")]
    [RequirePermission("institution_invoice:view")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid publicId, CancellationToken ct)
    {
        var bytes = await _pdfService.GenerateInstitutionInvoicePdfAsync(publicId, ct);
        return File(bytes, "application/pdf", $"kurum-fatura-{publicId:N}.pdf");
    }
}

public record CreateInstitutionInvoiceRequest(
    long PatientId,
    long InstitutionId,
    string InvoiceNo,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    decimal Amount,
    string? Currency,
    IReadOnlyList<long>? TreatmentItemIds,
    string? Notes
);

public record BillableItemResponse(
    long Id,
    string TreatmentName,
    string? ToothNumber,
    DateTime? CompletedAt,
    decimal InstitutionAmount,
    string Currency,
    Guid PlanPublicId,
    long BranchId
);

public record RegisterInstitutionPaymentRequest(
    decimal Amount,
    DateOnly PaymentDate,
    InstitutionPaymentMethod Method,
    string? ReferenceNo,
    string? Notes,
    string? Currency,
    string? BankAccountPublicId
);

public record CancelInvoiceRequest(string Reason);
public record RejectInvoiceRequest(string Reason);

public record StartFollowUpRequest(
    InstitutionInvoiceFollowUp Level,
    DateOnly OnDate,
    DateOnly? NextDate
);

public record UpdateInvoiceNotesRequest(string? Notes);
