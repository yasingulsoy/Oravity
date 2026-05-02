using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Online ödeme firması (PayTR, iyzico, Stripe, vb.).
/// Platform-geneli referans tablo; entegrasyon ayarları ileriki fazda eklenecek.
/// </summary>
public class PaymentProvider : BaseEntity
{
    public string Name { get; private set; } = string.Empty;   // "PayTR"
    public string? ShortName { get; private set; }             // "PayTR"
    public string? Website { get; private set; }               // "https://www.paytr.com"
    public bool IsActive { get; private set; } = true;

    private PaymentProvider() { }

    public static PaymentProvider Create(string name, string? shortName = null, string? website = null) =>
        new() { Name = name, ShortName = shortName, Website = website };

    public void Update(string name, string? shortName, string? website)
    {
        Name      = name;
        ShortName = shortName;
        Website   = website;
        MarkUpdated();
    }

    public void Deactivate() { IsActive = false; MarkUpdated(); }
    public void Activate()   { IsActive = true;  MarkUpdated(); }
}
