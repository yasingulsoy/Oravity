using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Core.Campaigns.Application.Commands;

public record DeleteCampaignCommand(Guid PublicId) : IRequest;

public class DeleteCampaignCommandHandler : IRequestHandler<DeleteCampaignCommand>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public DeleteCampaignCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task Handle(
        DeleteCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, cancellationToken)
            ?? throw new ForbiddenException("Şirket bağlamı gerekli.");

        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.PublicId == request.PublicId && c.CompanyId == companyId, cancellationToken)
            ?? throw new NotFoundException("Kampanya bulunamadı.");

        campaign.SoftDelete();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
