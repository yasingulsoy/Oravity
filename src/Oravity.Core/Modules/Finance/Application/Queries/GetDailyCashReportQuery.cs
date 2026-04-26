using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Finance.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Queries;

// ─── Query ────────────────────────────────────────────────────────────────

public record GetDailyCashReportQuery(DateOnly Date, long? BranchId = null)
    : IRequest<DailyCashReportDetailResponse>;

public class GetDailyCashReportQueryHandler
    : IRequestHandler<GetDailyCashReportQuery, DailyCashReportDetailResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public GetDailyCashReportQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<DailyCashReportDetailResponse> Handle(
        GetDailyCashReportQuery request, CancellationToken ct)
    {
        long branchId = request.BranchId
            ?? _tenant.BranchId
            ?? throw new InvalidOperationException("Şube bağlamı bulunamadı.");

        // ── Kasa raporu durumu ────────────────────────────────────────────
        var reportRecord = await _db.DailyCashReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BranchId == branchId && r.ReportDate == request.Date, ct);

        // Tenant filtresi
        var paymentsQ = _db.Payments
            .AsNoTracking()
            .Where(p => p.BranchId == branchId && p.PaymentDate == request.Date && !p.IsRefunded);

        if (_tenant.IsCompanyAdmin && _tenant.CompanyId.HasValue)
            paymentsQ = paymentsQ.Where(p => p.Branch.CompanyId == _tenant.CompanyId.Value);

        // ── Ödemeler + hasta bilgisi ──────────────────────────────────────
        var payments = await (
            from p in paymentsQ
            join pat in _db.Patients.AsNoTracking() on p.PatientId equals pat.Id into patJ
            from pat in patJ.DefaultIfEmpty()
            join u in _db.Users.AsNoTracking() on p.CreatedByUserId equals (long?)u.Id into uJ
            from u in uJ.DefaultIfEmpty()
            orderby p.CreatedAt
            select new CashPaymentLine(
                p.PublicId,
                p.Id,
                p.CreatedAt,
                pat != null ? $"{pat.FirstName} {pat.LastName}".Trim() : "—",
                p.Amount,
                p.Currency,
                p.ExchangeRate,
                p.BaseAmount,
                p.Method,
                FinanceMappings.MethodLabel(p.Method),
                p.Notes,
                u != null ? u.FullName : "—"
            )
        ).ToListAsync(ct);

        // ── Para birimi × yöntem matrisi ─────────────────────────────────
        var byMethod = payments
            .GroupBy(p => p.Method)
            .Select(g => new CashMethodTotal(
                g.Key,
                FinanceMappings.MethodLabel(g.Key),
                g.Sum(p => p.BaseAmount),   // TRY toplamı
                g.Count(),
                g.GroupBy(p => p.Currency)
                 .Select(cg => new CashCurrencyTotal(
                     cg.Key,
                     cg.Sum(p => p.Amount),
                     cg.Sum(p => p.BaseAmount),
                     cg.Count()))
                 .OrderBy(x => x.Currency)
                 .ToList()))
            .OrderBy(m => (int)m.Method)
            .ToList();

        var byCurrency = payments
            .GroupBy(p => p.Currency)
            .Select(g => new CashCurrencyTotal(
                g.Key,
                g.Sum(p => p.Amount),
                g.Sum(p => p.BaseAmount),
                g.Count()))
            .OrderBy(x => x.Currency)
            .ToList();

        var totalTry   = payments.Sum(p => p.BaseAmount);
        var totalCount = payments.Count;

        DailyCashReportResponse? reportStatus = reportRecord is not null
            ? CashReportMappings.ToResponse(reportRecord)
            : null;

        return new DailyCashReportDetailResponse(
            request.Date,
            branchId,
            reportStatus,
            payments,
            byMethod,
            byCurrency,
            totalTry,
            totalCount);
    }
}
