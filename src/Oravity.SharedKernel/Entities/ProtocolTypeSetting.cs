namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Protokol tipi yapılandırması — protocol_types tablosundan yönetilir.
/// Backend ProtocolType enum'u ile ID üzerinden eşleşir (1-5 sabit).
/// </summary>
public class ProtocolTypeSetting
{
    public int    Id          { get; private set; }
    public string Name        { get; private set; } = default!;
    public string Code        { get; private set; } = default!;
    public string? Description { get; private set; }
    public string Color       { get; private set; } = "#6366f1";
    public int    SortOrder   { get; private set; }
    public bool   IsActive    { get; private set; } = true;

    private ProtocolTypeSetting() { }

    public static ProtocolTypeSetting Create(int id, string name, string code, string color, int sortOrder, string? description = null) =>
        new()
        {
            Id          = id,
            Name        = name,
            Code        = code.ToUpperInvariant(),
            Description = description,
            Color       = color,
            SortOrder   = sortOrder,
            IsActive    = true,
        };

    public void Update(string name, string? description, string color, int sortOrder, bool isActive)
    {
        Name        = name;
        Description = description;
        Color       = color;
        SortOrder   = sortOrder;
        IsActive    = isActive;
    }
}
