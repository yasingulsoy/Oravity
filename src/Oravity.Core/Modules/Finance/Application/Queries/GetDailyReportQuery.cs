using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Finance.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Queries;

public record GetDailyReportQuery(
    DateOnly Date,
    long? BranchId = null
) : IRequest<DailyReportResponse>;

public class GetDailyReportQueryHandler
    : IRequestHandler<GetDailyReportQuery, DailyReportResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetDailyReportQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<DailyReportResponse> Handle(
        GetDailyReportQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Payments
            .AsNoTracking()
            .Where(p => p.PaymentDate == request.Date && !p.IsRefunded);

        // Tenant filtresi
        if (_tenant.IsPlatformAdmin)
        {
            if (request.BranchId.HasValue)
                query = query.Where(p => p.BranchId == request.BranchId.Value);
        }
        else if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
        {
            query = query.Where(p => p.BranchId == _tenant.BranchId.Value);
        }
        else if (_tenant.IsCompanyAdmin && _tenant.CompanyId.HasValue)
        {
            query = query.Where(p => p.Branch.CompanyId == _tenant.CompanyId.Value);
            if (request.BranchId.HasValue)
                query = query.Where(p => p.BranchId == request.BranchId.Value);
        }
        else
        {
            query = query.Where(_ => false);
        }

        var payments = await query
            .Select(p => new { p.Method, p.Amount })
            .ToListAsync(cancellationToken);

        var byMethod = payments
            .GroupBy(p => p.Method)
            .Select(g => new PaymentMethodTotal(
                g.Key,
                FinanceMappings.MethodLabel(g.Key),
                g.Sum(p => p.Amount),
                g.Count()))
            .OrderBy(m => (int)m.Method)
            .ToList();

        var branchId = request.BranchId ?? _tenant.BranchId ?? 0;

        return new DailyReportResponse(
            request.Date,
            branchId,
            byMethod,
            byMethod.Sum(m => m.Amount),
            byMethod.Sum(m => m.Count));
    }
}
