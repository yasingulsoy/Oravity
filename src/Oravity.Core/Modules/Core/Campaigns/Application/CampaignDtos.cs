using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Core.Campaigns.Application;

public record CampaignResponse(
    Guid      PublicId,
    string    Code,
    string    Name,
    string?   Description,
    DateTime  ValidFrom,
    DateTime  ValidUntil,
    bool      IsActive,
    Guid?     LinkedRulePublicId,
    DateTime  CreatedAt
);

public static class CampaignMappings
{
    public static CampaignResponse ToResponse(Campaign c)
        => new(c.PublicId, c.Code, c.Name, c.Description,
               c.ValidFrom, c.ValidUntil, c.IsActive,
               c.LinkedRulePublicId, c.CreatedAt);
}
