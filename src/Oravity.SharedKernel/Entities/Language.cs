using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public class Language : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string NativeName { get; private set; } = default!;
    public string Direction { get; private set; } = "ltr";
    public string? FlagEmoji { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsDefault { get; private set; }
    public int SortOrder { get; private set; }

    private Language() { }

    public static Language Create(
        string code,
        string name,
        string nativeName,
        string direction = "ltr",
        string? flagEmoji = null,
        bool isDefault = false,
        int sortOrder = 0)
    {
        return new Language
        {
            Code = code.ToLowerInvariant(),
            Name = name,
            NativeName = nativeName,
            Direction = direction,
            FlagEmoji = flagEmoji,
            IsDefault = isDefault,
            SortOrder = sortOrder
        };
    }

    public void SetDefault(bool value) => IsDefault = value;
    public void SetActive(bool value) => IsActive = value;
}
