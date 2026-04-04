using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Infrastructure.Services;

/// <summary>
/// Döviz kuru servisi.
/// GetRate() akışı (öncelik sırası):
///   1. exchange_rate_overrides — şube/şirket bazlı manuel geçersiz kılmalar
///   2. exchange_rates          — TCMB/ECB'den çekilen günlük kurlar
///   3. En son bilinen kur      — fallback (belirtilen tarihten önce)
/// Sonuçlar 1 saat Redis cache'e alınır.
/// </summary>
public class ExchangeRateService : IExchangeRateService
{
    private readonly AppDbContext          _db;
    private readonly IDistributedCache     _cache;
    private readonly ILogger<ExchangeRateService> _logger;

    public ExchangeRateService(
        AppDbContext db,
        IDistributedCache cache,
        ILogger<ExchangeRateService> logger)
    {
        _db     = db;
        _cache  = cache;
        _logger = logger;
    }

    /// <summary>
    /// Belirtilen para birimi ve tarih için TRY kurununu döner.
    /// currency = "TRY" ise her zaman 1 döner.
    /// </summary>
    public async Task<decimal> GetRate(
        string currency,
        DateOnly date,
        long? companyId = null,
        long? branchId  = null,
        CancellationToken ct = default)
    {
        if (string.Equals(currency, "TRY", StringComparison.OrdinalIgnoreCase))
            return 1m;

        var cacheKey = $"exrate:{currency.ToUpperInvariant()}:{date:yyyy-MM-dd}:" +
                       $"c{companyId ?? 0}:b{branchId ?? 0}";

        // ── Cache kontrolü ─────────────────────────────────────────────
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached is not null && decimal.TryParse(cached, out var cachedRate))
            return cachedRate;

        var rate = await ResolveRate(currency.ToUpperInvariant(), date, companyId, branchId, ct);

        await _cache.SetStringAsync(
            cacheKey,
            rate.ToString("F6"),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) },
            ct);

        return rate;
    }

    /// <summary>Belirli bir tarih için tüm kurları döner (dict: currency → rate).</summary>
    public async Task<Dictionary<string, decimal>> GetRatesForDate(
        DateOnly date,
        long? companyId = null,
        long? branchId  = null,
        CancellationToken ct = default)
    {
        // Aktif para birimleri — exchange_rates tablosundaki benzersiz from_currency'ler
        var currencies = await _db.ExchangeRates
            .Select(r => r.FromCurrency)
            .Distinct()
            .ToListAsync(ct);

        var result = new Dictionary<string, decimal>();
        foreach (var c in currencies)
            result[c] = await GetRate(c, date, companyId, branchId, ct);

        return result;
    }

    // ── Özel metotlar ─────────────────────────────────────────────────────

    private async Task<decimal> ResolveRate(
        string currency,
        DateOnly date,
        long? companyId,
        long? branchId,
        CancellationToken ct)
    {
        // ── Adım 1: Manuel geçersiz kılma (exchange_rate_overrides) ───────
        if (companyId.HasValue)
        {
            var overrideRate = await _db.ExchangeRateOverrides
                .Where(o => o.CompanyId == companyId
                         && (o.BranchId == null || o.BranchId == branchId)
                         && o.Currency  == currency
                         && o.IsActive
                         && o.ValidFrom <= date
                         && (o.ValidUntil == null || o.ValidUntil >= date))
                .OrderByDescending(o => o.BranchId.HasValue) // branch-specific önce
                .Select(o => (decimal?)o.Rate)
                .FirstOrDefaultAsync(ct);

            if (overrideRate.HasValue)
            {
                _logger.LogDebug("ExchangeRate override kullanıldı: {Currency} {Date} → {Rate}",
                    currency, date, overrideRate.Value);
                return overrideRate.Value;
            }
        }

        // ── Adım 2: TCMB/ECB kur tablosu (exchange_rates) ────────────────
        var exactRate = await _db.ExchangeRates
            .Where(r => r.FromCurrency == currency
                     && r.ToCurrency   == "TRY"
                     && r.RateDate     == date)
            .Select(r => (decimal?)r.Rate)
            .FirstOrDefaultAsync(ct);

        if (exactRate.HasValue)
            return exactRate.Value;

        // ── Adım 3: En son bilinen kur (fallback) ─────────────────────────
        var fallback = await _db.ExchangeRates
            .Where(r => r.FromCurrency == currency
                     && r.ToCurrency   == "TRY"
                     && r.RateDate     <= date)
            .OrderByDescending(r => r.RateDate)
            .Select(r => (decimal?)r.Rate)
            .FirstOrDefaultAsync(ct);

        if (fallback.HasValue)
        {
            _logger.LogWarning(
                "ExchangeRate fallback kullanıldı: {Currency} {Date} — en son kur tarihinden önceki değer.",
                currency, date);
            return fallback.Value;
        }

        _logger.LogError("ExchangeRate bulunamadı: {Currency} {Date}", currency, date);
        throw new InvalidOperationException(
            $"'{currency}' için {date:yyyy-MM-dd} tarihinde döviz kuru bulunamadı.");
    }

    /// <summary>Cache'i geçersiz kıl (kur güncellemesi sonrası çağrılır).</summary>
    public async Task InvalidateCacheAsync(string currency, DateOnly date, CancellationToken ct = default)
    {
        // Tüm company/branch kombinasyonları için genel pattern — Redis'te prefix silme
        // Simple approach: wildcard desteklenmediği için sadece genel cache'i sil
        var cacheKey = $"exrate:{currency.ToUpperInvariant()}:{date:yyyy-MM-dd}:c0:b0";
        await _cache.RemoveAsync(cacheKey, ct);
    }
}
