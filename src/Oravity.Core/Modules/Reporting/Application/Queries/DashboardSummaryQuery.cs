using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;
using Oravity.Core.Modules.Reporting.Application;

namespace Oravity.Core.Modules.Reporting.Application.Queries;

/// <summary>
/// Dashboard özet verisi — bugünkü KPI'lar.
/// ICacheService üzerinden 5 dk Redis TTL ile cache'lenir.
/// </summary>
public record DashboardSummaryQuery : IRequest<DashboardSummary>;

public class DashboardSummaryQueryHandler : IRequestHandler<DashboardSummaryQuery, DashboardSummary>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser   _user;
    private readonly ICacheService  _cache;
    private readonly ILogger<DashboardSummaryQueryHandler> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public DashboardSummaryQueryHandler(
        AppDbContext    db,
        ITenantContext  tenant,
        ICurrentUser    user,
        ICacheService   cache,
        ILogger<DashboardSummaryQueryHandler> logger)
    {
        _db     = db;
        _tenant = tenant;
        _user   = user;
        _cache  = cache;
        _logger = logger;
    }

    public async Task<DashboardSummary> Handle(DashboardSummaryQuery request, CancellationToken ct)
    {
        var cacheKey = $"dashboard:{_tenant.CompanyId}:{_tenant.BranchId}:{DateTime.UtcNow:yyyyMMdd}";

        var cached = await _cache.GetAsync<DashboardSummary>(cacheKey, ct);
        if (cached is not null)
        {
            _logger.LogDebug("DashboardSummary cache'den döndü: {Key}", cacheKey);
            return cached;
        }

        var result = await BuildSummaryAsync(ct);
        await _cache.SetAsync(cacheKey, result, CacheTtl, ct);
        return result;
    }

    private async Task<DashboardSummary> BuildSummaryAsync(CancellationToken ct)
    {
        var todayUtcStart = DateTime.UtcNow.Date;
        var todayUtcEnd   = todayUtcStart.AddDays(1);

        // ── Randevu istatistikleri ────────────────────────────────────────
        var apptGroups = await _db.Appointments
            .Where(a => a.BranchId == _tenant.BranchId
                     && a.StartTime >= todayUtcStart
                     && a.StartTime < todayUtcEnd)
            .GroupBy(a => a.StatusId)
            .Select(g => new { StatusId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var apptTotal     = apptGroups.Sum(g => g.Count);
        var apptCompleted = apptGroups.Where(g => g.StatusId == AppointmentStatus.WellKnownIds.Completed).Sum(g => g.Count);
        var apptNoShow    = apptGroups.Where(g => g.StatusId == AppointmentStatus.WellKnownIds.NoShow).Sum(g => g.Count);
        var apptCancelled = apptGroups.Where(g => g.StatusId == AppointmentStatus.WellKnownIds.Cancelled).Sum(g => g.Count);
        var apptPending   = apptTotal - apptCompleted - apptNoShow - apptCancelled;

        var appointments = new AppointmentTodaySummary(
            apptTotal, apptCompleted, apptPending, apptNoShow, apptCancelled);

        // ── Bugünkü gelir ─────────────────────────────────────────────────
        var todayDate = DateOnly.FromDateTime(todayUtcStart);
        var payments = await _db.Payments
            .Where(p => p.BranchId == _tenant.BranchId
                     && !p.IsRefunded
                     && p.PaymentDate == todayDate)
            .GroupBy(p => p.Method)
            .Select(g => new { Method = g.Key, Amount = g.Sum(p => p.Amount), Count = g.Count() })
            .ToListAsync(ct);

        var paymentMethodLabels = new Dictionary<PaymentMethod, string>
        {
            [PaymentMethod.Cash]         = "Nakit",
            [PaymentMethod.CreditCard]   = "Kredi Kartı",
            [PaymentMethod.BankTransfer] = "Havale/EFT",
            [PaymentMethod.Installment]  = "Taksit",
            [PaymentMethod.Check]        = "Çek"
        };

        var byMethod = payments.Select(p => new RevenueByMethod(
            paymentMethodLabels.GetValueOrDefault(p.Method, p.Method.ToString()),
            p.Amount, p.Count)).ToList();

        var revenue = new RevenueTodaySummary(byMethod.Sum(m => m.Amount), byMethod);

        // ── Bekleyen online randevu talepleri ─────────────────────────────
        var pendingBookings = await _db.OnlineBookingRequests
            .CountAsync(r => r.BranchId == _tenant.BranchId
                          && r.Status == BookingRequestStatus.Pending, ct);

        // ── Okunmamış bildirimler ─────────────────────────────────────────
        var unreadNotifications = await _db.Notifications
            .CountAsync(n => n.BranchId == _tenant.BranchId
                          && n.ToUserId == _user.UserId
                          && !n.IsRead, ct);

        return new DashboardSummary(
            appointments,
            revenue,
            pendingBookings,
            unreadNotifications,
            DateTime.UtcNow);
    }
}
