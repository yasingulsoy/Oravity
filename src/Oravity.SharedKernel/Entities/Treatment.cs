using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Şirkete ait tedavi kataloğu öğesi.
/// TreatmentPlan'daki TreatmentPlanItem'ların referans aldığı kayıt.
/// </summary>
public class Treatment : AuditableEntity
{
    /// <summary>null = global şablon, değer varsa şirkete özel.</summary>
    public long? CompanyId { get; private set; }
    public Company? Company { get; private set; }

    /// <summary>TDB sınıflandırma kodu. Örn: "1-1", "2-4"</summary>
    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public long? CategoryId { get; private set; }
    public TreatmentCategory? Category { get; private set; }

    /// <summary>SUT/MBYS işlem kodu. Örn: "402150"</summary>
    public string? SutCode { get; private set; }

    /// <summary>JSONB string — etiketler dizisi. Örn: '["ortopantomografi","röntgen"]'</summary>
    public string? Tags { get; private set; }

    public decimal KdvRate { get; private set; }
    public bool RequiresSurfaceSelection { get; private set; }
    public bool RequiresLaboratory { get; private set; }

    /// <summary>Lab işlemi öneri kategorisi (örn. "Zirkonyum", "Porselen", "Protez").</summary>
    public string? LabDefaultCategory { get; private set; }

    /// <summary>İzin verilen kapsam kodları. PostgreSQL integer[] kolonu.</summary>
    public int[] AllowedScopes { get; private set; } = [];

    public bool IsActive { get; private set; } = true;

    private Treatment() { }

    public static Treatment Create(
        long? companyId,
        string code,
        string name,
        long? categoryId,
        decimal kdvRate,
        bool requiresSurfaceSelection,
        bool requiresLaboratory,
        int[]? allowedScopes,
        string? tags,
        string? sutCode = null,
        string? labDefaultCategory = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Tedavi kodu boş olamaz.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tedavi adı boş olamaz.", nameof(name));

        return new Treatment
        {
            CompanyId                = companyId,
            Code                     = code.Trim(),
            Name                     = name.Trim(),
            CategoryId               = categoryId,
            KdvRate                  = kdvRate,
            RequiresSurfaceSelection = requiresSurfaceSelection,
            RequiresLaboratory       = requiresLaboratory,
            AllowedScopes            = allowedScopes ?? [],
            Tags                     = tags,
            SutCode                  = sutCode?.Trim(),
            LabDefaultCategory       = string.IsNullOrWhiteSpace(labDefaultCategory) ? null : labDefaultCategory.Trim(),
            IsActive                 = true
        };
    }

    public void Update(
        string code,
        string name,
        long? categoryId,
        decimal kdvRate,
        bool requiresSurfaceSelection,
        bool requiresLaboratory,
        int[]? allowedScopes,
        string? tags,
        string? sutCode = null,
        string? labDefaultCategory = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Tedavi kodu boş olamaz.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tedavi adı boş olamaz.", nameof(name));

        Code                     = code.Trim();
        Name                     = name.Trim();
        CategoryId               = categoryId;
        KdvRate                  = kdvRate;
        RequiresSurfaceSelection = requiresSurfaceSelection;
        RequiresLaboratory       = requiresLaboratory;
        AllowedScopes            = allowedScopes ?? [];
        Tags                     = tags;
        SutCode                  = sutCode?.Trim();
        LabDefaultCategory       = string.IsNullOrWhiteSpace(labDefaultCategory) ? null : labDefaultCategory.Trim();
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }
}
