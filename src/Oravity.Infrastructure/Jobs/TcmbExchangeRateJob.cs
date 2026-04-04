using System.Globalization;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Infrastructure.Jobs;

/// <summary>
/// TCMB döviz kuru çekme job'ı.
/// Her gün 09:30 ve 15:30'da çalışır.
/// TCMB'nin XML gösterge kur API'sini kullanır.
/// Kur: https://www.tcmb.gov.tr/kurlar/today.xml
/// </summary>
public class TcmbExchangeRateJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory   _httpClientFactory;
    private readonly ILogger<TcmbExchangeRateJob> _logger;

    // Desteklenen para birimleri — TCMB kodları
    private static readonly string[] SupportedCurrencies =
        ["USD", "EUR", "GBP", "CHF", "JPY", "SAR", "AED", "DKK", "SEK", "NOK"];

    private const string TcmbApiUrl = "https://www.tcmb.gov.tr/kurlar/today.xml";

    public TcmbExchangeRateJob(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<TcmbExchangeRateJob> logger)
    {
        _scopeFactory      = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
    }

    /// <summary>Hangfire tarafından çağrılan ana metot.</summary>
    public async Task Execute()
    {
        _logger.LogInformation("TcmbExchangeRateJob başlatıldı.");

        try
        {
            var rates = await FetchRatesFromTcmbAsync();

            if (rates.Count == 0)
            {
                _logger.LogWarning("TCMB'den kur çekilemedi veya hiç kur bulunamadı.");
                return;
            }

            await SaveRatesAsync(rates);
            _logger.LogInformation("TCMB kurları kaydedildi: {Count} kur.", rates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TcmbExchangeRateJob hata ile tamamlandı.");
            throw;
        }
    }

    // ── TCMB XML çekme ────────────────────────────────────────────────────

    private async Task<List<(string Currency, decimal Rate)>> FetchRatesFromTcmbAsync()
    {
        var client = _httpClientFactory.CreateClient("tcmb");
        var result = new List<(string Currency, decimal Rate)>();

        try
        {
            var xml = await client.GetStringAsync(TcmbApiUrl);
            var doc = XDocument.Parse(xml);

            foreach (var currency in doc.Descendants("Currency"))
            {
                var code = currency.Attribute("CurrencyCode")?.Value;
                if (code is null || !SupportedCurrencies.Contains(code))
                    continue;

                // TCMB XML: ForexSelling veya BanknoteSelling (yoksa ForexBuying)
                var rateStr = currency.Element("ForexSelling")?.Value
                           ?? currency.Element("ForexBuying")?.Value;

                if (string.IsNullOrWhiteSpace(rateStr))
                    continue;

                // TCMB'de ondalık ayracı virgül veya nokta olabilir
                rateStr = rateStr.Replace(',', '.');

                if (!decimal.TryParse(rateStr,
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var rate) || rate <= 0)
                {
                    _logger.LogWarning("TCMB — {Code} kuru geçersiz: '{Value}'", code, rateStr);
                    continue;
                }

                // JPY için TCMB 100 yen bazında verir; bire dönüştür
                if (code == "JPY")
                    rate = rate / 100m;

                result.Add((code, rate));
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "TCMB API'ye ulaşılamadı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCMB XML ayrıştırma hatası.");
        }

        return result;
    }

    // ── Veritabanına kaydetme ─────────────────────────────────────────────

    private async Task SaveRatesAsync(List<(string Currency, decimal Rate)> rates)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cache = scope.ServiceProvider.GetRequiredService<IExchangeRateService>();

        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var (currency, rate) in rates)
        {
            // Aynı tarihte zaten var mı kontrol et (UPSERT)
            var existing = await db.ExchangeRates
                .FirstOrDefaultAsync(r => r.FromCurrency == currency
                                       && r.ToCurrency   == "TRY"
                                       && r.RateDate     == today);

            if (existing is null)
            {
                db.ExchangeRates.Add(ExchangeRate.Create(
                    fromCurrency: currency,
                    rate:         rate,
                    rateDate:     today,
                    source:       "tcmb"));
            }
            // Mevcut kaydı güncelleme yapmıyoruz — günlük en son kayıt önemlidir;
            // gerekliyse yeni satır eklenir. Snapshot bütünlüğü için eski kayıt korunur.

            // Cache'i temizle
            await cache.InvalidateCacheAsync(currency, today);
        }

        await db.SaveChangesAsync();
    }
}
