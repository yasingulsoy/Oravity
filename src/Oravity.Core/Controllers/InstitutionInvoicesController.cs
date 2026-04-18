using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.InstitutionInvoice.Application;
using Oravity.Core.Modules.InstitutionInvoice.Application.Commands;
using Oravity.Core.Modules.InstitutionInvoice.Application.Queries;
using Oravity.SharedKernel.Entities;

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

    public InstitutionInvoicesController(IMediator mediator) => _mediator = mediator;

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
    public async Task<IActionResult> Create([FromBody] CreateInstitutionInvoiceRequest r)
    {
        var result = await _mediator.Send(new CreateInstitutionInvoiceCommand(
            r.PatientId, r.InstitutionId, r.InvoiceNo,
            r.InvoiceDate, r.DueDate, r.Amount, r.Currency ?? "TRY",
            r.TreatmentItemIds, r.Notes));
        return Created($"api/institution-invoices/{result.PublicId}", result);
    }

    [HttpPost("api/institution-invoices/{publicId:guid}/payments")]
    [RequirePermission("institution_invoice:payment")]
    [ProducesResponseType(typeof(InstitutionPaymentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterPayment(Guid publicId, [FromBody] RegisterInstitutionPaymentRequest r)
    {
        var result = await _mediator.Send(new RegisterInstitutionPaymentCommand(
            publicId, r.Amount, r.PaymentDate, r.Method,
            r.ReferenceNo, r.Notes, r.Currency ?? "TRY"));
        return Created($"api/institution-invoices/{publicId}/payments/{result.PublicId}", result);
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

public record RegisterInstitutionPaymentRequest(
    decimal Amount,
    DateOnly PaymentDate,
    InstitutionPaymentMethod Method,
    string? ReferenceNo,
    string? Notes,
    string? Currency
);

public record RejectInvoiceRequest(string Reason);

public record StartFollowUpRequest(
    InstitutionInvoiceFollowUp Level,
    DateOnly OnDate,
    DateOnly? NextDate
);

public record UpdateInvoiceNotesRequest(string? Notes);
