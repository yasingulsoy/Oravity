using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Anlaşmalı kurum (sigorta şirketi, kurumsal müşteri, anlaşmalı hastane vb.)
/// CompanyId = null  → platform geneli
/// CompanyId = X    → şirkete özel
/// </summary>
public class Institution : BaseEntity
{
    public long? CompanyId { get; private set; }
    public Company? Company { get; private set; }

    // Temel
    public string Name { get; private set; } = default!;
    public string? Code { get; private set; }
    /// <summary>sigorta | kurumsal | kamu | uluslararası</summary>
    public string? Type { get; private set; }
    /// <summary>domestic | international — yurtiçi/yurtdışı pazarlama ayrımı</summary>
    public string? MarketSegment { get; private set; }

    // İletişim
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Website { get; private set; }

    // Adres
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public string? Address { get; private set; }

    // Yetkili Kişi
    public string? ContactPerson { get; private set; }
    public string? ContactPhone { get; private set; }

    // Mali / Fatura
    public string? TaxNumber { get; private set; }
    public string? TaxOffice { get; private set; }

    // Ödeme Koşulları
    public decimal? DiscountRate { get; private set; }
    public int PaymentDays { get; private set; } = 30;
    public string? PaymentTerms { get; private set; }

    // Genel
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Institution() { }

    public static Institution Create(
        string name,
        string? code = null,
        string? type = null,
        long? companyId = null,
        string? marketSegment = null,
        string? phone = null,
        string? email = null,
        string? website = null,
        string? country = null,
        string? city = null,
        string? district = null,
        string? address = null,
        string? contactPerson = null,
        string? contactPhone = null,
        string? taxNumber = null,
        string? taxOffice = null,
        decimal? discountRate = null,
        int paymentDays = 30,
        string? paymentTerms = null,
        string? notes = null)
        => new()
        {
            Name = name.Trim(),
            Code = code?.Trim().ToUpperInvariant(),
            Type = type,
            MarketSegment = marketSegment,
            CompanyId = companyId,
            Phone = phone?.Trim(),
            Email = email?.Trim().ToLowerInvariant(),
            Website = website?.Trim(),
            Country = country?.Trim(),
            City = city?.Trim(),
            District = district?.Trim(),
            Address = address?.Trim(),
            ContactPerson = contactPerson?.Trim(),
            ContactPhone = contactPhone?.Trim(),
            TaxNumber = taxNumber?.Trim(),
            TaxOffice = taxOffice?.Trim(),
            DiscountRate = discountRate,
            PaymentDays = paymentDays,
            PaymentTerms = paymentTerms?.Trim(),
            Notes = notes?.Trim(),
            IsActive = true,
        };

    public void Update(
        string name,
        string? code,
        string? type,
        bool isActive,
        string? marketSegment = null,
        string? phone = null,
        string? email = null,
        string? website = null,
        string? country = null,
        string? city = null,
        string? district = null,
        string? address = null,
        string? contactPerson = null,
        string? contactPhone = null,
        string? taxNumber = null,
        string? taxOffice = null,
        decimal? discountRate = null,
        int paymentDays = 30,
        string? paymentTerms = null,
        string? notes = null)
    {
        Name = name.Trim();
        Code = code?.Trim().ToUpperInvariant();
        Type = type;
        MarketSegment = marketSegment;
        IsActive = isActive;
        Phone = phone?.Trim();
        Email = email?.Trim().ToLowerInvariant();
        Website = website?.Trim();
        Country = country?.Trim();
        City = city?.Trim();
        District = district?.Trim();
        Address = address?.Trim();
        ContactPerson = contactPerson?.Trim();
        ContactPhone = contactPhone?.Trim();
        TaxNumber = taxNumber?.Trim();
        TaxOffice = taxOffice?.Trim();
        DiscountRate = discountRate;
        PaymentDays = paymentDays;
        PaymentTerms = paymentTerms?.Trim();
        Notes = notes?.Trim();
        MarkUpdated();
    }
}
