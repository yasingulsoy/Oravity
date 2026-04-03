using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Oravity.Core.Modules.Finance.EInvoice.Infrastructure.Adapters;

/// <summary>
/// Provider string'ine göre doğru adapter'ı döndürür (SPEC §ENTEGRATÖR §4).
/// Yeni entegratörler buraya eklenir; komutlar/handler'lar değişmez.
/// </summary>
public class EInvoiceAdapterFactory
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<EInvoiceAdapterFactory> _logger;

    public EInvoiceAdapterFactory(IServiceProvider sp, ILogger<EInvoiceAdapterFactory> logger)
    {
        _sp     = sp;
        _logger = logger;
    }

    public IEInvoiceAdapter Create(string provider)
    {
        _logger.LogDebug("EInvoiceAdapterFactory: provider={Provider}", provider);

        return provider.ToUpperInvariant() switch
        {
            "XML_EXPORT" or "GIB_PORTAL" => _sp.GetRequiredService<XmlExportAdapter>(),
            "PARASUT"                    => _sp.GetRequiredService<ParasutAdapter>(),
            "LOGO"                       => _sp.GetRequiredService<LogoAdapter>(),
            _ => throw new NotSupportedException(
                $"Desteklenmeyen e-fatura sağlayıcısı: '{provider}'. " +
                "Desteklenen: XML_EXPORT, GIB_PORTAL, PARASUT, LOGO")
        };
    }
}
