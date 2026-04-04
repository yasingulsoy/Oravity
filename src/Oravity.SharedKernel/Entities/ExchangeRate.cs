namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Döviz kuru kaydı.
/// TCMB veya diğer kaynaklardan çekilen günlük alış/satış kurları.
/// Benzersizlik: (FromCurrency, ToCurrency, RateDate) üçlüsü.
/// </summary>
public class ExchangeRate
{
    public long Id { get; private set; }

    /// <summary>Kaynak para birimi (örn. "EUR").</summary>
    public string FromCurrency { get; private set; } = default!;
    /// <summary>Hedef para birimi (genellikle "TRY").</summary>
    public string ToCurrency { get; private set; } = "TRY";

    /// <summary>Döviz kuru (1 FromCurrency = Rate ToCurrency).</summary>
    public decimal Rate { get; private set; }
    /// <summary>Kurun geçerli olduğu tarih.</summary>
    public DateOnly RateDate { get; private set; }

    /// <summary>Kaynak: 'tcmb', 'ecb', 'manual'.</summary>
    public string Source { get; private set; } = "tcmb";

    public DateTime CreatedAt { get; private set; }

    private ExchangeRate() { }

    public static ExchangeRate Create(
        string fromCurrency,
        decimal rate,
        DateOnly rateDate,
        string source = "tcmb",
        string toCurrency = "TRY")
    {
        if (rate <= 0)
            throw new ArgumentException("Kur değeri sıfırdan büyük olmalıdır.", nameof(rate));

        return new ExchangeRate
        {
            FromCurrency = fromCurrency.ToUpperInvariant(),
            ToCurrency   = toCurrency.ToUpperInvariant(),
            Rate         = rate,
            RateDate     = rateDate,
            Source       = source,
            CreatedAt    = DateTime.UtcNow
        };
    }
}
