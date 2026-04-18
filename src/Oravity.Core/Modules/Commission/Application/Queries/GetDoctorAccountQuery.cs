using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Commission.Application.Queries;

public record DoctorMonthlyPeriod(
    int Year,
    int Month,
    decimal TotalGross,
    decimal TotalDeductions,
    decimal TotalCommission,
    decimal TotalNet,
    int CompletedCount,
    bool BonusApplied,
    decimal? TargetAmount,
    bool TargetReached
);

public record DoctorAccountResponse(
    long DoctorId,
    string DoctorName,
    long? BranchId,
    decimal TotalPending,
    decimal TotalDistributed,
    IReadOnlyList<DoctorMonthlyPeriod> Monthly
);

public record GetDoctorAccountQuery(
    long DoctorId,
    long? BranchId = null,
    int? Year = null
) : IRequest<DoctorAccountResponse>;

public class GetDoctorAccountQueryHandler
    : IRequestHandler<GetDoctorAccountQuery, DoctorAccountResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetDoctorAccountQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<DoctorAccountResponse> Handle(
        GetDoctorAccountQuery r, CancellationToken ct)
    {
        var doctor = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == r.DoctorId, ct)
            ?? throw new NotFoundException($"Hekim bulunamadı: {r.DoctorId}");

        var q = _db.DoctorCommissions.AsNoTracking()
            .Where(c => c.DoctorId == r.DoctorId);

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            q = q.Where(c => c.BranchId == _tenant.BranchId.Value);
        else if (r.BranchId.HasValue)
            q = q.Where(c => c.BranchId == r.BranchId.Value);

        if (r.Year.HasValue) q = q.Where(c => c.PeriodYear == r.Year.Value);

        var commissions = await q.ToListAsync(ct);

        var pending     = commissions.Where(c => c.Status == CommissionStatus.Pending).Sum(c => c.NetCommissionAmount);
        var distributed = commissions.Where(c => c.Status == CommissionStatus.Distributed).Sum(c => c.NetCommissionAmount);

        var grouped = commissions
            .Where(c => c.Status != CommissionStatus.Cancelled)
            .GroupBy(c => new { c.PeriodYear, c.PeriodMonth })
            .Select(g => new
            {
                g.Key.PeriodYear, g.Key.PeriodMonth,
                Gross       = g.Sum(x => x.GrossAmount),
                Deductions  = g.Sum(x => x.PosCommissionAmount + x.LabCostDeducted
                                        + x.TreatmentCostDeducted + x.TreatmentPlanCommissionDeducted
                                        + x.ExtraExpenseAmount),
                Commission  = g.Sum(x => x.CommissionAmount),
                Net         = g.Sum(x => x.NetCommissionAmount),
                Count       = g.Count(),
                Bonus       = g.Any(x => x.BonusApplied)
            })
            .OrderByDescending(x => x.PeriodYear).ThenByDescending(x => x.PeriodMonth)
            .ToList();

        // Hedef karşılaştırma
        var branchForTarget = _tenant.IsBranchLevel && _tenant.BranchId.HasValue
            ? _tenant.BranchId.Value : (r.BranchId ?? 0);

        var targetYears = grouped.Select(x => x.PeriodYear).Distinct().ToList();
        var targets = branchForTarget == 0
            ? new List<DoctorTarget>()
            : await _db.DoctorTargets.AsNoTracking()
                .Where(t => t.DoctorId == r.DoctorId && t.BranchId == branchForTarget && targetYears.Contains(t.Year))
                .ToListAsync(ct);

        var monthly = grouped.Select(x =>
        {
            var tgt = targets.FirstOrDefault(t => t.Year == x.PeriodYear && t.Month == x.PeriodMonth);
            return new DoctorMonthlyPeriod(
                x.PeriodYear, x.PeriodMonth,
                x.Gross, x.Deductions, x.Commission, x.Net,
                x.Count, x.Bonus, tgt?.TargetAmount,
                tgt != null && x.Gross >= tgt.TargetAmount);
        }).ToList();

        return new DoctorAccountResponse(
            r.DoctorId, doctor.FullName, r.BranchId ?? _tenant.BranchId,
            pending, distributed, monthly);
    }
}
