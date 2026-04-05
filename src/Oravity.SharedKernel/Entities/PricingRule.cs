using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Fiyatlandırma kuralı.
/// Birden fazla kural Priority sırasına göre uygulanır; StopProcessing=true sonrasındakiler atlanır.
/// </summary>
public class PricingRule : AuditableEntity
{
    public long CompanyId { get; private set; }
    public long? BranchId { get; private set; }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    /// <summary>Kural tipi: "percentage" | "fixed" | "formula"</summary>
    public string RuleType { get; private set; } = default!;

    /// <summary>Düşük sayı = yüksek öncelik (1 en yüksek).</summary>
    public int Priority { get; private set; }

    /// <summary>JSONB — uygulanacak tedavi/kategori filtreleri.</summary>
    public string? IncludeFilters { get; private set; }

    /// <summary>JSONB — hariç tutulacak tedavi/kategori filtreleri.</summary>
    public string? ExcludeFilters { get; private set; }

    /// <summary>FormulaEngine.Evaluate() ile hesaplanan formül. RuleType="formula" için.</summary>
    public string? Formula { get; private set; }

    public string OutputCurrency { get; private set; } = "TRY";

    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidUntil { get; private set; }

    public bool IsActive { get; private set; } = true;

    /// <summary>true ise sonraki kurallar işlenmez.</summary>
    public bool StopProcessing { get; private set; }

    public long? CreatedBy { get; private set; }

    private PricingRule() { }

    public static PricingRule Create(
        long companyId,
        long? branchId,
        string name,
        string? description,
        string ruleType,
        int priority,
        string? includeFilters,
        string? excludeFilters,
        string? formula,
        string outputCurrency,
        DateTime? validFrom,
        DateTime? validUntil,
        bool stopProcessing,
        long? createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Kural adı boş olamaz.", nameof(name));
        if (string.IsNullOrWhiteSpace(ruleType))
            throw new ArgumentException("Kural tipi boş olamaz.", nameof(ruleType));

        return new PricingRule
        {
            CompanyId       = companyId,
            BranchId        = branchId,
            Name            = name.Trim(),
            Description     = description,
            RuleType        = ruleType,
            Priority        = priority,
            IncludeFilters  = includeFilters,
            ExcludeFilters  = excludeFilters,
            Formula         = formula,
            OutputCurrency  = string.IsNullOrWhiteSpace(outputCurrency) ? "TRY" : outputCurrency,
            ValidFrom       = validFrom,
            ValidUntil      = validUntil,
            IsActive        = true,
            StopProcessing  = stopProcessing,
            CreatedBy       = createdBy
        };
    }

    public void Update(
        string name,
        string? description,
        string ruleType,
        int priority,
        string? includeFilters,
        string? excludeFilters,
        string? formula,
        string outputCurrency,
        DateTime? validFrom,
        DateTime? validUntil,
        bool stopProcessing)
    {
        Name           = name.Trim();
        Description    = description;
        RuleType       = ruleType;
        Priority       = priority;
        IncludeFilters = includeFilters;
        ExcludeFilters = excludeFilters;
        Formula        = formula;
        OutputCurrency = string.IsNullOrWhiteSpace(outputCurrency) ? "TRY" : outputCurrency;
        ValidFrom      = validFrom;
        ValidUntil     = validUntil;
        StopProcessing = stopProcessing;
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }
}
