namespace Oravity.SharedKernel.Interfaces;

/// <summary>
/// Döviz kuru servisi arayüzü.
/// Öncelik sırası: Manuel Override → TCMB/ECB kur tablosu → Fallback (en son bilinen kur).
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Belirtilen para birimi ve tarihe göre TRY kuru döner.
    /// currency = "TRY" → her zaman 1m.
    /// </summary>
    Task<decimal> GetRate(
        string currency,
        DateOnly date,
        long? companyId = null,
        long? branchId  = null,
        CancellationToken ct = default);

    /// <summary>Verilen tarih için tüm aktif kurları dict olarak döner.</summary>
    Task<Dictionary<string, decimal>> GetRatesForDate(
        DateOnly date,
        long? companyId = null,
        long? branchId  = null,
        CancellationToken ct = default);

    /// <summary>Redis cache'ini geçersiz kıl (kur güncellemesinden sonra).</summary>
    Task InvalidateCacheAsync(string currency, DateOnly date, CancellationToken ct = default);
}
