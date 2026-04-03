using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oravity.Infrastructure.Database;

namespace Oravity.Infrastructure.Jobs;

/// <summary>
/// Transactional Outbox Processor — Her 5 saniyede çalışır (Hangfire).
///
/// İş akışı:
///   1. status=1 (Bekliyor) veya status=3 (Retry) olan, next_retry_at süresi
///      dolmuş mesajları al (max 50).
///   2. Her mesaj için OutboxEventDispatcher'a yönlendir.
///   3. Başarı → status=2 (İşlendi), processed_at=now
///   4. Hata →
///        attempt_count++
///        attempt_count >= max_attempts → status=4 (Başarısız)
///        değilse → status=3, exponential backoff: 5^n saniye
///   5. Tek SaveChangesAsync — toplu commit.
///
/// SPEC §MİMARİ REVİZYON v2 §2
/// </summary>
public class OutboxProcessorJob
{
    private readonly AppDbContext _db;
    private readonly OutboxEventDispatcher _dispatcher;
    private readonly ILogger<OutboxProcessorJob> _logger;

    private const int BatchSize = 50;

    public OutboxProcessorJob(
        AppDbContext db,
        OutboxEventDispatcher dispatcher,
        ILogger<OutboxProcessorJob> logger)
    {
        _db         = db;
        _dispatcher = dispatcher;
        _logger     = logger;
    }

    /// <summary>
    /// Hangfire bu metodu her 5 saniyede çağırır.
    /// [DisableConcurrentExecution] — çakışan iki job çalışmasın.
    /// </summary>
    [DisableConcurrentExecution(30)]
    [AutomaticRetry(Attempts = 0)]  // Hangfire seviyesinde retry devre dışı — kendi retry mekanizmamız var
    public async Task Execute()
    {
        var now = DateTime.UtcNow;

        var messages = await _db.OutboxMessages
            .Where(m => (m.Status == 1 || m.Status == 3) && m.NextRetryAt <= now)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync();

        if (messages.Count == 0) return;

        _logger.LogInformation(
            "OutboxProcessorJob: {Count} mesaj işlenecek", messages.Count);

        int succeeded = 0;
        int failed    = 0;
        int retrying  = 0;

        foreach (var msg in messages)
        {
            try
            {
                await _dispatcher.Dispatch(msg.EventType, msg.Payload);
                msg.MarkProcessed();
                succeeded++;

                _logger.LogDebug(
                    "Outbox OK — {Id} [{EventType}]", msg.Id, msg.EventType);
            }
            catch (Exception ex)
            {
                msg.MarkFailed(ex.Message);

                if (msg.Status == 4)
                {
                    failed++;
                    _logger.LogError(ex,
                        "Outbox DEAD — {Id} [{EventType}] {AttemptCount}/{MaxAttempts}",
                        msg.Id, msg.EventType, msg.AttemptCount, msg.MaxAttempts);
                }
                else
                {
                    retrying++;
                    _logger.LogWarning(
                        "Outbox RETRY — {Id} [{EventType}] attempt={AttemptCount}, next={NextRetry}",
                        msg.Id, msg.EventType, msg.AttemptCount, msg.NextRetryAt);
                }
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "OutboxProcessorJob tamamlandı: OK={Succeeded} RETRY={Retrying} DEAD={Failed}",
            succeeded, retrying, failed);
    }
}
