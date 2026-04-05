using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Şirkete ait tedavi kataloğu öğesi.
/// TreatmentPlan'daki TreatmentPlanItem'ların referans aldığı kayıt.
/// </summary>
public class Treatment : AuditableEntity
{
    public long CompanyId { get; private set; }
    public Company Company { get; private set; } = default!;

    /// <summary>Benzersiz tedavi kodu (büyük harf, boşluksuz). Örn: "D001"</summary>
    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public long? CategoryId { get; private set; }
    public TreatmentCategory? Category { get; private set; }

    /// <summary>JSONB string — etiketler dizisi. Örn: '["ortopantomografi","röntgen"]'</summary>
    public string? Tags { get; private set; }

    public decimal KdvRate { get; private set; }
    public bool RequiresSurfaceSelection { get; private set; }
    public bool RequiresLaboratory { get; private set; }

    /// <summary>İzin verilen kapsam kodları. PostgreSQL integer[] kolonu.</summary>
    public int[] AllowedScopes { get; private set; } = [];

    public bool IsActive { get; private set; } = true;

    private Treatment() { }

    public static Treatment Create(
        long companyId,
        string code,
        string name,
        long? categoryId,
        decimal kdvRate,
        bool requiresSurfaceSelection,
        bool requiresLaboratory,
        int[]? allowedScopes,
        string? tags)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Tedavi kodu boş olamaz.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tedavi adı boş olamaz.", nameof(name));

        return new Treatment
        {
            CompanyId                = companyId,
            Code                     = code.Trim().ToUpperInvariant(),
            Name                     = name.Trim(),
            CategoryId               = categoryId,
            KdvRate                  = kdvRate,
            RequiresSurfaceSelection = requiresSurfaceSelection,
            RequiresLaboratory       = requiresLaboratory,
            AllowedScopes            = allowedScopes ?? [],
            Tags                     = tags,
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
        string? tags)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Tedavi kodu boş olamaz.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tedavi adı boş olamaz.", nameof(name));

        Code                     = code.Trim().ToUpperInvariant();
        Name                     = name.Trim();
        CategoryId               = categoryId;
        KdvRate                  = kdvRate;
        RequiresSurfaceSelection = requiresSurfaceSelection;
        RequiresLaboratory       = requiresLaboratory;
        AllowedScopes            = allowedScopes ?? [];
        Tags                     = tags;
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }
}
