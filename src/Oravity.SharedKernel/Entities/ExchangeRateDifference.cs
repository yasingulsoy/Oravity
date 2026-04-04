namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Döviz kuru farkı kaydı.
/// Fatura/ödeme kesildiği andaki kur ile fiili tahsilat/ödeme anındaki kur
/// arasındaki farktan doğan kâr/zarar tutarını izler.
/// </summary>
public class ExchangeRateDifference
{
    public long Id { get; private set; }

    public long CompanyId { get; private set; }
    public Company Company { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    // ── Kaynak işlem ─────────────────────────────────────────────────────
    /// <summary>Kaynak tablo: 'payment', 'einvoice', 'doctor_commission'.</summary>
    public string SourceType { get; private set; } = default!;
    public long SourceId { get; private set; }

    // ── Kur bilgileri ────────────────────────────────────────────────────
    public string Currency { get; private set; } = default!;
    /// <summary>Orijinal işlemdeki kur (fatura/plan anı).</summary>
    public decimal OriginalRate { get; private set; }
    /// <summary>Fiili işlemdeki kur (tahsilat/ödeme anı).</summary>
    public decimal ActualRate { get; private set; }
    /// <summary>İşlemin dövizli tutarı.</summary>
    public decimal ForeignAmount { get; private set; }

    // ── Hesaplanan fark ──────────────────────────────────────────────────
    /// <summary>
    /// Kur farkı TRY = ForeignAmount × (ActualRate − OriginalRate).
    /// Pozitif = kâr, Negatif = zarar.
    /// </summary>
    public decimal DifferenceAmount { get; private set; }

    /// <summary>Kur farkı tipi: 1=Kâr, 2=Zarar.</summary>
    public int DifferenceType { get; private set; }

    public DateTime RecordedAt { get; private set; }
    public string? Notes { get; private set; }

    private ExchangeRateDifference() { }

    public static ExchangeRateDifference Create(
        long companyId,
        long branchId,
        string sourceType,
        long sourceId,
        string currency,
        decimal originalRate,
        decimal actualRate,
        decimal foreignAmount,
        string? notes = null)
    {
        if (originalRate <= 0 || actualRate <= 0)
            throw new ArgumentException("Kur değerleri sıfırdan büyük olmalıdır.");

        var diff     = Math.Round(foreignAmount * (actualRate - originalRate), 4);
        var diffType = diff >= 0 ? 1 : 2; // 1=Kâr, 2=Zarar

        return new ExchangeRateDifference
        {
            CompanyId        = companyId,
            BranchId         = branchId,
            SourceType       = sourceType,
            SourceId         = sourceId,
            Currency         = currency.ToUpperInvariant(),
            OriginalRate     = originalRate,
            ActualRate       = actualRate,
            ForeignAmount    = foreignAmount,
            DifferenceAmount = Math.Abs(diff),
            DifferenceType   = diffType,
            RecordedAt       = DateTime.UtcNow,
            Notes            = notes
        };
    }
}
