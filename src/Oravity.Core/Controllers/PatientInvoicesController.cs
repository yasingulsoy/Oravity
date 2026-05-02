using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.InvoiceIntegration;
using Oravity.Core.Modules.PatientInvoice.Application;
using Oravity.Core.Modules.PatientInvoice.Application.Commands;
using Oravity.Core.Modules.PatientInvoice.Application.Queries;
using Oravity.Core.Services;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Controllers;

/// <summary>
/// Hastaya kesilen fatura yönetimi (e-Arşiv / e-Fatura).
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class PatientInvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantContext _tenant;
    private readonly InvoiceIntegratorFactory _integratorFactory;
    private readonly InvoicePdfService _pdfService;

    public PatientInvoicesController(
        IMediator mediator,
        ITenantContext tenant,
        InvoiceIntegratorFactory integratorFactory,
        InvoicePdfService pdfService)
    {
        _mediator = mediator;
        _tenant = tenant;
        _integratorFactory = integratorFactory;
        _pdfService = pdfService;
    }

    /// <summary>
    /// Şube entegratör ayarına göre hasta faturası için bir sonraki fatura numarasını döner.
    /// </summary>
    [HttpGet("api/patient-invoices/next-number")]
    [RequirePermission("patient_invoice:create")]
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

    [HttpGet("api/patient-invoices")]
    [RequirePermission("patient_invoice:view")]
    [ProducesResponseType(typeof(PagedPatientInvoiceResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] PatientInvoiceStatus? status = null,
        [FromQuery] long? patientId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _mediator.Send(new GetPatientInvoicesQuery(
            status, patientId, from, to, page, pageSize));
        return Ok(result);
    }

    [HttpGet("api/patient-invoices/{publicId:guid}")]
    [RequirePermission("patient_invoice:view")]
    [ProducesResponseType(typeof(PatientInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid publicId)
    {
        var result = await _mediator.Send(new GetPatientInvoiceByIdQuery(publicId));
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("api/patient-invoices")]
    [RequirePermission("patient_invoice:create")]
    [ProducesResponseType(typeof(PatientInvoiceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePatientInvoiceRequest r, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreatePatientInvoiceCommand(
            r.PatientId, r.InvoiceNo, r.InvoiceType ?? "EARCHIVE",
            r.InvoiceDate, r.DueDate, r.Amount, r.KdvRate ?? 0.10m,
            r.Currency ?? "TRY",
            r.RecipientType, r.RecipientName,
            r.RecipientTcNo, r.RecipientVkn, r.RecipientTaxOffice,
            r.TreatmentItemIds, r.Notes));
        return Created($"api/patient-invoices/{result.PublicId}", result);
    }

    [HttpPost("api/patient-invoices/{publicId:guid}/cancel")]
    [RequirePermission("patient_invoice:cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid publicId, [FromBody] CancelPatientInvoiceRequest r, CancellationToken ct)
    {
        await _mediator.Send(new CancelPatientInvoiceCommand(publicId, r.Reason));
        return NoContent();
    }

    [HttpGet("api/patient-invoices/{publicId:guid}/pdf")]
    [RequirePermission("patient_invoice:view")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid publicId, CancellationToken ct)
    {
        var bytes = await _pdfService.GeneratePatientInvoicePdfAsync(publicId, ct);
        return File(bytes, "application/pdf", $"fatura-{publicId:N}.pdf");
    }
}

public record CreatePatientInvoiceRequest(
    long PatientId,
    string InvoiceNo,
    string? InvoiceType,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    decimal Amount,
    decimal? KdvRate,
    string? Currency,
    InvoiceRecipientType RecipientType,
    string RecipientName,
    string? RecipientTcNo,
    string? RecipientVkn,
    string? RecipientTaxOffice,
    IReadOnlyList<long>? TreatmentItemIds,
    string? Notes
);

public record CancelPatientInvoiceRequest(string? Reason);
