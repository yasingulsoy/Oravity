using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Şirkete ait laboratuvar tanımı (protez, zirkonyum, implant üstü kuron vb. iş gönderilen dış laboratuvar).
/// </summary>
public class Laboratory : AuditableEntity
{
    public long CompanyId { get; private set; }

    public string Name { get; private set; } = default!;
    public string? Code { get; private set; }

    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Website { get; private set; }

    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public string? Address { get; private set; }

    public string? ContactPerson { get; private set; }
    public string? ContactPhone { get; private set; }

    /// <summary>JSONB — çalışma günleri: ["Monday","Tuesday",...].</summary>
    public string? WorkingDays { get; private set; }
    public string? WorkingHours { get; private set; }

    public string? PaymentTerms { get; private set; }
    public int PaymentDays { get; private set; } = 30;

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; } = true;

    private Laboratory() { }

    public static Laboratory Create(
        long companyId,
        string name,
        string? code,
        string? phone,
        string? email,
        string? website,
        string? country,
        string? city,
        string? district,
        string? address,
        string? contactPerson,
        string? contactPhone,
        string? workingDays,
        string? workingHours,
        string? paymentTerms,
        int paymentDays,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Laboratuvar adı boş olamaz.", nameof(name));

        return new Laboratory
        {
            CompanyId     = companyId,
            Name          = name.Trim(),
            Code          = string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant(),
            Phone         = phone?.Trim(),
            Email         = email?.Trim(),
            Website       = website?.Trim(),
            Country       = string.IsNullOrWhiteSpace(country) ? "Türkiye" : country.Trim(),
            City          = city?.Trim(),
            District      = district?.Trim(),
            Address       = address?.Trim(),
            ContactPerson = contactPerson?.Trim(),
            ContactPhone  = contactPhone?.Trim(),
            WorkingDays   = workingDays,
            WorkingHours  = workingHours?.Trim(),
            PaymentTerms  = paymentTerms,
            PaymentDays   = paymentDays <= 0 ? 30 : paymentDays,
            Notes         = notes,
            IsActive      = true,
        };
    }

    public void Update(
        string name,
        string? code,
        string? phone,
        string? email,
        string? website,
        string? country,
        string? city,
        string? district,
        string? address,
        string? contactPerson,
        string? contactPhone,
        string? workingDays,
        string? workingHours,
        string? paymentTerms,
        int paymentDays,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Laboratuvar adı boş olamaz.", nameof(name));

        Name          = name.Trim();
        Code          = string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
        Phone         = phone?.Trim();
        Email         = email?.Trim();
        Website       = website?.Trim();
        Country       = country?.Trim();
        City          = city?.Trim();
        District      = district?.Trim();
        Address       = address?.Trim();
        ContactPerson = contactPerson?.Trim();
        ContactPhone  = contactPhone?.Trim();
        WorkingDays   = workingDays;
        WorkingHours  = workingHours?.Trim();
        PaymentTerms  = paymentTerms;
        PaymentDays   = paymentDays <= 0 ? 30 : paymentDays;
        Notes         = notes;
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }
}
