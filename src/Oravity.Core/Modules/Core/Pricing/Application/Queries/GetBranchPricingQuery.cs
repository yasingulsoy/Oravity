using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Core.Pricing.Application.Queries;

public record BranchPricingResponse(
    long    BranchId,
    string  BranchName,
    decimal PricingMultiplier
);

public record GetBranchPricingQuery : IRequest<IReadOnlyList<BranchPricingResponse>>;

public class GetBranchPricingQueryHandler
    : IRequestHandler<GetBranchPricingQuery, IReadOnlyList<BranchPricingResponse>>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public GetBranchPricingQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<BranchPricingResponse>> Handle(
        GetBranchPricingQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, cancellationToken)
            ?? throw new Oravity.SharedKernel.Exceptions.ForbiddenException("Şirket bağlamı gereklidir.");

        return await _db.Branches
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId && b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new BranchPricingResponse(b.Id, b.Name, b.PricingMultiplier))
            .ToListAsync(cancellationToken);
    }
}
