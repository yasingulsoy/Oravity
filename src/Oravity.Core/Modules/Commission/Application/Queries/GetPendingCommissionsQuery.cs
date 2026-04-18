using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Commission.Application.Queries;

public record PendingCommissionResponse(
    long Id,
    long DoctorId,
    string DoctorName,
    long TreatmentPlanItemId,
    long? TreatmentId,
    string? TreatmentName,
    long BranchId,
    decimal GrossAmount,
    decimal NetBaseAmount,
    decimal CommissionRate,
    decimal CommissionAmount,
    decimal NetCommissionAmount,
    bool BonusApplied,
    int PeriodYear,
    int PeriodMonth,
    DateTime CreatedAt
);

public record PendingCommissionsSummary(
    IReadOnlyList<PendingCommissionResponse> Items,
    decimal TotalNet,
    int Count
);

public record GetPendingCommissionsQuery(
    long? DoctorId = null,
    long? BranchId = null,
    int? Year = null,
    int? Month = null
) : IRequest<PendingCommissionsSummary>;

public class GetPendingCommissionsQueryHandler
    : IRequestHandler<GetPendingCommissionsQuery, PendingCommissionsSummary>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetPendingCommissionsQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PendingCommissionsSummary> Handle(
        GetPendingCommissionsQuery r, CancellationToken ct)
    {
        var q = _db.DoctorCommissions.AsNoTracking()
            .Where(c => c.Status == CommissionStatus.Pending);

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            q = q.Where(c => c.BranchId == _tenant.BranchId.Value);
        else if (r.BranchId.HasValue)
            q = q.Where(c => c.BranchId == r.BranchId.Value);

        if (r.DoctorId.HasValue) q = q.Where(c => c.DoctorId == r.DoctorId.Value);
        if (r.Year.HasValue)     q = q.Where(c => c.PeriodYear == r.Year.Value);
        if (r.Month.HasValue)    q = q.Where(c => c.PeriodMonth == r.Month.Value);

        var rows = await (from c in q
                          join u in _db.Users.AsNoTracking() on c.DoctorId equals u.Id
                          join pi in _db.TreatmentPlanItems.AsNoTracking() on c.TreatmentPlanItemId equals pi.Id into itemsJ
                          from pi in itemsJ.DefaultIfEmpty()
                          join t in _db.Treatments.AsNoTracking() on (pi != null ? pi.TreatmentId : 0) equals t.Id into treatments
                          from t in treatments.DefaultIfEmpty()
                          select new
                          {
                              c.Id, c.DoctorId, DoctorName = u.FullName,
                              c.TreatmentPlanItemId,
                              TreatmentId = (long?)(pi != null ? pi.TreatmentId : (long?)null),
                              TreatmentName = t != null ? t.Name : null,
                              c.BranchId,
                              c.GrossAmount, c.NetBaseAmount, c.CommissionRate,
                              c.CommissionAmount, c.NetCommissionAmount, c.BonusApplied,
                              c.PeriodYear, c.PeriodMonth, c.CreatedAt
                          })
                          .OrderByDescending(x => x.CreatedAt)
                          .Take(1000)
                          .ToListAsync(ct);

        var items = rows.Select(x => new PendingCommissionResponse(
            x.Id, x.DoctorId, x.DoctorName, x.TreatmentPlanItemId,
            x.TreatmentId, x.TreatmentName,
            x.BranchId, x.GrossAmount, x.NetBaseAmount,
            x.CommissionRate, x.CommissionAmount, x.NetCommissionAmount,
            x.BonusApplied, x.PeriodYear, x.PeriodMonth, x.CreatedAt)).ToList();

        return new PendingCommissionsSummary(items, items.Sum(x => x.NetCommissionAmount), items.Count);
    }
}
