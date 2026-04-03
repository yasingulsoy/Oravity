using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Finance.EInvoice.Application;
using Oravity.Core.Modules.Finance.EInvoice.Application.Commands;
using Oravity.Core.Modules.Finance.EInvoice.Application.Queries;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Controllers;

[ApiController]
[Route("api/einvoices")]
[Authorize]
[Tags("E-Fatura")]
public class EInvoiceController : ControllerBase
{
    private readonly IMediator _mediator;

    public EInvoiceController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// E-fatura / e-arşiv listesi. Tarih, durum, alıcı adı ve tür filtreleri destekler.
    /// </summary>
    [HttpGet]
    [RequirePermission("invoice:view")]
    [ProducesResponseType(typeof(EInvoicePagedResult), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] EInvoiceType? invoiceType  = null,
        [FromQuery] string?       gibStatus    = null,
        [FromQuery] bool?         isCancelled  = null,
        [FromQuery] DateTime?     from         = null,
        [FromQuery] DateTime?     to           = null,
        [FromQuery] string?       receiverName = null,
        [FromQuery] int           page         = 1,
        [FromQuery] int           pageSize     = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetEInvoicesQuery(invoiceType, gibStatus, isCancelled, from, to, receiverName, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Tek e-fatura detayı (kalemler dahil).
    /// </summary>
    [HttpGet("{publicId:guid}")]
    [RequirePermission("invoice:view")]
    [ProducesResponseType(typeof(EInvoiceDetail), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDetail(Guid publicId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEInvoiceDetailQuery(publicId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Hasta ödemesinden e-arşiv fatura oluşturur.
    /// Adapter (XML_EXPORT varsayılan): UBL-TR XML üretilir ve yanıtta döner.
    /// </summary>
    [HttpPost("earchive")]
    [RequirePermission("invoice:create")]
    [ProducesResponseType(typeof(CreateEArchiveResult), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateEArchive(
        [FromBody] CreateEArchiveBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateEArchiveCommand(
            PaymentId:      body.PaymentId,
            ReceiverName:   body.ReceiverName,
            ReceiverTc:     body.ReceiverTc,
            ReceiverAddress:body.ReceiverAddress,
            ReceiverEmail:  body.ReceiverEmail,
            Items:          body.Items), ct);

        return CreatedAtAction(nameof(GetDetail), new { publicId = result.PublicId }, result);
    }

    /// <summary>
    /// E-fatura / e-arşiv iptal eder.
    /// XML_EXPORT: yerel kayıt; entegratör: GİB iptal isteği gönderilir.
    /// </summary>
    [HttpPost("{publicId:guid}/cancel")]
    [RequirePermission("invoice:cancel")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Cancel(
        Guid publicId, [FromBody] CancelBody body, CancellationToken ct)
    {
        await _mediator.Send(new CancelEInvoiceCommand(publicId, body.Reason), ct);
        return NoContent();
    }

    /// <summary>
    /// E-fatura PDF'ini indirir.
    /// PDF yoksa (Aşama 1 — XML_EXPORT) 204 döner.
    /// </summary>
    [HttpGet("{publicId:guid}/pdf")]
    [RequirePermission("invoice:view")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DownloadPdf(Guid publicId, CancellationToken ct)
    {
        var pdf = await _mediator.Send(new GetEInvoicePdfQuery(publicId), ct);

        if (pdf is null || !System.IO.File.Exists(pdf.PdfPath))
            return NoContent();

        var bytes = await System.IO.File.ReadAllBytesAsync(pdf.PdfPath, ct);
        return File(bytes, "application/pdf", $"{pdf.EInvoiceNo ?? publicId.ToString()}.pdf");
    }

    // ─── Request bodies ───────────────────────────────────────────────────
    public record CreateEArchiveBody(
        long    PaymentId,
        string? ReceiverName    = null,
        string? ReceiverTc      = null,
        string? ReceiverAddress = null,
        string? ReceiverEmail   = null,
        IReadOnlyList<EInvoiceItemInput>? Items = null);

    public record CancelBody(string Reason);
}
