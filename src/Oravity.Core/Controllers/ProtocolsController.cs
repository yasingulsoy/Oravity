using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Core.Modules.Visit.Application.Commands;
using Oravity.Core.Modules.Visit.Application.Queries;

namespace Oravity.Core.Controllers;

[ApiController]
[Route("api/protocols")]
[Authorize]
[Produces("application/json")]
public class ProtocolsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProtocolsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Vizite için yeni protokol oluşturur.</summary>
    [HttpPost]
    [RequirePermission("protocol:create")]
    [ProducesResponseType(typeof(ProtocolDetailResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProtocolRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateProtocolCommand(req.VisitPublicId, req.DoctorId, req.ProtocolType), ct);
        return CreatedAtAction(nameof(GetDetail), new { publicId = result.PublicId }, result);
    }

    /// <summary>Protokolü tamamlar.</summary>
    [HttpPost("{publicId:guid}/complete")]
    [RequirePermission("protocol:update")]
    [ProducesResponseType(typeof(ProtocolDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(Guid publicId, CancellationToken ct)
    {
        var result = await _mediator.Send(new CompleteProtocolCommand(publicId), ct);
        return Ok(result);
    }

    /// <summary>Hekimin aktif protokolleri.</summary>
    [HttpGet("my")]
    [RequirePermission("protocol:view")]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorProtocolResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProtocols([FromQuery] long? doctorId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDoctorProtocolsQuery(doctorId), ct);
        return Ok(result);
    }

    /// <summary>Protokol detayı.</summary>
    [HttpGet("{publicId:guid}")]
    [RequirePermission("protocol:view")]
    [ProducesResponseType(typeof(ProtocolDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetail(Guid publicId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProtocolDetailQuery(publicId), ct);
        return Ok(result);
    }
}

// ─── Request DTO ──────────────────────────────────────────────────────────

public record CreateProtocolRequest(Guid VisitPublicId, long DoctorId, int ProtocolType);
