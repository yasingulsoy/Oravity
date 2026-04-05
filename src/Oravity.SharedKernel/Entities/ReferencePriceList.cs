using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Referans fiyat listesi (SUT, SGK, sigorta şirketi vb.).
/// </summary>
public class ReferencePriceList : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    /// <summary>Kaynak tipi: "SUT" | "SGK" | "insurance" | "private"</summary>
    public string SourceType { get; private set; } = default!;

    public int Year { get; private set; }
    public bool IsActive { get; private set; } = true;

    public ICollection<ReferencePriceItem> Items { get; private set; } = [];

    private ReferencePriceList() { }

    public static ReferencePriceList Create(string code, string name, string sourceType, int year)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Kod boş olamaz.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Ad boş olamaz.", nameof(name));

        return new ReferencePriceList
        {
            Code       = code.Trim().ToUpperInvariant(),
            Name       = name.Trim(),
            SourceType = sourceType.Trim(),
            Year       = year,
            IsActive   = true
        };
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }
}
