using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public class Branch : BaseEntity
{
    public string Name { get; private set; } = default!;
    public long CompanyId { get; private set; }

    /// <summary>
    /// Null = şirketin verticalini kullan. Karma klinik senaryosu için override.
    /// </summary>
    public long? VerticalId { get; private set; }

    public string DefaultLanguageCode { get; private set; } = "tr";
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Şubeye özel cari fiyat çarpanı (varsayılan 1.0).
    /// Formüllerde MULTI değişkeni olarak kullanılır.
    /// Örnek: Bodrum = 1.10 → CARI * MULTI = CARI * 1.10
    /// </summary>
    public decimal PricingMultiplier { get; private set; } = 1.0m;

    public Company Company { get; private set; } = default!;
    public Vertical? Vertical { get; private set; }
    public ICollection<UserRoleAssignment> UserRoleAssignments { get; private set; } = [];

    private Branch() { }

    public static Branch Create(
        string name,
        long companyId,
        long? verticalId = null,
        string defaultLanguageCode = "tr")
    {
        return new Branch
        {
            Name = name,
            CompanyId = companyId,
            VerticalId = verticalId,
            DefaultLanguageCode = defaultLanguageCode,
            IsActive = true
        };
    }

    public void SetVerticalOverride(long? verticalId) => VerticalId = verticalId;
    public void SetActive(bool value) => IsActive = value;
    public void SetLanguage(string code) => DefaultLanguageCode = code;
    public void SetPricingMultiplier(decimal multiplier) => PricingMultiplier = multiplier <= 0 ? 1.0m : multiplier;
}
