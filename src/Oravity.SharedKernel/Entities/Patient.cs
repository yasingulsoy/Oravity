using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hasta kaydı. Tenant izolasyonu branch_id üzerinden sağlanır.
/// TC Kimlik No uygulama katmanında AES-256 ile şifreli, aramalarda SHA-256 hash kullanılır.
/// </summary>
public class Patient : AuditableEntity
{
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;

    public DateOnly? BirthDate { get; private set; }

    /// <summary>male / female / other</summary>
    public string? Gender { get; private set; }

    /// <summary>AES-256 şifreli TC Kimlik No</summary>
    public string? TcNumberEncrypted { get; private set; }

    /// <summary>SHA-256 hash — aramada kullanılır</summary>
    public string? TcNumberHash { get; private set; }

    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }

    /// <summary>A+, A-, B+, B-, AB+, AB-, 0+, 0-</summary>
    public string? BloodType { get; private set; }

    public string PreferredLanguageCode { get; private set; } = "tr";
    public bool IsActive { get; private set; } = true;

    private Patient() { }

    public static Patient Create(
        long branchId,
        string firstName,
        string lastName,
        string? phone,
        string? email,
        DateOnly? birthDate = null,
        string? gender = null,
        string? tcNumberEncrypted = null,
        string? tcNumberHash = null,
        string? address = null,
        string? bloodType = null,
        string? preferredLanguageCode = null)
    {
        return new Patient
        {
            BranchId = branchId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Phone = phone?.Trim(),
            Email = email?.ToLowerInvariant().Trim(),
            BirthDate = birthDate,
            Gender = gender,
            TcNumberEncrypted = tcNumberEncrypted,
            TcNumberHash = tcNumberHash,
            Address = address,
            BloodType = bloodType,
            PreferredLanguageCode = preferredLanguageCode ?? "tr",
            IsActive = true
        };
    }

    public void Update(
        string firstName,
        string lastName,
        string? phone,
        string? email,
        DateOnly? birthDate,
        string? gender,
        string? address,
        string? bloodType,
        string? preferredLanguageCode)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone?.Trim();
        Email = email?.ToLowerInvariant().Trim();
        BirthDate = birthDate;
        Gender = gender;
        Address = address;
        BloodType = bloodType;
        if (preferredLanguageCode is not null)
            PreferredLanguageCode = preferredLanguageCode;
        MarkUpdated();
    }

    public void UpdateTcNumber(string? encrypted, string? hash)
    {
        TcNumberEncrypted = encrypted;
        TcNumberHash = hash;
        MarkUpdated();
    }

    public void SetActive(bool value)
    {
        IsActive = value;
        MarkUpdated();
    }
}
