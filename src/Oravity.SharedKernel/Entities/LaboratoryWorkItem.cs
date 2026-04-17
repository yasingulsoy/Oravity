using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Bir laboratuvar iş emrinin tek bir fiyat kalemi (bir iş emri birden fazla kalem içerebilir).
/// </summary>
public class LaboratoryWorkItem : BaseEntity
{
    public long WorkId { get; private set; }
    public LaboratoryWork Work { get; private set; } = default!;

    public long? LabPriceItemId { get; private set; }
    public LaboratoryPriceItem? LabPriceItem { get; private set; }

    public string ItemName { get; private set; } = default!;
    public int Quantity { get; private set; } = 1;
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }
    public string Currency { get; private set; } = "TRY";

    public string? Notes { get; private set; }

    private LaboratoryWorkItem() { }

    public static LaboratoryWorkItem Create(
        long? labPriceItemId,
        string itemName,
        int quantity,
        decimal unitPrice,
        string currency,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            throw new ArgumentException("Kalem adı boş olamaz.", nameof(itemName));
        if (quantity <= 0) throw new ArgumentException("Miktar pozitif olmalı.", nameof(quantity));
        if (unitPrice < 0) throw new ArgumentException("Birim fiyat negatif olamaz.", nameof(unitPrice));

        return new LaboratoryWorkItem
        {
            LabPriceItemId = labPriceItemId,
            ItemName       = itemName.Trim(),
            Quantity       = quantity,
            UnitPrice      = unitPrice,
            TotalPrice     = quantity * unitPrice,
            Currency       = string.IsNullOrWhiteSpace(currency) ? "TRY" : currency.Trim().ToUpperInvariant(),
            Notes          = notes,
        };
    }
}
