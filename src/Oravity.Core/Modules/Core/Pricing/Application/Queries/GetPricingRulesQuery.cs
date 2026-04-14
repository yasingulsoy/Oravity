using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Pricing.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Core.Pricing.Application.Queries;

public record GetPricingRulesQuery(
    bool ActiveOnly = true
) : IRequest<IReadOnlyList<PricingRuleResponse>>;

public class GetPricingRulesQueryHandler
    : IRequestHandler<GetPricingRulesQuery, IReadOnlyList<PricingRuleResponse>>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public GetPricingRulesQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<PricingRuleResponse>> Handle(
        GetPricingRulesQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, cancellationToken)
            ?? throw new ForbiddenException("Fiyatlandırma kurallarını listelemek için şirket bağlamı gereklidir.");

        var query = _db.PricingRules
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId);

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            query = query.Where(r => r.BranchId == null || r.BranchId == _tenant.BranchId.Value);

        if (request.ActiveOnly)
            query = query.Where(r => r.IsActive);

        var rules = await query
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return rules.Select(PricingMappings.ToResponse).ToList();
    }
}
