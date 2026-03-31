using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Notification.Infrastructure.Services;

// ─── SMS Adapter (SPEC §İLETİŞİM ALTYAPISI — SMS Adapter Pattern) ────────

public record SmsMessage(string Phone, string Text, string? SenderId = null);

public record SmsResult(bool Success, string? ProviderMessageId, string? Error);

public interface ISmsAdapter
{
    Task<SmsResult> Send(SmsMessage message);
}

/// <summary>
/// Gerçek SMS sağlayıcısı olmadığında kullanılan stub — log'a yazar.
/// Prodüksiyon'da AsistSmsAdapter / NetgsmSmsAdapter ile replace edilir.
/// </summary>
public class StubSmsAdapter : ISmsAdapter
{
    private readonly ILogger<StubSmsAdapter> _logger;

    public StubSmsAdapter(ILogger<StubSmsAdapter> logger)
    {
        _logger = logger;
    }

    public Task<SmsResult> Send(SmsMessage message)
    {
        _logger.LogInformation("[SMS STUB] → {Phone}: {Text}", message.Phone, message.Text);
        return Task.FromResult(new SmsResult(true, $"STUB-{Guid.NewGuid():N}", null));
    }
}

// ─── Hangfire Job ─────────────────────────────────────────────────────────

/// <summary>
/// Hangfire recurring job — her dakika çalışır.
/// SmsQueue'da status=1 olan kayıtları işler.
/// Exponential backoff uygulanır (entity içinde tanımlı).
/// Kayıt: Program.cs → RecurringJob.AddOrUpdate
/// </summary>
public class SmsDispatchService
{
    private readonly AppDbContext _db;
    private readonly ISmsAdapter _smsAdapter;
    private readonly ILogger<SmsDispatchService> _logger;
    private const int BatchSize = 50;

    public SmsDispatchService(
        AppDbContext db,
        ISmsAdapter smsAdapter,
        ILogger<SmsDispatchService> logger)
    {
        _db = db;
        _smsAdapter = smsAdapter;
        _logger = logger;
    }

    public async Task Execute()
    {
        var now = DateTime.UtcNow;

        var pending = await _db.SmsQueues
            .Where(s =>
                s.Status == SmsStatus.Queued &&
                (s.NextRetryAt == null || s.NextRetryAt <= now))
            .OrderBy(s => s.CreatedAt)
            .Take(BatchSize)
            .ToListAsync();

        if (pending.Count == 0) return;

        _logger.LogInformation("SmsDispatch: {Count} kayıt işlenecek", pending.Count);

        foreach (var sms in pending)
        {
            try
            {
                var result = await _smsAdapter.Send(new SmsMessage(sms.ToPhone, sms.Message));

                if (result.Success)
                {
                    sms.MarkSent(result.ProviderMessageId);
                    _logger.LogInformation(
                        "SMS gönderildi: Id={Id}, Phone={Phone}, Provider={ProviderId}",
                        sms.Id, sms.ToPhone, result.ProviderMessageId);
                }
                else
                {
                    sms.MarkFailed(result.Error ?? "Sağlayıcı hatası");
                    _logger.LogWarning(
                        "SMS gönderilemedi: Id={Id}, Error={Error}, Attempt={Attempt}",
                        sms.Id, result.Error, sms.AttemptCount);
                }
            }
            catch (Exception ex)
            {
                sms.MarkFailed(ex.Message);
                _logger.LogError(ex, "SMS işleme hatası: Id={Id}", sms.Id);
            }
        }

        await _db.SaveChangesAsync();
    }
}
