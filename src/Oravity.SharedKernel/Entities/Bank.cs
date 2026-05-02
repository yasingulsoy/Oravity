using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Global referans banka tablosu.
/// PosTerminal ve BankAccount bu tabloya FK ile bağlanır.
/// Platform-geneli (tenant filtresi uygulanmaz).
/// </summary>
public class Bank : BaseEntity
{
    public string Name { get; private set; } = string.Empty;       // "Türkiye İş Bankası A.Ş."
    public string ShortName { get; private set; } = string.Empty;  // "İşbank"
    public string? BicCode { get; private set; }                    // "ISBKTRIS" (SWIFT)
    public bool IsActive { get; private set; } = true;

    private Bank() { }

    public static Bank Create(string name, string shortName, string? bicCode = null) =>
        new() { Name = name, ShortName = shortName, BicCode = bicCode };

    public void Update(string name, string shortName, string? bicCode)
    {
        Name      = name;
        ShortName = shortName;
        BicCode   = bicCode;
        MarkUpdated();
    }

    public void Deactivate() { IsActive = false; MarkUpdated(); }
    public void Activate()   { IsActive = true;  MarkUpdated(); }
}
