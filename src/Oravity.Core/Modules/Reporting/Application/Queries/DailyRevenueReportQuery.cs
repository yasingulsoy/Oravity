using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Reporting.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Reporting.Application.Queries;

/// <summary>
/// Tarih aralığı ve şube bazında günlük gelir raporu.
/// v_daily_revenue view'ının EF karşılığı olarak direkt sorgu kullanılır.
/// </summary>
public record DailyRevenueReportQuery(
    DateTime  StartDate,
    DateTime  EndDate,
    long?     BranchId = null) : IRequest<DailyRevenueReport>;

public class DailyRevenueReportQueryHandler : IRequestHandler<DailyRevenueReportQuery, DailyRevenueReport>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public DailyRevenueReportQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<DailyRevenueReport> Handle(DailyRevenueReportQuery request, CancellationToken ct)
    {
        var startDate = DateOnly.FromDateTime(request.StartDate.Date);
        var endDate   = DateOnly.FromDateTime(request.EndDate.Date);
        // Komisyon sorguları için UTC aralık
        var startUtc = request.StartDate.Date.ToUniversalTime();
        var endUtc   = request.EndDate.Date.AddDays(1).ToUniversalTime();

        var query = _db.Payments
            .Where(p => p.Branch.CompanyId == _tenant.CompanyId
                     && !p.IsRefunded
                     && p.PaymentDate >= startDate
                     && p.PaymentDate <= endDate);

        if (request.BranchId.HasValue)
            query = query.Where(p => p.BranchId == request.BranchId.Value);
        else if (_tenant.BranchId > 0)
            query = query.Where(p => p.BranchId == _tenant.BranchId);

        var rawPayments = await query
            .Select(p => new
            {
                Date    = p.PaymentDate,
                p.Method,
                p.Amount,
                p.BranchId,
                DoctorId   = (long?)null, // Payment direkt doktora bağlı değil — komisyon tablosundan
                DoctorName = (string?)null
            })
            .ToListAsync(ct);

        // Gün bazında gruplama
        var byDay = rawPayments
            .GroupBy(p => p.Date)
            .Select(g => new DailyRevenueLine(g.Key, g.Sum(p => p.Amount), g.Count()))
            .OrderBy(d => d.Date)
            .ToList();

        // Ödeme yöntemi bazında
        var methodLabels = new Dictionary<PaymentMethod, string>
        {
            [PaymentMethod.Cash]         = "Nakit",
            [PaymentMethod.CreditCard]   = "Kredi Kartı",
            [PaymentMethod.BankTransfer] = "Havale/EFT",
            [PaymentMethod.Installment]  = "Taksit",
            [PaymentMethod.Check]        = "Çek"
        };

        var byMethod = rawPayments
            .GroupBy(p => p.Method)
            .Select(g => new RevenueByMethod(
                methodLabels.GetValueOrDefault(g.Key, g.Key.ToString()),
                g.Sum(p => p.Amount), g.Count()))
            .OrderByDescending(m => m.Amount)
            .ToList();

        // Hekim bazında gelir — komisyon tablosu üzerinden
        var commissionData = await _db.DoctorCommissions
            .Include(c => c.Doctor)
            .Where(c => c.Branch.CompanyId == _tenant.CompanyId
                     && c.CreatedAt >= startUtc
                     && c.CreatedAt < endUtc)
            .GroupBy(c => new { c.DoctorId, c.Doctor.FullName })
            .Select(g => new RevenueByDoctor(
                g.Key.DoctorId,
                g.Key.FullName,
                g.Sum(c => c.GrossAmount),
                g.Count()))
            .OrderByDescending(d => d.Total)
            .ToListAsync(ct);

        var grandTotal = rawPayments.Sum(p => p.Amount);

        return new DailyRevenueReport(
            request.StartDate, request.EndDate, grandTotal, byDay, byMethod, commissionData);
    }
}
