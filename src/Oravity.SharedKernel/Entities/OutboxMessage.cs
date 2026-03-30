using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Transactional Outbox Pattern — kritik event'lerin ana işlemle aynı
/// transaction'da kaydedilip güvenli işlenmesini sağlar.
/// </summary>
public class OutboxMessage : BaseEntity
{
    public string EventType { get; private set; } = default!;

    /// <summary>
    /// JSONB column — event payload'ı. Seri hale getirilmiş JSON string.
    /// </summary>
    public string Payload { get; private set; } = default!;

    /// <summary>
    /// 1 = Bekliyor, 2 = İşlendi, 3 = Hata/Retry, 4 = Başarısız (max retry)
    /// </summary>
    public int Status { get; private set; } = 1;

    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; } = 5;
    public DateTime NextRetryAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(string eventType, string jsonPayload)
    {
        return new OutboxMessage
        {
            EventType = eventType,
            Payload = jsonPayload,
            Status = 1,
            NextRetryAt = DateTime.UtcNow
        };
    }

    public void MarkProcessed()
    {
        Status = 2;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        AttemptCount++;
        ErrorMessage = errorMessage;

        if (AttemptCount >= MaxAttempts)
        {
            Status = 4;
        }
        else
        {
            Status = 3;
            // Exponential backoff: 5s, 25s, 125s, 625s, 3125s
            NextRetryAt = DateTime.UtcNow.AddSeconds(Math.Pow(5, AttemptCount));
        }
    }
}
