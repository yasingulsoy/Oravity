namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Firma başına e-fatura entegratör ayarları (SPEC §E-FATURA §2 einvoice_integrations).
/// config alanı şifreli API kimlik bilgilerini JSONB olarak saklar.
/// </summary>
public class EInvoiceIntegration
{
    public long Id { get; private set; }

    public long CompanyId { get; private set; }

    /// <summary>
    /// Desteklenen sağlayıcılar:
    /// XML_EXPORT | GIB_PORTAL | PARASUT | LOGO | MIKRO | UYUMSOFT
    /// </summary>
    public string Provider { get; private set; } = default!;

    // Klinik / firma bilgisi
    public string Vkn { get; private set; } = default!;
    public string TaxOffice { get; private set; } = default!;
    public string CompanyTitle { get; private set; } = default!;
    public string? Address { get; private set; }

    /// <summary>JSONB — şifreli API kimlik bilgileri: username, password, alias vb.</summary>
    public string Config { get; private set; } = "{}";

    /// <summary>true = ödeme alındığında otomatik e-arşiv oluştur (Outbox PaymentReceived handler'ı kullanır)</summary>
    public bool AutoSendEArchive { get; private set; }
    public bool IsActive { get; private set; } = true;
    /// <summary>true = GİB test ortamı kullanılır</summary>
    public bool IsTestMode { get; private set; } = true;

    public DateTime CreatedAt { get; private set; }

    private EInvoiceIntegration() { }

    public static EInvoiceIntegration Create(
        long companyId,
        string provider,
        string vkn,
        string taxOffice,
        string companyTitle,
        string? address = null,
        string config = "{}",
        bool autoSendEArchive = false,
        bool isTestMode = true)
    {
        return new EInvoiceIntegration
        {
            CompanyId         = companyId,
            Provider          = provider.ToUpperInvariant(),
            Vkn               = vkn,
            TaxOffice         = taxOffice,
            CompanyTitle      = companyTitle,
            Address           = address,
            Config            = config,
            AutoSendEArchive  = autoSendEArchive,
            IsActive          = true,
            IsTestMode        = isTestMode,
            CreatedAt         = DateTime.UtcNow
        };
    }

    public void UpdateConfig(string config) => Config = config;
    public void Activate()   => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void SetTestMode(bool isTest) => IsTestMode = isTest;
    public void SetAutoSend(bool auto)   => AutoSendEArchive = auto;
}
