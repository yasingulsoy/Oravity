using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Core.Campaigns.Application;
using Oravity.Core.Modules.Core.Campaigns.Application.Commands;
using Oravity.Core.Modules.Core.Campaigns.Application.Queries;

namespace Oravity.Core.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
public class CampaignsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CampaignsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("api/campaigns")]
    [RequirePermission("pricing:view")]
    [ProducesResponseType(typeof(IReadOnlyList<CampaignResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCampaigns([FromQuery] bool activeOnly = false)
    {
        var result = await _mediator.Send(new GetCampaignsQuery(activeOnly));
        return Ok(result);
    }

    [HttpPost("api/campaigns")]
    [RequirePermission("pricing:create")]
    [ProducesResponseType(typeof(CampaignResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request)
    {
        var result = await _mediator.Send(new CreateCampaignCommand(
            request.Code, request.Name, request.Description,
            request.ValidFrom, request.ValidUntil, request.LinkedRulePublicId));
        return Created($"api/campaigns/{result.PublicId}", result);
    }

    [HttpPut("api/campaigns/{publicId:guid}")]
    [RequirePermission("pricing:edit")]
    [ProducesResponseType(typeof(CampaignResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCampaign(Guid publicId, [FromBody] UpdateCampaignRequest request)
    {
        var result = await _mediator.Send(new UpdateCampaignCommand(
            publicId, request.Name, request.Description,
            request.ValidFrom, request.ValidUntil, request.IsActive,
            request.LinkedRulePublicId));
        return Ok(result);
    }

    [HttpDelete("api/campaigns/{publicId:guid}")]
    [RequirePermission("pricing:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCampaign(Guid publicId)
    {
        await _mediator.Send(new DeleteCampaignCommand(publicId));
        return NoContent();
    }
}

public record CreateCampaignRequest(
    string    Code,
    string    Name,
    string?   Description,
    DateTime  ValidFrom,
    DateTime  ValidUntil,
    Guid?     LinkedRulePublicId
);

public record UpdateCampaignRequest(
    string    Name,
    string?   Description,
    DateTime  ValidFrom,
    DateTime  ValidUntil,
    bool      IsActive,
    Guid?     LinkedRulePublicId
);
