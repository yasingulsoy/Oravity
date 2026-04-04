using System.Text.Json;
using Microsoft.Extensions.Logging;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Infrastructure.Services;

/// <summary>
/// Finansal işlem servisi.
/// Ödeme kaydı, döviz kuru entegrasyonu ve outbox event yönetimini yürütür.
/// </summary>
public class FinancialTransactionService
{
    private readonly AppDbContext            _db;
    private readonly IExchangeRateService    _exchangeRateService;
    private readonly ITenantContext          _tenantContext;
    private readonly ILogger<FinancialTransactionService> _logger;

    public FinancialTransactionService(
        AppDbContext db,
        IExchangeRateService exchangeRateService,
        ITenantContext tenantContext,
        ILogger<FinancialTransactionService> logger)
    {
        _db                  = db;
        _exchangeRateService = exchangeRateService;
        _tenantContext       = tenantContext;
        _logger              = logger;
    }

    /// <summary>
    /// Ödeme kaydeder, döviz kuru bilgilerini otomatik doldurur ve outbox event ekler.
    /// </summary>
    public async Task<Payment> RecordPayment(
        long patientId,
        decimal amount,
        PaymentMethod method,
        DateOnly paymentDate,
        string currency = "TRY",
        string? notes = null,
        CancellationToken ct = default)
    {
        var branchId  = _tenantContext.BranchId
            ?? throw new InvalidOperationException("Ödeme kaydı için şube bağlamı gereklidir.");

        // ── Döviz kuru al ─────────────────────────────────────────────────
        decimal exchangeRate = 1m;
        if (!string.Equals(currency, "TRY", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                exchangeRate = await _exchangeRateService.GetRate(
                    currency:   currency,
                    date:       paymentDate,
                    companyId:  _tenantContext.CompanyId,
                    branchId:   branchId,
                    ct:         ct);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex,
                    "Döviz kuru bulunamadı ({Currency} {Date}), kur=1 ile devam ediliyor.",
                    currency, paymentDate);
                exchangeRate = 1m;
            }
        }

        // ── Ödeme oluştur ─────────────────────────────────────────────────
        var payment = Payment.Create(
            patientId:    patientId,
            branchId:     branchId,
            amount:       amount,
            method:       method,
            paymentDate:  paymentDate,
            currency:     currency,
            exchangeRate: exchangeRate,
            notes:        notes);

        _db.Payments.Add(payment);

        // ── Outbox: PaymentReceivedEvent ──────────────────────────────────
        var payload = JsonSerializer.Serialize(new
        {
            payment.Id,
            payment.PatientId,
            payment.BranchId,
            payment.Amount,
            payment.Currency,
            payment.ExchangeRate,
            payment.BaseAmount,
            payment.Method,
            payment.PaymentDate,
            RecordedAt = DateTime.UtcNow
        });
        _db.OutboxMessages.Add(OutboxMessage.Create("PaymentReceivedEvent", payload));

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Ödeme kaydedildi: PatientId={PatientId} Amount={Amount} {Currency} (Kur: {Rate}) BaseAmount={BaseAmount} TRY",
            patientId, amount, currency, exchangeRate, payment.BaseAmount);

        return payment;
    }

    /// <summary>
    /// Dövizli tedavi plan kalemi için kur snapshot'ı oluşturur.
    /// Plan onaylandığında veya kalem oluşturulduğunda çağrılır.
    /// </summary>
    public async Task<decimal> GetRateForTreatmentItem(
        string currency,
        long? companyId,
        long? branchId,
        CancellationToken ct = default)
    {
        if (string.Equals(currency, "TRY", StringComparison.OrdinalIgnoreCase))
            return 1m;

        var today = DateOnly.FromDateTime(DateTime.Today);
        return await _exchangeRateService.GetRate(
            currency:  currency,
            date:      today,
            companyId: companyId,
            branchId:  branchId,
            ct:        ct);
    }

    /// <summary>
    /// Kur farkı oluşturur (tahsilat/ödeme anındaki kur ile orijinal kur arasındaki fark).
    /// </summary>
    public async Task RecordExchangeRateDifference(
        long companyId,
        long branchId,
        string sourceType,
        long sourceId,
        string currency,
        decimal originalRate,
        decimal foreignAmount,
        CancellationToken ct = default)
    {
        if (string.Equals(currency, "TRY", StringComparison.OrdinalIgnoreCase))
            return;

        var today       = DateOnly.FromDateTime(DateTime.Today);
        var actualRate  = await _exchangeRateService.GetRate(
            currency:  currency,
            date:      today,
            companyId: companyId,
            branchId:  branchId,
            ct:        ct);

        // Fark yok veya önemsiz ise kaydetme (eşik: 0.0001 TRY)
        var diff = Math.Abs(foreignAmount * (actualRate - originalRate));
        if (diff < 0.0001m) return;

        var record = ExchangeRateDifference.Create(
            companyId:    companyId,
            branchId:     branchId,
            sourceType:   sourceType,
            sourceId:     sourceId,
            currency:     currency,
            originalRate: originalRate,
            actualRate:   actualRate,
            foreignAmount: foreignAmount);

        _db.ExchangeRateDifferences.Add(record);
        await _db.SaveChangesAsync(ct);
    }
}
