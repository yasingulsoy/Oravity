using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Survey.Application;
using Oravity.Core.Modules.Survey.Application.Commands;
using Oravity.Core.Modules.Survey.Application.Queries;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Controllers;

[ApiController]
[Route("api/complaints")]
[Authorize]
[Tags("Şikayet Yönetimi")]
public class ComplaintsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ComplaintsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Şikayetleri listeler — filtreler: status, priority, tarih aralığı.</summary>
    [HttpGet]
    [RequirePermission("complaint:view")]
    public async Task<IActionResult> GetComplaints(
        [FromQuery] ComplaintStatus? status = null,
        [FromQuery] ComplaintPriority? priority = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetComplaintsQuery(status, priority, from, to, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Yeni şikayet oluşturur.</summary>
    [HttpPost]
    [RequirePermission("complaint:create")]
    public async Task<IActionResult> CreateComplaint(
        [FromBody] CreateComplaintBody body,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CreateComplaintCommand(
            body.CompanyId, body.BranchId, body.PatientId,
            body.Source, body.Subject, body.Description,
            body.Priority), ct);
        return Created($"api/complaints/{result.PublicId}", result);
    }

    /// <summary>Şikayet durumunu günceller.</summary>
    [HttpPut("{id:guid}/status")]
    [RequirePermission("complaint:manage")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateStatusBody body,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new UpdateComplaintStatusCommand(id, body.Status, body.Resolution), ct);
        return Ok(result);
    }

    /// <summary>Şikayeti bir personele atar.</summary>
    [HttpPut("{id:guid}/assign")]
    [RequirePermission("complaint:manage")]
    public async Task<IActionResult> Assign(
        Guid id,
        [FromBody] AssignBody body,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new AssignComplaintCommand(id, body.AssignedToUserId), ct);
        return Ok(result);
    }

    /// <summary>Şikayete not ekler.</summary>
    [HttpPost("{id:guid}/notes")]
    [RequirePermission("complaint:manage")]
    public async Task<IActionResult> AddNote(
        Guid id,
        [FromBody] AddNoteBody body,
        CancellationToken ct = default)
    {
        var noteId = await _mediator.Send(
            new AddComplaintNoteCommand(id, body.Note, body.IsInternal), ct);
        return Ok(new { Id = noteId });
    }

    // ── request bodies ─────────────────────────────────────────────────────

    public record CreateComplaintBody(
        long CompanyId,
        long BranchId,
        long? PatientId,
        ComplaintSource Source,
        string Subject,
        string Description,
        ComplaintPriority Priority = ComplaintPriority.Normal
    );

    public record UpdateStatusBody(
        ComplaintStatus Status,
        string? Resolution = null
    );

    public record AssignBody(long AssignedToUserId);
    public record AddNoteBody(string Note, bool IsInternal = true);
}
