using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Oravity.Infrastructure.Database;

namespace Oravity.Core.Modules.Core.Localization.Application.Services;

/// <summary>
/// Çeviri servisi (SPEC §ÇOKLU DİL §4).
/// IMemoryCache ile 1 saatlik TTL uygular.
/// Eksik çevirilerde sırayla TR'ye → key'e düşer.
/// </summary>
public class TranslationService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TranslationService> _logger;

    public TranslationService(AppDbContext db, IMemoryCache cache, ILogger<TranslationService> logger)
    {
        _db     = db;
        _cache  = cache;
        _logger = logger;
    }

    /// <summary>
    /// Tek bir anahtar için çeviri döndürür.
    /// Args varsa {name} → değer replace eder.
    /// </summary>
    public async Task<string> Get(
        string key,
        string langCode,
        Dictionary<string, string>? args = null,
        CancellationToken ct = default)
    {
        var cacheKey = $"trans:{langCode}:{key}";

        var value = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

            // 1. İstenen dilde ara
            var found = await _db.Translations
                .Where(t => t.TranslationKey.Key == key && t.Language.Code == langCode)
                .Select(t => t.Value)
                .FirstOrDefaultAsync(ct);

            if (found is not null)
                return found;

            // 2. TR'ye düş (varsayılan dil)
            if (langCode != "tr")
            {
                found = await _db.Translations
                    .Where(t => t.TranslationKey.Key == key && t.Language.Code == "tr")
                    .Select(t => t.Value)
                    .FirstOrDefaultAsync(ct);
            }

            // 3. Son çare: key kendisi
            return found ?? key;
        });

        if (value is null) return key;

        // Parametre değiştirme: "Merhaba {name}" → "Merhaba Ali"
        if (args is not null)
            foreach (var (k, v) in args)
                value = value.Replace($"{{{k}}}", v);

        return value;
    }

    /// <summary>
    /// Bir dilin tüm çevirilerini Dictionary olarak döndürür.
    /// Frontend cache için; tüm dataset cachelendiğinde bireysel GetOrCreate'ten daha verimli.
    /// </summary>
    public async Task<Dictionary<string, string>> GetAll(string langCode, CancellationToken ct = default)
    {
        var cacheKey = $"trans:all:{langCode}";

        var dict = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

            return await _db.Translations
                .Where(t => t.Language.Code == langCode)
                .Select(t => new { t.TranslationKey.Key, t.Value })
                .ToDictionaryAsync(x => x.Key, x => x.Value, ct);
        });

        return dict ?? [];
    }

    /// <summary>
    /// Belirtilen dil+anahtar kombinasyonunun cache'ini temizler.
    /// GetAll cache'ini de temizler (veri değiştiğinde stale kalmaması için).
    /// </summary>
    public void InvalidateCache(string langCode, string? key = null)
    {
        if (key is not null)
            _cache.Remove($"trans:{langCode}:{key}");

        _cache.Remove($"trans:all:{langCode}");
        _logger.LogDebug("TranslationService cache temizlendi → lang={Lang}, key={Key}", langCode, key ?? "*");
    }
}
