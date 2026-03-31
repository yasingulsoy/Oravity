using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Finance.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Queries;

public record GetDoctorCommissionsQuery(
    long? DoctorId,
    DateOnly? From,
    DateOnly? To,
    CommissionStatus? Status,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedCommissionResult>;

public record PagedCommissionResult(
    IReadOnlyList<DoctorCommissionResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    decimal TotalCommissionAmount
);

public class GetDoctorCommissionsQueryHandler
    : IRequestHandler<GetDoctorCommissionsQuery, PagedCommissionResult>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetDoctorCommissionsQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PagedCommissionResult> Handle(
        GetDoctorCommissionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.DoctorCommissions.AsNoTracking();

        // Tenant filtresi
        if (!_tenant.IsPlatformAdmin)
        {
            if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
                query = query.Where(c => c.BranchId == _tenant.BranchId.Value);
            else if (_tenant.IsCompanyAdmin && _tenant.CompanyId.HasValue)
                query = query.Where(c => c.Branch.CompanyId == _tenant.CompanyId.Value);
            else
                query = query.Where(_ => false);
        }

        if (request.DoctorId.HasValue)
            query = query.Where(c => c.DoctorId == request.DoctorId.Value);

        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status.Value);

        if (request.From.HasValue)
        {
            var fromUtc = request.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(c => c.CreatedAt >= fromUtc);
        }
        if (request.To.HasValue)
        {
            var toUtc = request.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(c => c.CreatedAt <= toUtc);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalAmount = await query.SumAsync(c => (decimal?)c.CommissionAmount, cancellationToken) ?? 0m;

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => FinanceMappings.ToResponse(c))
            .ToListAsync(cancellationToken);

        return new PagedCommissionResult(items, totalCount, request.Page, request.PageSize, totalAmount);
    }
}
