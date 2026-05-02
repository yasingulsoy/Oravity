using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Finance.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
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
        // ── Şube bağlamı çözümle ────────────────────────────────────────────
        long? resolvedBranchId = request.BranchId ?? _tenant.BranchId;

        if (resolvedBranchId is null)
            throw new InvalidOperationException(
                "Günlük kasa raporu için şube seçimi zorunludur. branchId parametresi gönderiniz.");

        var branchId = resolvedBranchId.Value;

        // Tenant güvenlik filtresi
        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue && branchId != _tenant.BranchId.Value)
            throw new ForbiddenException("Bu şubenin raporuna erişim yetkiniz yok.");

        if (_tenant.IsCompanyAdmin && _tenant.CompanyId.HasValue)
        {
            var belongsToCompany = await _db.Branches.AsNoTracking()
                .AnyAsync(b => b.Id == branchId && b.CompanyId == _tenant.CompanyId.Value, ct);
            if (!belongsToCompany)
                throw new ForbiddenException("Bu şubenin raporuna erişim yetkiniz yok.");
        }

        // ── Kasa raporu durumu ────────────────────────────────────────────
        var reportRecord = await _db.DailyCashReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BranchId == branchId && r.ReportDate == request.Date, ct);

        // ── Ödemeler ─────────────────────────────────────────────────────
        var rawPayments = await (
            from p in _db.Payments.AsNoTracking()
            join pat in _db.Patients.AsNoTracking() on p.PatientId equals pat.Id into patJ
            from pat in patJ.DefaultIfEmpty()
            join u in _db.Users.AsNoTracking() on p.CreatedByUserId equals (long?)u.Id into uJ
            from u in uJ.DefaultIfEmpty()
            join pos in _db.PosTerminals.AsNoTracking() on p.PosTerminalId equals (long?)pos.Id into posJ
            from pos in posJ.DefaultIfEmpty()
            join posBank in _db.Banks.AsNoTracking() on pos.BankId equals (long?)posBank.Id into posBankJ
            from posBank in posBankJ.DefaultIfEmpty()
            join bank in _db.BankAccounts.AsNoTracking() on p.BankAccountId equals (long?)bank.Id into bankJ
            from bank in bankJ.DefaultIfEmpty()
            join bankRef in _db.Banks.AsNoTracking() on bank.BankId equals (long?)bankRef.Id into bankRefJ
            from bankRef in bankRefJ.DefaultIfEmpty()
            where p.BranchId == branchId && p.PaymentDate == request.Date && !p.IsRefunded && !p.IsDeleted
            orderby p.CreatedAt
            select new
            {
                p.PublicId,
                p.Id,
                p.CreatedAt,
                PatientName     = pat != null ? $"{pat.FirstName} {pat.LastName}".Trim() : "—",
                p.Amount,
                p.Currency,
                p.ExchangeRate,
                p.BaseAmount,
                p.Method,
                p.Notes,
                RecordedBy      = u != null ? u.FullName : "—",
                PosPublicId     = pos != null ? (Guid?)pos.PublicId : null,
                PosName         = pos != null ? pos.Name : null,
                PosBankName     = posBank != null ? posBank.ShortName : null,
                BankPublicId    = bank != null ? (Guid?)bank.PublicId : null,
                BankAcctName    = bank != null ? bank.AccountName : null,
                BankName        = bankRef != null ? bankRef.ShortName : null,
                BankCurrency    = bank != null ? bank.Currency : null,
            }
        ).ToListAsync(ct);

        var payments = rawPayments.Select(p => new CashPaymentLine(
            p.PublicId, p.Id, p.CreatedAt, p.PatientName,
            p.Amount, p.Currency, p.ExchangeRate, p.BaseAmount,
            p.Method, FinanceMappings.MethodLabel(p.Method),
            p.Notes, p.RecordedBy
        )).ToList();

        // ── Para birimi × yöntem matrisi ─────────────────────────────────
        var byMethod = payments
            .GroupBy(p => p.Method)
            .Select(g => new CashMethodTotal(
                g.Key,
                FinanceMappings.MethodLabel(g.Key),
                g.Sum(p => p.BaseAmount),
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

        // ── POS toplamı (Kredi Kartı + Taksit) ───────────────────────────
        var posPayments = rawPayments
            .Where(p => p.Method is PaymentMethod.CreditCard or PaymentMethod.Installment)
            .ToList();

        var posTotals = posPayments
            .GroupBy(p => new { p.PosPublicId, p.PosName, p.PosBankName })
            .Select(g => new PosTotalLine(
                g.Key.PosPublicId,
                g.Key.PosName ?? "Bilinmeyen POS",
                g.Key.PosBankName ?? "—",
                g.Sum(p => p.BaseAmount),
                g.Count(),
                g.GroupBy(p => p.Currency)
                 .Select(cg => new CashCurrencyTotal(
                     cg.Key,
                     cg.Sum(p => p.Amount),
                     cg.Sum(p => p.BaseAmount),
                     cg.Count()))
                 .OrderBy(x => x.Currency)
                 .ToList()))
            .OrderBy(x => x.TerminalName)
            .ToList();

        // ── Banka toplamı (Havale/EFT) ────────────────────────────────────
        var bankPayments = rawPayments
            .Where(p => p.Method is PaymentMethod.BankTransfer)
            .ToList();

        var bankTotals = bankPayments
            .GroupBy(p => new { p.BankPublicId, p.BankAcctName, p.BankName, p.BankCurrency })
            .Select(g => new BankTotalLine(
                g.Key.BankPublicId,
                g.Key.BankAcctName ?? "Bilinmeyen Hesap",
                g.Key.BankName ?? "—",
                g.Key.BankCurrency ?? "TRY",
                g.Sum(p => p.BaseAmount),
                g.Count(),
                g.GroupBy(p => p.Currency)
                 .Select(cg => new CashCurrencyTotal(
                     cg.Key,
                     cg.Sum(p => p.Amount),
                     cg.Sum(p => p.BaseAmount),
                     cg.Count()))
                 .OrderBy(x => x.Currency)
                 .ToList()))
            .OrderBy(x => x.BankName).ThenBy(x => x.AccountName)
            .ToList();

        // ── KASA bölümü ──────────────────────────────────────────────────
        // Önceki günden devir: bir önceki günün onaylı nakit ödemeleri
        var prevDate = request.Date.AddDays(-1);
        var prevApproved = await _db.DailyCashReports.AsNoTracking()
            .AnyAsync(r => r.BranchId == branchId
                        && r.ReportDate == prevDate
                        && r.Status == CashReportStatus.Approved, ct);

        List<CashCurrencyTotal> oncekiGunDevir;
        if (prevApproved)
        {
            oncekiGunDevir = await _db.Payments.AsNoTracking()
                .Where(p => p.BranchId == branchId
                         && p.PaymentDate == prevDate
                         && p.Method == PaymentMethod.Cash
                         && !p.IsRefunded && !p.IsDeleted)
                .GroupBy(p => p.Currency)
                .Select(g => new CashCurrencyTotal(
                    g.Key,
                    g.Sum(p => p.Amount),
                    g.Sum(p => p.BaseAmount),
                    g.Count()))
                .OrderBy(x => x.Currency)
                .ToListAsync(ct);
        }
        else
        {
            oncekiGunDevir = [];
        }

        var bugunNakit = rawPayments
            .Where(p => p.Method == PaymentMethod.Cash)
            .GroupBy(p => p.Currency)
            .Select(g => new CashCurrencyTotal(
                g.Key,
                g.Sum(p => p.Amount),
                g.Sum(p => p.BaseAmount),
                g.Count()))
            .OrderBy(x => x.Currency)
            .ToList();

        // Toplam kasa = devir + bugün (her para birimi için birleştir)
        var toplamKasa = oncekiGunDevir
            .Concat(bugunNakit)
            .GroupBy(x => x.Currency)
            .Select(g => new CashCurrencyTotal(
                g.Key,
                g.Sum(x => x.Amount),
                g.Sum(x => x.BaseTry),
                g.Sum(x => x.Count)))
            .OrderBy(x => x.Currency)
            .ToList();

        var kasa = new KasaSection(oncekiGunDevir, bugunNakit, toplamKasa);

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
            payments.Sum(p => p.BaseAmount),
            payments.Count,
            posTotals,
            bankTotals,
            kasa);
    }
}
