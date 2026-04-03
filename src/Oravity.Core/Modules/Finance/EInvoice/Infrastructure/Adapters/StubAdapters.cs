using Microsoft.Extensions.Logging;

namespace Oravity.Core.Modules.Finance.EInvoice.Infrastructure.Adapters;

/// <summary>
/// Paraşüt entegratörü stub (Aşama 2 — REST API ile tamamlanacak).
/// Şu an NotImplementedException fırlatır; gerçek implementasyon
/// Paraşüt sandbox API dokümantasyonuna göre yapılacak.
/// </summary>
public class ParasutAdapter : IEInvoiceAdapter
{
    private readonly ILogger<ParasutAdapter> _logger;
    public ParasutAdapter(ILogger<ParasutAdapter> logger) => _logger = logger;

    public Task<EInvoiceResult> SendEArchive(EInvoiceRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Paraşüt adapter henüz implemente edilmedi. Bkz. SPEC §ENTEGRATÖR §6 Aşama 2.");

    public Task<EInvoiceResult> SendEInvoice(EInvoiceRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Paraşüt adapter henüz implemente edilmedi.");

    public Task<EInvoiceResult> SendESMM(EInvoiceRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Paraşüt adapter henüz implemente edilmedi.");

    public Task<EInvoiceStatus> QueryStatus(string uuid, CancellationToken ct = default)
        => throw new NotImplementedException("Paraşüt adapter henüz implemente edilmedi.");

    public Task<EInvoiceResult> Cancel(string uuid, string reason, CancellationToken ct = default)
        => throw new NotImplementedException("Paraşüt adapter henüz implemente edilmedi.");

    public Task<bool> IsGibRegistered(string vkn, CancellationToken ct = default)
        => throw new NotImplementedException("Paraşüt adapter henüz implemente edilmedi.");
}

/// <summary>
/// Logo e-Dönüşüm entegratörü stub (Aşama 2).
/// </summary>
public class LogoAdapter : IEInvoiceAdapter
{
    private readonly ILogger<LogoAdapter> _logger;
    public LogoAdapter(ILogger<LogoAdapter> logger) => _logger = logger;

    public Task<EInvoiceResult> SendEArchive(EInvoiceRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Logo adapter henüz implemente edilmedi. Bkz. SPEC §ENTEGRATÖR §6 Aşama 2.");

    public Task<EInvoiceResult> SendEInvoice(EInvoiceRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Logo adapter henüz implemente edilmedi.");

    public Task<EInvoiceResult> SendESMM(EInvoiceRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Logo adapter henüz implemente edilmedi.");

    public Task<EInvoiceStatus> QueryStatus(string uuid, CancellationToken ct = default)
        => throw new NotImplementedException("Logo adapter henüz implemente edilmedi.");

    public Task<EInvoiceResult> Cancel(string uuid, string reason, CancellationToken ct = default)
        => throw new NotImplementedException("Logo adapter henüz implemente edilmedi.");

    public Task<bool> IsGibRegistered(string vkn, CancellationToken ct = default)
        => throw new NotImplementedException("Logo adapter henüz implemente edilmedi.");
}
