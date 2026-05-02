using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Şubeye ait banka hesabı.
/// Havale/EFT ödemelerinde hangi bankaya yatırıldığı takip edilir.
/// "Toplam Bankaya Yatan" raporunda kullanılır.
/// </summary>
public class BankAccount : AuditableEntity
{
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public long? BankId { get; private set; }
    public Bank? Bank { get; private set; }

    public string AccountName { get; private set; } = string.Empty; // "TL Vadesiz"
    public string? Iban { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public bool IsActive { get; private set; } = true;

    private BankAccount() { }

    public static BankAccount Create(long branchId, long? bankId, string accountName,
        string? iban = null, string currency = "TRY") =>
        new() { BranchId = branchId, BankId = bankId, AccountName = accountName,
                Iban = iban, Currency = currency };

    public void Update(long? bankId, string accountName, string? iban, string currency)
    {
        BankId      = bankId;
        AccountName = accountName;
        Iban        = iban;
        Currency    = currency;
        MarkUpdated();
    }

    public void Deactivate() { IsActive = false; MarkUpdated(); }
    public void Activate()   { IsActive = true;  MarkUpdated(); }
}
