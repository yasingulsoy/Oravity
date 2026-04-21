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

    /// <summary>
    /// Tedavinin klinik maliyet tahmini (hakediş hesabında kesinti olarak kullanılır).
    /// Null ya da 0 ise tedavi maliyeti kesintisi uygulanmaz.
    /// </summary>
    public decimal? CostPrice { get; private set; }

    public bool RequiresSurfaceSelection { get; private set; }
    public bool RequiresLaboratory { get; private set; }

    /// <summary>Lab işlemi öneri kategorisi (örn. "Zirkonyum", "Porselen", "Protez").</summary>
    public string? LabDefaultCategory { get; private set; }

    /// <summary>İzin verilen kapsam kodları. PostgreSQL integer[] kolonu.</summary>
    public int[] AllowedScopes { get; private set; } = [];

    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Diş şemasında bu tedavi seçildiğinde dişin üzerine bindirilecek SVG simge kodu.
    /// Örn: "root-canal", "crown", "implant", "filling-o". Null = simge gösterme.
    /// </summary>
    public string? ChartSymbolCode { get; private set; }

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
        string? labDefaultCategory = null,
        decimal? costPrice = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Tedavi kodu boş olamaz.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tedavi adı boş olamaz.", nameof(name));
        if (costPrice.HasValue && costPrice.Value < 0)
            throw new ArgumentException("Tedavi maliyeti negatif olamaz.", nameof(costPrice));

        return new Treatment
        {
            CompanyId                = companyId,
            Code                     = code.Trim(),
            Name                     = name.Trim(),
            CategoryId               = categoryId,
            KdvRate                  = kdvRate,
            CostPrice                = costPrice,
            RequiresSurfaceSelection = requiresSurfaceSelection,
            RequiresLaboratory       = requiresLaboratory,
            AllowedScopes            = allowedScopes ?? [],
            Tags                     = tags,
            SutCode                  = sutCode?.Trim(),
            LabDefaultCategory       = string.IsNullOrWhiteSpace(labDefaultCategory) ? null : labDefaultCategory.Trim(),
            IsActive                 = true
        };
    }

    public void SetChartSymbol(string? code)
    {
        ChartSymbolCode = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        MarkUpdated();
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
        string? labDefaultCategory = null,
        decimal? costPrice = null,
        string? chartSymbolCode = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Tedavi kodu boş olamaz.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tedavi adı boş olamaz.", nameof(name));
        if (costPrice.HasValue && costPrice.Value < 0)
            throw new ArgumentException("Tedavi maliyeti negatif olamaz.", nameof(costPrice));

        Code                     = code.Trim();
        Name                     = name.Trim();
        CategoryId               = categoryId;
        KdvRate                  = kdvRate;
        CostPrice                = costPrice;
        RequiresSurfaceSelection = requiresSurfaceSelection;
        RequiresLaboratory       = requiresLaboratory;
        AllowedScopes            = allowedScopes ?? [];
        Tags                     = tags;
        SutCode                  = sutCode?.Trim();
        LabDefaultCategory       = string.IsNullOrWhiteSpace(labDefaultCategory) ? null : labDefaultCategory.Trim();
        ChartSymbolCode          = string.IsNullOrWhiteSpace(chartSymbolCode) ? null : chartSymbolCode.Trim();
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }
}
