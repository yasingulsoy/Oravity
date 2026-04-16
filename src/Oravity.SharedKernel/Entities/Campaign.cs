using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Fiyatlandırma kampanyası.
/// Kampanya kodu, PricingRule includeFilters'daki campaignCodes ile eşleşir.
/// </summary>
public class Campaign : AuditableEntity
{
    public long CompanyId { get; private set; }

    /// <summary>Benzersiz kampanya kodu — kurallarla eşleşme için (ör: YAZ2026).</summary>
    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    public DateTime ValidFrom { get; private set; }
    public DateTime ValidUntil { get; private set; }

    public bool IsActive { get; private set; } = true;

    /// <summary>Otomatik bağlanan fiyat kuralının PublicId'si (opsiyonel).</summary>
    public Guid? LinkedRulePublicId { get; private set; }

    public long? CreatedBy { get; private set; }

    private Campaign() { }

    public static Campaign Create(
        long companyId,
        string code,
        string name,
        string? description,
        DateTime validFrom,
        DateTime validUntil,
        Guid? linkedRulePublicId,
        long? createdBy)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Kampanya kodu boş olamaz.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Kampanya adı boş olamaz.", nameof(name));
        if (validUntil <= validFrom)
            throw new ArgumentException("Bitiş tarihi başlangıçtan sonra olmalı.", nameof(validUntil));

        return new Campaign
        {
            CompanyId          = companyId,
            Code               = code.Trim().ToUpperInvariant(),
            Name               = name.Trim(),
            Description        = description,
            ValidFrom          = validFrom,
            ValidUntil         = validUntil,
            IsActive           = true,
            LinkedRulePublicId = linkedRulePublicId,
            CreatedBy          = createdBy,
        };
    }

    public void Update(
        string name,
        string? description,
        DateTime validFrom,
        DateTime validUntil,
        Guid? linkedRulePublicId)
    {
        Name               = name.Trim();
        Description        = description;
        ValidFrom          = validFrom;
        ValidUntil         = validUntil;
        LinkedRulePublicId = linkedRulePublicId;
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }
}
