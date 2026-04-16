using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Core.Campaigns.Application.Queries;

public record GetCampaignsQuery(bool ActiveOnly = false) : IRequest<IReadOnlyList<CampaignResponse>>;

public class GetCampaignsQueryHandler
    : IRequestHandler<GetCampaignsQuery, IReadOnlyList<CampaignResponse>>
{
    private readonly AppDbContext  _db;
    private readonly ITenantContext _tenant;

    public GetCampaignsQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<CampaignResponse>> Handle(
        GetCampaignsQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, cancellationToken);
        if (companyId == null) return [];

        var q = _db.Campaigns.AsNoTracking()
            .Where(c => c.CompanyId == companyId.Value);

        if (request.ActiveOnly)
            q = q.Where(c => c.IsActive && c.ValidUntil >= DateTime.UtcNow);

        var campaigns = await q
            .OrderByDescending(c => c.ValidFrom)
            .ToListAsync(cancellationToken);

        return campaigns.Select(CampaignMappings.ToResponse).ToList();
    }
}
