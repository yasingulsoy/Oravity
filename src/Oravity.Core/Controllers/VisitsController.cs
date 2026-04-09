using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Core.Modules.Visit.Application.Commands;
using Oravity.Core.Modules.Visit.Application.Queries;

namespace Oravity.Core.Controllers;

[ApiController]
[Route("api/visits")]
[Authorize]
[Produces("application/json")]
public class VisitsController : ControllerBase
{
    private readonly IMediator _mediator;

    public VisitsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Randevusu olan hastayı check-in yapar.</summary>
    [HttpPost("check-in")]
    [RequirePermission("visit:create")]
    [ProducesResponseType(typeof(VisitResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CheckIn([FromBody] CheckInPatientRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new CheckInPatientCommand(req.AppointmentPublicId, req.Notes), ct);
        return CreatedAtAction(nameof(GetWaitingList), null, result);
    }

    /// <summary>Walk-in (randevusuz) hasta check-in.</summary>
    [HttpPost("walkin")]
    [RequirePermission("visit:create")]
    [ProducesResponseType(typeof(VisitResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> WalkIn([FromBody] CheckInWalkInRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new CheckInWalkInCommand(req.PatientId, req.Notes), ct);
        return CreatedAtAction(nameof(GetWaitingList), null, result);
    }

    /// <summary>Hastanın vizitesini tamamlar (check-out).</summary>
    [HttpPost("{publicId:guid}/checkout")]
    [RequirePermission("visit:update")]
    [ProducesResponseType(typeof(VisitResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckOut(Guid publicId, CancellationToken ct)
    {
        var result = await _mediator.Send(new CheckOutVisitCommand(publicId), ct);
        return Ok(result);
    }

    /// <summary>Güncel bekleme listesi (bugün, aktif viziteler).</summary>
    [HttpGet("waiting")]
    [RequirePermission("visit:view")]
    [ProducesResponseType(typeof(IReadOnlyList<WaitingListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWaitingList([FromQuery] long? branchId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWaitingListQuery(branchId), ct);
        return Ok(result);
    }
}

// ─── Request DTOs ──────────────────────────────────────────────────────────

public record CheckInPatientRequest(Guid AppointmentPublicId, string? Notes);
public record CheckInWalkInRequest(long PatientId, string? Notes);
