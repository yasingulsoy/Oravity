namespace Oravity.SharedKernel.Entities;

public enum SmsStatus
{
    Queued   = 1,  // Kuyrukta
    Sent     = 2,  // Gönderildi
    Error    = 3,  // Hata
    IysRejected = 4  // İYS Reddi
}

/// <summary>
/// SMS gönderim kuyruğu (SPEC §İLETİŞİM ALTYAPISI — SMS).
/// Hangfire SmsDispatchService tarafından her dakika işlenir.
/// Exponential backoff: 1. hata → 5 dk, 2. hata → 15 dk, 3. hata → 60 dk
/// BaseEntity türemez — public_id / is_deleted gerekmez.
/// </summary>
public class SmsQueue
{
    public long Id { get; private set; }

    public long CompanyId { get; private set; }
    public Company Company { get; private set; } = default!;

    /// <summary>SMS sağlayıcı referans ID. providers tablosu ayrı implement edilecek.</summary>
    public int ProviderId { get; private set; }

    public string ToPhone { get; private set; } = default!;
    public string Message { get; private set; } = default!;

    /// <summary>Kaynak tipi: APPOINTMENT_REMINDER, OTP, PAYMENT_REMINDER, MANUAL vb.</summary>
    public string SourceType { get; private set; } = default!;

    public SmsStatus Status { get; private set; } = SmsStatus.Queued;

    public int AttemptCount { get; private set; }

    /// <summary>Bir sonraki deneme zamanı. İlk sıraya girişte null.</summary>
    public DateTime? NextRetryAt { get; private set; }

    public string? ErrorMessage { get; private set; }

    /// <summary>Başarılı gönderimde sağlayıcının döndürdüğü mesaj ID.</summary>
    public string? ProviderMessageId { get; private set; }

    public DateTime? SentAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SmsQueue() { }

    public static SmsQueue Create(
        long companyId,
        int providerId,
        string toPhone,
        string message,
        string sourceType)
    {
        return new SmsQueue
        {
            CompanyId   = companyId,
            ProviderId  = providerId,
            ToPhone     = toPhone,
            Message     = message,
            SourceType  = sourceType,
            Status      = SmsStatus.Queued,
            AttemptCount = 0,
            CreatedAt   = DateTime.UtcNow
        };
    }

    public void MarkSent(string? providerMessageId)
    {
        Status            = SmsStatus.Sent;
        SentAt            = DateTime.UtcNow;
        ProviderMessageId = providerMessageId;
    }

    public void MarkFailed(string errorMessage, int maxAttempts = 3)
    {
        AttemptCount++;
        ErrorMessage = errorMessage;

        if (AttemptCount >= maxAttempts)
        {
            Status = SmsStatus.Error;
            return;
        }

        // Exponential backoff: 5 dk → 15 dk → 60 dk
        var delayMinutes = AttemptCount switch
        {
            1 => 5,
            2 => 15,
            _ => 60
        };
        Status      = SmsStatus.Queued;
        NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
    }

    public void MarkIysRejected()
    {
        Status = SmsStatus.IysRejected;
    }
}
