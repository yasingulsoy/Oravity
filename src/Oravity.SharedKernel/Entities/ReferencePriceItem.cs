using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Referans fiyat listesindeki tek bir tedavi kalemi.
/// </summary>
public class ReferencePriceItem : BaseEntity
{
    public long ListId { get; private set; }
    public ReferencePriceList List { get; private set; } = default!;

    public string TreatmentCode { get; private set; } = default!;
    public string TreatmentName { get; private set; } = default!;

    public decimal Price { get; private set; }
    public decimal PriceKdv { get; private set; }
    public string Currency { get; private set; } = "TRY";

    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidUntil { get; private set; }

    /// <summary>JSONB — ek metadata (SGK puan, birim, notlar vb.).</summary>
    public string? Metadata { get; private set; }

    private ReferencePriceItem() { }

    public void SetPrice(decimal price, decimal priceKdv, string? currency = null)
    {
        Price    = price;
        PriceKdv = priceKdv;
        if (!string.IsNullOrWhiteSpace(currency))
            Currency = currency.ToUpperInvariant();
        MarkUpdated();
    }

    public static ReferencePriceItem Create(
        long listId,
        string treatmentCode,
        string treatmentName,
        decimal price,
        decimal priceKdv,
        string currency,
        DateTime? validFrom,
        DateTime? validUntil)
    {
        if (string.IsNullOrWhiteSpace(treatmentCode))
            throw new ArgumentException("Tedavi kodu boş olamaz.", nameof(treatmentCode));

        return new ReferencePriceItem
        {
            ListId        = listId,
            TreatmentCode = treatmentCode.Trim().ToUpperInvariant(),
            TreatmentName = treatmentName.Trim(),
            Price         = price,
            PriceKdv      = priceKdv,
            Currency      = string.IsNullOrWhiteSpace(currency) ? "TRY" : currency.ToUpperInvariant(),
            ValidFrom     = validFrom,
            ValidUntil    = validUntil
        };
    }
}
