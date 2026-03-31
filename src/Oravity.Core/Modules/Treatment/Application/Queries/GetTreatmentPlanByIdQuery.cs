using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Treatment.Application.Queries;

public record GetTreatmentPlanByIdQuery(Guid PublicId) : IRequest<TreatmentPlanResponse>;

public class GetTreatmentPlanByIdQueryHandler
    : IRequestHandler<GetTreatmentPlanByIdQuery, TreatmentPlanResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetTreatmentPlanByIdQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<TreatmentPlanResponse> Handle(
        GetTreatmentPlanByIdQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.TreatmentPlans
            .AsNoTracking()
            .Include(p => p.Items.Where(i => !i.IsDeleted))
            .Where(p => p.PublicId == request.PublicId);

        query = ApplyTenantFilter(query);

        var plan = await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Tedavi planı bulunamadı: {request.PublicId}");

        return TreatmentPlanMappings.ToResponse(plan);
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
