using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Core.Campaigns.Application.Commands;

public record UpdateCampaignCommand(
    Guid      PublicId,
    string    Name,
    string?   Description,
    DateTime  ValidFrom,
    DateTime  ValidUntil,
    bool      IsActive,
    Guid?     LinkedRulePublicId
) : IRequest<CampaignResponse>;

public class UpdateCampaignCommandHandler
    : IRequestHandler<UpdateCampaignCommand, CampaignResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public UpdateCampaignCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<CampaignResponse> Handle(
        UpdateCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, cancellationToken)
            ?? throw new ForbiddenException("Şirket bağlamı gerekli.");

        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.PublicId == request.PublicId && c.CompanyId == companyId, cancellationToken)
            ?? throw new NotFoundException("Kampanya bulunamadı.");

        campaign.Update(
            request.Name,
            request.Description,
            DateTime.SpecifyKind(request.ValidFrom, DateTimeKind.Utc),
            DateTime.SpecifyKind(request.ValidUntil, DateTimeKind.Utc),
            request.LinkedRulePublicId);

        campaign.SetActive(request.IsActive);
        await _db.SaveChangesAsync(cancellationToken);

        return CampaignMappings.ToResponse(campaign);
    }
}
