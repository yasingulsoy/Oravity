using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Oravity.Infrastructure.Database;

namespace Oravity.Infrastructure.Jobs;

/// <summary>
/// Outbox sağlık kontrolü.
/// Son 10 dakikada status=4 (Dead Letter / Başarısız) mesaj varsa → Unhealthy.
/// Varsa ama 10 dakikadan eskiyse → Degraded.
/// Yoksa → Healthy.
/// </summary>
public class OutboxHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;

    public OutboxHealthCheck(AppDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var tenMinutesAgo = DateTime.UtcNow.AddMinutes(-10);

        // Son 10 dakikada ölü mesaj var mı?
        var recentDeadCount = await _db.OutboxMessages
            .CountAsync(m => m.Status == 4 && m.CreatedAt >= tenMinutesAgo, cancellationToken);

        if (recentDeadCount > 0)
        {
            return HealthCheckResult.Unhealthy(
                $"Son 10 dakikada {recentDeadCount} outbox mesajı başarısız oldu (status=4). " +
                "Kritik event işleme hatası — lütfen outbox_messages tablosunu inceleyin.");
        }

        // Uzun süredir bekleyen mesaj var mı? (1 saatten fazla status=3)
        var stuckCount = await _db.OutboxMessages
            .CountAsync(m => (m.Status == 1 || m.Status == 3) &&
                             m.CreatedAt < DateTime.UtcNow.AddHours(-1), cancellationToken);

        if (stuckCount > 0)
        {
            return HealthCheckResult.Degraded(
                $"{stuckCount} outbox mesajı 1 saatten fazladır işlenemiyor. " +
                "OutboxProcessorJob'un çalıştığını kontrol edin.",
                data: new Dictionary<string, object> { ["stuck_count"] = stuckCount });
        }

        // Toplam bekleyen sayısını meta veri olarak ekle
        var pendingCount = await _db.OutboxMessages
            .CountAsync(m => m.Status == 1 || m.Status == 3, cancellationToken);

        return HealthCheckResult.Healthy(
            "Outbox sağlıklı.",
            data: new Dictionary<string, object> { ["pending"] = pendingCount });
    }
}
