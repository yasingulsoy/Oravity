namespace Oravity.SharedKernel.Entities;

/// <summary>
/// E-fatura kalem satırı (SPEC §E-FATURA §2 einvoice_items).
/// Her kalem için ayrı KDV oranı tanımlanabilir (estetik %20, tedavi %10).
/// </summary>
public class EInvoiceItem
{
    public long Id { get; private set; }

    public long EInvoiceId { get; private set; }
    public EInvoice EInvoice { get; private set; } = default!;

    public int SortOrder { get; private set; }
    public string Description { get; private set; } = default!; // "Kompozit Dolgu - 13 No'lu Diş"

    public decimal Quantity { get; private set; } = 1m;
    public string Unit { get; private set; } = "Adet";

    public decimal UnitPrice { get; private set; }
    public decimal DiscountRate { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxRate { get; private set; } = 10m;
    public decimal TaxAmount { get; private set; }
    /// <summary>KDV dahil satır toplamı</summary>
    public decimal Total { get; private set; }

    private EInvoiceItem() { }

    public static EInvoiceItem Create(
        long einvoiceId,
        string description,
        decimal unitPrice,
        decimal quantity = 1m,
        decimal discountRate = 0m,
        decimal taxRate = 10m,
        string unit = "Adet",
        int sortOrder = 0)
    {
        var discountAmount = Math.Round(unitPrice * quantity * (discountRate / 100m), 2);
        var taxableAmount  = unitPrice * quantity - discountAmount;
        var taxAmount      = Math.Round(taxableAmount * (taxRate / 100m), 2);
        var total          = taxableAmount + taxAmount;

        return new EInvoiceItem
        {
            EInvoiceId     = einvoiceId,
            SortOrder      = sortOrder,
            Description    = description,
            Quantity       = quantity,
            Unit           = unit,
            UnitPrice      = unitPrice,
            DiscountRate   = discountRate,
            DiscountAmount = discountAmount,
            TaxRate        = taxRate,
            TaxAmount      = taxAmount,
            Total          = total
        };
    }
}
