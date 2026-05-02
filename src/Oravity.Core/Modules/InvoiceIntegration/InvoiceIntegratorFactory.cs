using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.InvoiceIntegration;

public class InvoiceIntegratorFactory
{
    // Sovos endpoint'leri
    public const string SovosEArchiveTestUrl = "https://earsivwstest.fitbulut.com/ClientEArsivServicesPort.svc";
    public const string SovosEArchiveLiveUrl = "https://earsivws.fitbulut.com/ClientEArsivServicesPort.svc";
    public const string SovosEInvoiceTestUrl = "https://efaturawstest.fitbulut.com/ClientEInvoiceServices/ClientEInvoiceServicesPort.svc";
    public const string SovosEInvoiceLiveUrl = "https://efaturaws.fitbulut.com/ClientEInvoiceServices/ClientEInvoiceServicesPort.svc";

    private readonly AppDbContext _db;
    private readonly LocalCounterIntegrator _localCounter;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public InvoiceIntegratorFactory(
        AppDbContext db,
        LocalCounterIntegrator localCounter,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _db = db;
        _localCounter = localCounter;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Şubenin ayarlarına göre uygun entegratörü döner.
    /// invoiceType: "EARCHIVE" | "EINVOICE" | "NORMAL"
    /// </summary>
    public async Task<IInvoiceIntegrator> GetForBranchAsync(
        long branchId, string invoiceType = "EARCHIVE", CancellationToken ct = default)
    {
        var settings = await _db.BranchInvoiceSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BranchId == branchId, ct);

        if (settings == null || settings.IntegratorType == InvoiceIntegratorType.None)
            return _localCounter;

        return settings.IntegratorType switch
        {
            InvoiceIntegratorType.Sovos => BuildSovosIntegrator(settings, invoiceType),
            InvoiceIntegratorType.DigitalPlanet =>
                throw new NotImplementedException("Digital Planet entegratörü Faz 2'de implement edilecek."),
            InvoiceIntegratorType.Custom =>
                throw new NotImplementedException("Custom entegratör yapılandırması Faz 2'de."),
            _ => _localCounter,
        };
    }

    private IInvoiceIntegrator BuildSovosIntegrator(BranchInvoiceSettings settings, string invoiceType)
    {
        var username = settings.IntegratorUsername
            ?? throw new InvalidOperationException("Sovos: kullanıcı adı ayarlanmamış.");
        var password = settings.IntegratorPassword
            ?? throw new InvalidOperationException("Sovos: şifre ayarlanmamış.");
        var vkn = settings.CompanyVkn
            ?? throw new InvalidOperationException("Sovos: firma VKN ayarlanmamış.");

        if (invoiceType == "EINVOICE")
        {
            var endpoint = settings.IntegratorEndpoint ?? SovosEInvoiceLiveUrl;
            var http = _httpClientFactory.CreateClient("sovos-einvoice");
            return new SovosEInvoiceIntegrator(
                http,
                _loggerFactory.CreateLogger<SovosEInvoiceIntegrator>(),
                endpoint, vkn, username, password);
        }
        else
        {
            var endpoint = settings.IntegratorEndpoint ?? SovosEArchiveLiveUrl;
            var http = _httpClientFactory.CreateClient("sovos-earchive");
            return new SovosEArchiveIntegrator(
                http,
                _loggerFactory.CreateLogger<SovosEArchiveIntegrator>(),
                endpoint, vkn, username, password);
        }
    }
}
