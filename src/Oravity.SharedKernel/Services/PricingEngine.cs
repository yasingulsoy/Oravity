namespace Oravity.SharedKernel.Services;

// ─── DTOs ────────────────────────────────────────────────────────────────

public record PricingContext
{
    /// <summary>Temel (SUT) fiyat</summary>
    public decimal BasePrice          { get; init; }

    /// <summary>Kurumun özel override fiyatı; null ise kullanılmaz</summary>
    public decimal? InstitutionPrice  { get; init; }

    /// <summary>Kampanya indirim oranı (0–100)</summary>
    public decimal? CampaignDiscountRate { get; init; }

    /// <summary>Anlaşmalı sigorta/kurum kodu (ISAK=1 ise indirim var)</summary>
    public bool IsInstitutionAgreement { get; init; }
}

public record PricingResult
{
    public decimal FinalPrice       { get; init; }
    public decimal OriginalPrice    { get; init; }
    public decimal TotalDiscount    { get; init; }
    public string  AppliedStrategy  { get; init; } = string.Empty;
}

// ─── PricingEngine ───────────────────────────────────────────────────────

/// <summary>
/// Fiyatlandırma öncelik sırası (SPEC §FİYATLANDIRMA POLİTİKASI):
///   1. Kurum override fiyatı      (en yüksek öncelik)
///   2. Kampanya indirimi           (override üzerine uygulanmaz; base üzerine uygulanır)
///   3. Temel fiyat                 (varsayılan)
/// </summary>
public class PricingEngine
{
    private readonly FormulaEngine _formula;

    public PricingEngine(FormulaEngine formula)
    {
        _formula = formula;
    }

    /// <summary>
    /// Bağlam bilgisinden final fiyatı hesaplar.
    /// </summary>
    public PricingResult Calculate(PricingContext ctx)
    {
        if (ctx.BasePrice < 0)
            throw new ArgumentException("Temel fiyat negatif olamaz.");

        // 1. Kurum override varsa — direkt kullan, kampanya uygulanmaz
        if (ctx.InstitutionPrice.HasValue)
        {
            var instPrice = ctx.InstitutionPrice.Value;
            return new PricingResult
            {
                OriginalPrice   = ctx.BasePrice,
                FinalPrice      = instPrice,
                TotalDiscount   = Math.Max(0, ctx.BasePrice - instPrice),
                AppliedStrategy = "InstitutionOverride"
            };
        }

        // 2. Kampanya indirimi
        if (ctx.CampaignDiscountRate.HasValue && ctx.CampaignDiscountRate.Value > 0)
        {
            var rate      = Math.Clamp(ctx.CampaignDiscountRate.Value, 0, 100);
            var discount  = ctx.BasePrice * rate / 100m;
            var final     = ctx.BasePrice - discount;
            return new PricingResult
            {
                OriginalPrice   = ctx.BasePrice,
                FinalPrice      = Math.Max(0, final),
                TotalDiscount   = discount,
                AppliedStrategy = "CampaignDiscount"
            };
        }

        // 3. Temel fiyat
        return new PricingResult
        {
            OriginalPrice   = ctx.BasePrice,
            FinalPrice      = ctx.BasePrice,
            TotalDiscount   = 0,
            AppliedStrategy = "BasePrice"
        };
    }

    /// <summary>
    /// Formül tabanlı fiyat hesaplama. Değişkenler:
    ///   ISAK  = ctx.IsInstitutionAgreement ? 1 : 0
    ///   TDB   = ctx.InstitutionPrice ?? ctx.BasePrice
    ///   CARI  = ctx.BasePrice
    ///   SUT   = ctx.BasePrice  (alias)
    /// </summary>
    public decimal EvaluateFormula(string formula, PricingContext ctx)
    {
        var vars = new Dictionary<string, decimal>
        {
            ["ISAK"] = ctx.IsInstitutionAgreement ? 1m : 0m,
            ["TDB"]  = ctx.InstitutionPrice ?? ctx.BasePrice,
            ["CARI"] = ctx.BasePrice,
            ["SUT"]  = ctx.BasePrice
        };
        return _formula.Evaluate(formula, vars);
    }
}
