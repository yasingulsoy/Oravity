using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum InstitutionPaymentModel
{
    /// <summary>İndirim: hasta indirimli fiyatı öder, kuruma fatura kesilmez.</summary>
    Discount  = 1,
    /// <summary>Provizyon: kurum tedavi başına sabit tutar öder, kalan hastadan alınır.</summary>
    Provision = 2,
}

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

    // E-Fatura & Tevkifat
    /// <summary>Kurum e-fatura mükellefi mi? Evet ise e-fatura, hayır ise kağıt fatura kesilir.</summary>
    public bool IsEInvoiceTaxpayer { get; private set; } = false;
    /// <summary>Bu kuruma kesilen faturalarda KDV tevkifatı uygulanır mı?</summary>
    public bool WithholdingApplies { get; private set; } = false;
    /// <summary>Tevkifat kodu. Örn: "616" (Diğer Hizmetler, KDVGUT-I/C-2.1.3.4.1)</summary>
    public string? WithholdingCode { get; private set; }
    /// <summary>Tevkifat oranı pay. Örn: 5 (5/10)</summary>
    public int WithholdingNumerator { get; private set; } = 5;
    /// <summary>Tevkifat oranı payda. Örn: 10 (5/10)</summary>
    public int WithholdingDenominator { get; private set; } = 10;

    // Ödeme Modeli
    /// <summary>Discount = indirim uygula, Provision = kurum katkı tutarı öder.</summary>
    public InstitutionPaymentModel PaymentModel { get; private set; } = InstitutionPaymentModel.Discount;

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
        InstitutionPaymentModel paymentModel = InstitutionPaymentModel.Discount,
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
        string? notes = null,
        bool isEInvoiceTaxpayer = false,
        bool withholdingApplies = false,
        string? withholdingCode = null,
        int withholdingNumerator = 5,
        int withholdingDenominator = 10)
        => new()
        {
            Name = name.Trim(),
            Code = code?.Trim().ToUpperInvariant(),
            Type = type,
            PaymentModel = paymentModel,
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
            IsEInvoiceTaxpayer = isEInvoiceTaxpayer,
            WithholdingApplies = withholdingApplies,
            WithholdingCode = withholdingCode?.Trim(),
            WithholdingNumerator = withholdingNumerator > 0 ? withholdingNumerator : 5,
            WithholdingDenominator = withholdingDenominator > 0 ? withholdingDenominator : 10,
        };

    public void Update(
        string name,
        string? code,
        string? type,
        bool isActive,
        InstitutionPaymentModel paymentModel = InstitutionPaymentModel.Discount,
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
        string? notes = null,
        bool isEInvoiceTaxpayer = false,
        bool withholdingApplies = false,
        string? withholdingCode = null,
        int withholdingNumerator = 5,
        int withholdingDenominator = 10)
    {
        Name = name.Trim();
        Code = code?.Trim().ToUpperInvariant();
        Type = type;
        PaymentModel = paymentModel;
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
        IsEInvoiceTaxpayer = isEInvoiceTaxpayer;
        WithholdingApplies = withholdingApplies;
        WithholdingCode = withholdingCode?.Trim();
        WithholdingNumerator = withholdingNumerator > 0 ? withholdingNumerator : 5;
        WithholdingDenominator = withholdingDenominator > 0 ? withholdingDenominator : 10;
        MarkUpdated();
    }
}
