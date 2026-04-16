using System.Text.Json;
using Oravity.SharedKernel.Entities;

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
    public string? AppliedRuleName  { get; init; }
    public string  Currency         { get; init; } = "TRY";
}

/// <summary>
/// Kural motoru için tedavi + referans fiyat bağlamı.
/// </summary>
public record RuleEvalContext
{
    public long    TreatmentId        { get; init; }
    public long?   CategoryId         { get; init; }
    public Guid?   CategoryPublicId   { get; init; }
    public string  TreatmentCode      { get; init; } = string.Empty;

    /// <summary>Referans listelerinden çekilen fiyatlar: ListCode → Price</summary>
    public IReadOnlyDictionary<string, decimal> ReferencePrices { get; init; }
        = new Dictionary<string, decimal>();

    /// <summary>Hastanın anlaşmalı kurumu (varsa). Formül filtreleri ve ISAK değişkeni için kullanılır.</summary>
    public long?   InstitutionId { get; init; }

    /// <summary>Anlaşmalı kurum hastası mı? (InstitutionId > 0 ise otomatik true)</summary>
    public bool    IsInstitutionAgreement => InstitutionId.HasValue;

    /// <summary>ÖSS kapsamında mı?</summary>
    public bool    IsOss { get; init; }

    /// <summary>Aktif kampanya kodu (varsa)</summary>
    public string? CampaignCode { get; init; }

    /// <summary>
    /// Şubeye özel cari fiyat çarpanı — formüllerde MULTI değişkeni.
    /// Varsayılan 1.0. Bodrum gibi şubeler için 1.10 vb. girilebilir.
    /// </summary>
    public decimal PricingMultiplier { get; init; } = 1.0m;
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

    /// <summary>
    /// Şirketin PricingRule listesini sırayla uygulayarak tedavi fiyatını hesaplar.
    /// Kural yoksa veya eşleşen kural bulunamazsa null döner.
    /// </summary>
    public PricingResult? CalculateWithRules(
        RuleEvalContext ctx,
        IReadOnlyList<PricingRule> rules)
    {
        var now = DateTime.UtcNow;
        PricingResult? lastResult = null;

        foreach (var rule in rules.OrderBy(r => r.Priority))
        {
            if (!rule.IsActive) continue;
            if (rule.ValidFrom.HasValue  && rule.ValidFrom.Value  > now) continue;
            if (rule.ValidUntil.HasValue && rule.ValidUntil.Value < now) continue;

            if (!MatchesFilters(ctx, rule.IncludeFilters, rule.ExcludeFilters)) continue;

            var vars = BuildFormulaVariables(ctx);
            var tdb = vars["TDB"];

            decimal finalPrice;
            try
            {
                finalPrice = rule.RuleType switch
                {
                    "formula" when !string.IsNullOrWhiteSpace(rule.Formula) =>
                        _formula.Evaluate(rule.Formula, vars),

                    "percentage" when !string.IsNullOrWhiteSpace(rule.Formula) =>
                        tdb * (1m - decimal.Parse(rule.Formula, System.Globalization.CultureInfo.InvariantCulture) / 100m),

                    "fixed" when !string.IsNullOrWhiteSpace(rule.Formula) =>
                        decimal.Parse(rule.Formula, System.Globalization.CultureInfo.InvariantCulture),

                    _ => tdb
                };
            }
            catch
            {
                continue;
            }

            finalPrice = Math.Max(0, Math.Round(finalPrice, 2));

            var result = new PricingResult
            {
                OriginalPrice  = tdb,
                FinalPrice     = finalPrice,
                TotalDiscount  = Math.Max(0, tdb - finalPrice),
                AppliedStrategy = rule.RuleType,
                AppliedRuleName = rule.Name,
                Currency        = rule.OutputCurrency,
            };

            if (rule.StopProcessing) return result;

            lastResult = result;
        }

        return lastResult;
    }

    /// <summary>
    /// RuleEvalContext'teki referans fiyat sözlüğünden formül değişkenlerini çözümler.
    /// TDB_2026 → TDB, CARI_2026 → CARI, SUT_2025 → SUT, ISAK_2026 → ISAK şeklinde
    /// prefix eşleştirmesi yapar.
    /// </summary>
    private static Dictionary<string, decimal> BuildFormulaVariables(RuleEvalContext ctx)
    {
        decimal Resolve(string prefix, decimal fallback)
        {
            var key = ctx.ReferencePrices.Keys
                .FirstOrDefault(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            return key != null ? ctx.ReferencePrices[key] : fallback;
        }

        var tdb  = Resolve("TDB",  ctx.ReferencePrices.Values.FirstOrDefault());
        var cari = Resolve("CARI", tdb);
        var sut  = Resolve("SUT",  tdb);
        var isak = Resolve("ISAK", 0m);

        return new Dictionary<string, decimal>
        {
            ["TDB"]   = tdb,
            ["CARI"]  = cari,
            ["SUT"]   = sut,
            ["ISAK"]  = isak,
            ["MULTI"] = ctx.PricingMultiplier,
        };
    }

    // ─── Yardımcılar ─────────────────────────────────────────────────────────

    private static bool MatchesFilters(RuleEvalContext ctx, string? includeJson, string? excludeJson)
    {
        // ExcludeFilters: eşleşirse kural uygulanmaz
        if (!string.IsNullOrWhiteSpace(excludeJson) && FilterMatches(ctx, excludeJson))
            return false;

        // IncludeFilters: null ise tüm tedavilere uygulanır
        if (string.IsNullOrWhiteSpace(includeJson))
            return true;

        return FilterMatches(ctx, includeJson);
    }

    /// <summary>
    /// Filter JSON şeması:
    /// {
    ///   "treatmentIds": [1,2],
    ///   "categoryIds": [3,4],
    ///   "institutionAgreement": true,   // sadece anlaşmalı kurum hastası
    ///   "ossOnly": true,                // sadece ÖSS kapsamı
    ///   "campaignCodes": ["YAZ2026"]    // aktif kampanya kodu
    /// }
    /// Tüm belirtilen koşullar sağlanırsa true döner (AND mantığı).
    /// treatmentIds/categoryIds OR mantığıyla çalışır (en az biri eşleşmeli).
    /// </summary>
    private static bool FilterMatches(RuleEvalContext ctx, string json)
    {
        try
        {
            var doc  = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // ─── Koşul kriterleri (AND) ──────────────────────────────────

            // Belirli kurumlar: "institutionIds": [42, 73]
            if (root.TryGetProperty("institutionIds", out var instIds)
                && instIds.ValueKind == JsonValueKind.Array
                && instIds.GetArrayLength() > 0)
            {
                if (!ctx.InstitutionId.HasValue) return false;
                var match = false;
                foreach (var id in instIds.EnumerateArray())
                    if (id.GetInt64() == ctx.InstitutionId.Value) { match = true; break; }
                if (!match) return false;
            }

            // Sadece anlaşmalı kurum (herhangi): "institutionAgreement": true
            if (root.TryGetProperty("institutionAgreement", out var instProp)
                && instProp.GetBoolean()
                && !ctx.IsInstitutionAgreement)
                return false;

            if (root.TryGetProperty("ossOnly", out var ossProp)
                && ossProp.GetBoolean()
                && !ctx.IsOss)
                return false;

            if (root.TryGetProperty("campaignCodes", out var campaignArr)
                && campaignArr.ValueKind == JsonValueKind.Array)
            {
                var codes = campaignArr.EnumerateArray().Select(c => c.GetString()).ToList();
                if (codes.Count > 0 && (ctx.CampaignCode == null
                    || !codes.Any(c => string.Equals(c, ctx.CampaignCode, StringComparison.OrdinalIgnoreCase))))
                    return false;
            }

            // ─── Tedavi / kategori filtreleri (OR) ──────────────────────
            bool hasTreatmentFilter = root.TryGetProperty("treatmentIds", out var tIds)
                                      && tIds.ValueKind == JsonValueKind.Array
                                      && tIds.GetArrayLength() > 0;

            bool hasCategoryFilter  = root.TryGetProperty("categoryIds", out var cIds)
                                      && cIds.ValueKind == JsonValueKind.Array
                                      && cIds.GetArrayLength() > 0;

            if (!hasTreatmentFilter && !hasCategoryFilter)
                return true; // Tedavi filtresi yok → koşul kriterler geçtiyse eşleşti

            if (hasTreatmentFilter)
                foreach (var id in tIds.EnumerateArray())
                    if (id.GetInt64() == ctx.TreatmentId) return true;

            if (hasCategoryFilter && ctx.CategoryId.HasValue)
                foreach (var id in cIds.EnumerateArray())
                    if (id.GetInt64() == ctx.CategoryId.Value) return true;

            // categoryPublicIds: GUID dizisi (frontend'den gelen publicId formatı)
            bool hasCategoryPublicIdFilter = root.TryGetProperty("categoryPublicIds", out var cpIds)
                                             && cpIds.ValueKind == JsonValueKind.Array
                                             && cpIds.GetArrayLength() > 0;
            if (hasCategoryPublicIdFilter && ctx.CategoryPublicId.HasValue)
                foreach (var id in cpIds.EnumerateArray())
                    if (Guid.TryParse(id.GetString(), out var g) && g == ctx.CategoryPublicId.Value) return true;

            if (!hasTreatmentFilter && !hasCategoryFilter && hasCategoryPublicIdFilter)
                return false; // had public id filter but nothing matched

            return false;
        }
        catch
        {
            return false;
        }
    }
}
