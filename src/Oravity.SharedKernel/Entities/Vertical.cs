using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public class Vertical : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    public bool HasBodyChart { get; private set; }
    public string? BodyChartType { get; private set; }

    /// <summary>
    /// Comma-separated default module codes, e.g. "CORE,FINANCE,APPOINTMENT,TREATMENT"
    /// Stored as PostgreSQL text array.
    /// </summary>
    public string[] DefaultModules { get; private set; } = [];

    public string ProviderLabel { get; private set; } = "Hekim";
    public string PatientLabel { get; private set; } = "Hasta";
    public string TreatmentLabel { get; private set; } = "Tedavi";

    public bool RequiresKts { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    public ICollection<Company> Companies { get; private set; } = [];
    public ICollection<Branch> Branches { get; private set; } = [];

    private Vertical() { }

    public static Vertical Create(
        string code,
        string name,
        bool hasBodyChart = false,
        string? bodyChartType = null,
        string[]? defaultModules = null,
        string providerLabel = "Hekim",
        string patientLabel = "Hasta",
        string treatmentLabel = "Tedavi",
        bool requiresKts = false,
        bool isActive = true,
        int sortOrder = 0)
    {
        return new Vertical
        {
            Code = code.ToUpperInvariant(),
            Name = name,
            HasBodyChart = hasBodyChart,
            BodyChartType = bodyChartType,
            DefaultModules = defaultModules ?? ["CORE", "APPOINTMENT"],
            ProviderLabel = providerLabel,
            PatientLabel = patientLabel,
            TreatmentLabel = treatmentLabel,
            RequiresKts = requiresKts,
            IsActive = isActive,
            SortOrder = sortOrder
        };
    }

    public void SetActive(bool value) => IsActive = value;
}
