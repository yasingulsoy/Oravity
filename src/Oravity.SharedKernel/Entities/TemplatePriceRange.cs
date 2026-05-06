namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hakediş şablonu Fiyat Bandı (PriceRange) tipinde; brüt tedavi bedeline göre
/// hangi prim oranının uygulanacağını belirler.
/// Bant sırası MinAmount'a göre artan; ilk eşleşen bant kazanır.
/// Örnek: 0–5000 → %25, 5000–15000 → %30, 15000+ → %35
/// </summary>
public class TemplatePriceRange
{
    public long Id { get; private set; }

    public long TemplateId { get; private set; }
    public DoctorCommissionTemplate Template { get; private set; } = default!;

    /// <summary>Bandın alt sınırı (dahil).</summary>
    public decimal MinAmount { get; private set; }
    /// <summary>Bandın üst sınırı (hariç). null = açık uçlu ("ve üzeri").</summary>
    public decimal? MaxAmount { get; private set; }
    /// <summary>Bu bant için uygulanacak prim oranı (0–100 %).</summary>
    public decimal Rate { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private TemplatePriceRange() { }

    public static TemplatePriceRange Create(
        long templateId, decimal minAmount, decimal? maxAmount, decimal rate)
    {
        if (minAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(minAmount));
        if (maxAmount.HasValue && maxAmount.Value <= minAmount)
            throw new ArgumentOutOfRangeException(nameof(maxAmount), "MaxAmount MinAmount'tan büyük olmalı.");
        if (rate < 0 || rate > 100)
            throw new ArgumentOutOfRangeException(nameof(rate), "Oran 0–100 arasında olmalı.");

        return new TemplatePriceRange
        {
            TemplateId = templateId,
            MinAmount  = minAmount,
            MaxAmount  = maxAmount,
            Rate       = rate,
            CreatedAt  = DateTime.UtcNow,
        };
    }

    public void Update(decimal minAmount, decimal? maxAmount, decimal rate)
    {
        if (minAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(minAmount));
        if (maxAmount.HasValue && maxAmount.Value <= minAmount)
            throw new ArgumentOutOfRangeException(nameof(maxAmount), "MaxAmount MinAmount'tan büyük olmalı.");
        if (rate < 0 || rate > 100)
            throw new ArgumentOutOfRangeException(nameof(rate), "Oran 0–100 arasında olmalı.");

        MinAmount = minAmount;
        MaxAmount = maxAmount;
        Rate      = rate;
    }
}
