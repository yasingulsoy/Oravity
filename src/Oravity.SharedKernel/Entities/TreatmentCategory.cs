using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Tedavi kategorisi — hiyerarşik (ParentId ile ağaç yapısı desteklenir).
/// </summary>
public class TreatmentCategory : AuditableEntity
{
    /// <summary>null = global şablon, değer varsa şirkete özel.</summary>
    public long? CompanyId { get; private set; }
    public Company? Company { get; private set; }

    public string Name { get; private set; } = default!;

    public long? ParentId { get; private set; }
    public TreatmentCategory? Parent { get; private set; }

    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private TreatmentCategory() { }

    public static TreatmentCategory Create(
        long? companyId,
        string name,
        long? parentId,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Kategori adı boş olamaz.", nameof(name));

        return new TreatmentCategory
        {
            CompanyId = companyId,
            Name      = name.Trim(),
            ParentId  = parentId,
            SortOrder = sortOrder,
            IsActive  = true
        };
    }

    public void Update(string name, long? parentId, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Kategori adı boş olamaz.", nameof(name));

        Name      = name.Trim();
        ParentId  = parentId;
        SortOrder = sortOrder;
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }
}
