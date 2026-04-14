using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Treatment.Application.Queries;

public record GetPatientTreatmentPlansQuery(long PatientId) : IRequest<IReadOnlyList<TreatmentPlanResponse>>;

public class GetPatientTreatmentPlansQueryHandler
    : IRequestHandler<GetPatientTreatmentPlansQuery, IReadOnlyList<TreatmentPlanResponse>>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetPatientTreatmentPlansQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<TreatmentPlanResponse>> Handle(
        GetPatientTreatmentPlansQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.TreatmentPlans
            .AsNoTracking()
            .Include(p => p.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Treatment)
            .Where(p => p.PatientId == request.PatientId);

        query = ApplyTenantFilter(query);

        var plans = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return plans.Select(p => TreatmentPlanMappings.ToResponse(p)).ToList();
    }

    private IQueryable<TreatmentPlan> ApplyTenantFilter(IQueryable<TreatmentPlan> query)
    {
        if (_tenant.IsPlatformAdmin) return query;

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            return query.Where(p => p.BranchId == _tenant.BranchId.Value);

        if (_tenant.IsCompanyAdmin && _tenant.CompanyId.HasValue)
            return query.Where(p => p.Branch.CompanyId == _tenant.CompanyId.Value);

        return query.Where(_ => false);
    }
}
