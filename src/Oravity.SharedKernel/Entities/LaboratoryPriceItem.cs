using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Laboratuvara ait işlem fiyat kalemi (ör: "Zirkonyum Kuron", "Metal Porselen").
/// </summary>
public class LaboratoryPriceItem : AuditableEntity
{
    public long LaboratoryId { get; private set; }
    public Laboratory Laboratory { get; private set; } = default!;

    public string ItemName { get; private set; } = default!;
    public string? ItemCode { get; private set; }
    public string? Description { get; private set; }

    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "TRY";

    /// <summary>'per_tooth' | 'per_unit' | 'fixed'</summary>
    public string? PricingType { get; private set; }

    /// <summary>Tahmini üretim/teslim süresi (gün).</summary>
    public int? EstimatedDeliveryDays { get; private set; }

    /// <summary>Kategori: Zirkonyum / Porselen / Protez / Plak / Implant vb.</summary>
    public string? Category { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateOnly? ValidFrom { get; private set; }
    public DateOnly? ValidUntil { get; private set; }

    private LaboratoryPriceItem() { }

    public static LaboratoryPriceItem Create(
        long laboratoryId,
        string itemName,
        string? itemCode,
        string? description,
        decimal price,
        string currency,
        string? pricingType,
        int? estimatedDeliveryDays,
        string? category,
        DateOnly? validFrom,
        DateOnly? validUntil)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            throw new ArgumentException("İşlem adı boş olamaz.", nameof(itemName));
        if (price < 0) throw new ArgumentException("Fiyat negatif olamaz.", nameof(price));

        return new LaboratoryPriceItem
        {
            LaboratoryId          = laboratoryId,
            ItemName              = itemName.Trim(),
            ItemCode              = itemCode?.Trim(),
            Description           = description,
            Price                 = price,
            Currency              = string.IsNullOrWhiteSpace(currency) ? "TRY" : currency.Trim().ToUpperInvariant(),
            PricingType           = pricingType?.Trim().ToLowerInvariant(),
            EstimatedDeliveryDays = estimatedDeliveryDays,
            Category              = category?.Trim(),
            ValidFrom             = validFrom,
            ValidUntil            = validUntil,
            IsActive              = true,
        };
    }

    public void Update(
        string itemName,
        string? itemCode,
        string? description,
        decimal price,
        string currency,
        string? pricingType,
        int? estimatedDeliveryDays,
        string? category,
        DateOnly? validFrom,
        DateOnly? validUntil)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            throw new ArgumentException("İşlem adı boş olamaz.", nameof(itemName));
        if (price < 0) throw new ArgumentException("Fiyat negatif olamaz.", nameof(price));

        ItemName              = itemName.Trim();
        ItemCode              = itemCode?.Trim();
        Description           = description;
        Price                 = price;
        Currency              = string.IsNullOrWhiteSpace(currency) ? "TRY" : currency.Trim().ToUpperInvariant();
        PricingType           = pricingType?.Trim().ToLowerInvariant();
        EstimatedDeliveryDays = estimatedDeliveryDays;
        Category              = category?.Trim();
        ValidFrom             = validFrom;
        ValidUntil            = validUntil;
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }
}
