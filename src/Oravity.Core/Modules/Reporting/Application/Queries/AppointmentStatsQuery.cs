using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Reporting.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Reporting.Application.Queries;

/// <summary>
/// Randevu istatistikleri — status dağılımı, gelmedi oranı, ortalama süre.
/// v_appointment_stats view verisine karşılık gelir.
/// </summary>
public record AppointmentStatsQuery(
    DateTime StartDate,
    DateTime EndDate,
    long?    BranchId = null) : IRequest<AppointmentStatsReport>;

public class AppointmentStatsQueryHandler : IRequestHandler<AppointmentStatsQuery, AppointmentStatsReport>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public AppointmentStatsQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<AppointmentStatsReport> Handle(AppointmentStatsQuery request, CancellationToken ct)
    {
        var startUtc = request.StartDate.Date.ToUniversalTime();
        var endUtc   = request.EndDate.Date.AddDays(1).ToUniversalTime();

        var query = _db.Appointments
            .Where(a => a.Branch.CompanyId == _tenant.CompanyId
                     && a.StartTime >= startUtc
                     && a.StartTime < endUtc);

        if (request.BranchId.HasValue)
            query = query.Where(a => a.BranchId == request.BranchId.Value);
        else if (_tenant.BranchId > 0)
            query = query.Where(a => a.BranchId == _tenant.BranchId);

        // StatusId bazında gruplama
        var groups = await query
            .GroupBy(a => a.StatusId)
            .Select(g => new { StatusId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var total = groups.Sum(g => g.Count);

        // Ortalama randevu süresi (dakika) — sadece tamamlananlar
        var completedTimes = await query
            .Where(a => a.StatusId == AppointmentStatus.WellKnownIds.Completed)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync(ct);
        var avgMinutes = completedTimes.Count > 0
            ? completedTimes.Average(a => (a.EndTime - a.StartTime).TotalSeconds)
            : 0.0;

        // Gün bazında istatistik
        var byDayRaw = await query
            .Select(a => new { Date = a.StartTime.Date, a.StatusId })
            .ToListAsync(ct);

        var byDay = byDayRaw
            .GroupBy(a => a.Date)
            .Select(g => new AppointmentByDayLine(
                DateOnly.FromDateTime(g.Key),
                g.Count(),
                g.Count(a => a.StatusId == AppointmentStatus.WellKnownIds.Completed),
                g.Count(a => a.StatusId == AppointmentStatus.WellKnownIds.NoShow)))
            .OrderBy(d => d.Date)
            .ToList();

        // Status label haritası
        var statusLabels = new Dictionary<int, string>
        {
            [AppointmentStatus.WellKnownIds.Planned]   = "Planlandı",
            [AppointmentStatus.WellKnownIds.Confirmed] = "Onaylandı",
            [AppointmentStatus.WellKnownIds.Arrived]   = "Geldi",
            [AppointmentStatus.WellKnownIds.InRoom]    = "Odaya Alındı",
            [AppointmentStatus.WellKnownIds.Left]      = "Ayrıldı",
            [AppointmentStatus.WellKnownIds.Cancelled] = "İptal",
            [AppointmentStatus.WellKnownIds.Completed] = "Tamamlandı",
            [AppointmentStatus.WellKnownIds.NoShow]    = "Gelmedi"
        };

        var byStatus = groups.Select(g => new AppointmentStatusSummary(
            g.StatusId,
            statusLabels.GetValueOrDefault(g.StatusId, g.StatusId.ToString()),
            g.Count,
            total > 0 ? Math.Round((decimal)g.Count / total * 100, 1) : 0m))
            .OrderBy(s => s.Status)
            .ToList();

        var noShowCount = groups.FirstOrDefault(g => g.StatusId == AppointmentStatus.WellKnownIds.NoShow)?.Count ?? 0;
        var noShowRate  = total > 0 ? Math.Round((decimal)noShowCount / total * 100, 1) : 0m;

        return new AppointmentStatsReport(
            request.StartDate, request.EndDate,
            total, noShowRate, (int)(avgMinutes / 60),
            byStatus, byDay);
    }
}
