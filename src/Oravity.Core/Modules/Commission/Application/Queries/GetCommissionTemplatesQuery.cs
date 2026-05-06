using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Commission.Application.Queries;

public record GetCommissionTemplatesQuery(bool ActiveOnly = false)
    : IRequest<IReadOnlyList<CommissionTemplateResponse>>;

public class GetCommissionTemplatesQueryHandler
    : IRequestHandler<GetCommissionTemplatesQuery, IReadOnlyList<CommissionTemplateResponse>>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetCommissionTemplatesQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<CommissionTemplateResponse>> Handle(
        GetCommissionTemplatesQuery r, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct);
        if (companyId == null) return [];

        var q = _db.DoctorCommissionTemplates.AsNoTracking()
            .Include(t => t.JobStartPrices)
            .Include(t => t.PriceRanges)
            .Where(t => t.CompanyId == companyId.Value);

        if (r.ActiveOnly) q = q.Where(t => t.IsActive);

        var list = await q.OrderBy(t => t.Name).ToListAsync(ct);
        return list.Select(CommissionMappings.ToResponse).ToList();
    }
}

public record GetCommissionTemplateByIdQuery(Guid PublicId)
    : IRequest<CommissionTemplateResponse?>;

public class GetCommissionTemplateByIdQueryHandler
    : IRequestHandler<GetCommissionTemplateByIdQuery, CommissionTemplateResponse?>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetCommissionTemplateByIdQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<CommissionTemplateResponse?> Handle(
        GetCommissionTemplateByIdQuery r, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct);
        if (companyId == null) return null;

        var t = await _db.DoctorCommissionTemplates.AsNoTracking()
            .Include(t => t.JobStartPrices)
            .Include(t => t.PriceRanges)
            .FirstOrDefaultAsync(x => x.PublicId == r.PublicId && x.CompanyId == companyId.Value, ct);

        return t == null ? null : CommissionMappings.ToResponse(t);
    }
}
