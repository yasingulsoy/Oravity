using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Core.Campaigns.Application.Commands;

public record CreateCampaignCommand(
    string    Code,
    string    Name,
    string?   Description,
    DateTime  ValidFrom,
    DateTime  ValidUntil,
    Guid?     LinkedRulePublicId
) : IRequest<CampaignResponse>;

public class CreateCampaignCommandHandler
    : IRequestHandler<CreateCampaignCommand, CampaignResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser   _user;

    public CreateCampaignCommandHandler(AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db     = db;
        _tenant = tenant;
        _user   = user;
    }

    public async Task<CampaignResponse> Handle(
        CreateCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, cancellationToken)
            ?? throw new ForbiddenException("Kampanya oluşturmak için şirket bağlamı gereklidir.");

        var codeNorm = request.Code.Trim().ToUpperInvariant();

        var exists = await _db.Campaigns.AsNoTracking()
            .AnyAsync(c => c.CompanyId == companyId && c.Code == codeNorm, cancellationToken);
        if (exists)
            throw new ConflictException($"'{codeNorm}' kodlu kampanya zaten mevcut.");

        var campaign = Campaign.Create(
            companyId,
            codeNorm,
            request.Name,
            request.Description,
            request.ValidFrom,
            request.ValidUntil,
            request.LinkedRulePublicId,
            _user.IsAuthenticated ? _user.UserId : null);

        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync(cancellationToken);

        return CampaignMappings.ToResponse(campaign);
    }
}
