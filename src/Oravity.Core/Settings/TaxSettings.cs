namespace Oravity.Core.Settings;

/// <summary>
/// Türkiye KDV tevkifat eşiği ve vergisel sabitler.
/// Her yıl Maliye Bakanlığı tebliğiyle güncellenir — appsettings.json içinde tutulur.
/// </summary>
public class TaxSettings
{
    public const string SectionName = "Tax";

    /// <summary>
    /// KDV tevkifatı zorunlu olan asgari fatura tutarı (KDV dahil, TL).
    /// KDVGUT I/C-2.1.3.2 — 2026: 12.000 TL
    /// </summary>
    public decimal WithholdingThresholdTry { get; init; } = 12_000m;
}
