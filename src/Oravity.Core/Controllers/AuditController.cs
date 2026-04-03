using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Audit.Application;
using Oravity.Core.Modules.Audit.Application.Commands;
using Oravity.Core.Modules.Audit.Application.Queries;

namespace Oravity.Core.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize]
[Tags("Audit & KVKK")]
public class AuditController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Sistem audit loglarını filtreli ve sayfalı listeler.
    /// Filtreler: entityType, entityId, userId, action, from, to.
    /// </summary>
    [HttpGet("logs")]
    [RequirePermission("audit:view")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId   = null,
        [FromQuery] long?   userId     = null,
        [FromQuery] string? action     = null,
        [FromQuery] DateTime? from     = null,
        [FromQuery] DateTime? to       = null,
        [FromQuery] int page           = 1,
        [FromQuery] int pageSize       = 50,
        CancellationToken ct           = default)
    {
        var result = await _mediator.Send(
            new GetAuditLogsQuery(entityType, entityId, userId, action, from, to, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Belirli bir hastaya ait tüm audit kayıtları.
    /// </summary>
    [HttpGet("patients/{patientPublicId}")]
    [RequirePermission("audit:view")]
    public async Task<IActionResult> GetPatientLogs(
        string patientPublicId,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct     = default)
    {
        var result = await _mediator.Send(
            new GetPatientAuditLogsQuery(patientPublicId, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// KVKK onay/ret kaydeder.
    /// </summary>
    [HttpPost("kvkk/consent")]
    public async Task<IActionResult> RecordConsent(
        [FromBody] KvkkConsentRequest body,
        CancellationToken ct = default)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(
            new RecordKvkkConsentCommand(body.PatientId, body.ConsentType, body.IsGiven,
                body.IpAddress ?? ipAddress), ct);
        return Ok(new { result.Id, result.Message });
    }

    /// <summary>
    /// KVKK veri erişim/taşıma talebi oluşturur.
    /// </summary>
    [HttpPost("export-requests")]
    public async Task<IActionResult> CreateExportRequest(
        [FromBody] ExportRequestBody body,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CreateDataExportRequestCommand(body.PatientId), ct);
        return Created($"api/audit/export-requests/{result.Id}", result);
    }

    public record ExportRequestBody(long PatientId);
}
