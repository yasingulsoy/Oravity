namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Şube/şirket bazlı manuel kur geçersiz kılma.
/// Belirli bir şube veya şirket için standart TCMB kurunun yerine
/// özel bir kur uygulanmasını sağlar.
/// GetRate() akışında en yüksek öncelik olarak kontrol edilir.
/// </summary>
public class ExchangeRateOverride
{
    public long Id { get; private set; }

    public long CompanyId { get; private set; }
    public Company Company { get; private set; } = default!;

    /// <summary>Null ise tüm şubelere uygulanır.</summary>
    public long? BranchId { get; private set; }
    public Branch? Branch { get; private set; }

    /// <summary>Geçersiz kılınan para birimi (örn. "EUR").</summary>
    public string Currency { get; private set; } = default!;

    /// <summary>Uygulanacak özel kur değeri.</summary>
    public decimal Rate { get; private set; }

    /// <summary>Geçerlilik başlangıcı.</summary>
    public DateOnly ValidFrom { get; private set; }
    /// <summary>Geçerlilik sonu (null = süresiz).</summary>
    public DateOnly? ValidUntil { get; private set; }

    public bool IsActive { get; private set; } = true;
    public string? Notes { get; private set; }

    public long CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private ExchangeRateOverride() { }

    public static ExchangeRateOverride Create(
        long companyId,
        string currency,
        decimal rate,
        DateOnly validFrom,
        long createdBy,
        long? branchId = null,
        DateOnly? validUntil = null,
        string? notes = null)
    {
        if (rate <= 0)
            throw new ArgumentException("Kur değeri sıfırdan büyük olmalıdır.", nameof(rate));

        return new ExchangeRateOverride
        {
            CompanyId  = companyId,
            BranchId   = branchId,
            Currency   = currency.ToUpperInvariant(),
            Rate       = rate,
            ValidFrom  = validFrom,
            ValidUntil = validUntil,
            IsActive   = true,
            Notes      = notes,
            CreatedBy  = createdBy,
            CreatedAt  = DateTime.UtcNow
        };
    }

    public void Deactivate()
    {
        IsActive  = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRate(decimal newRate, string? notes = null)
    {
        if (newRate <= 0)
            throw new ArgumentException("Kur değeri sıfırdan büyük olmalıdır.", nameof(newRate));

        Rate      = newRate;
        Notes     = notes ?? Notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
