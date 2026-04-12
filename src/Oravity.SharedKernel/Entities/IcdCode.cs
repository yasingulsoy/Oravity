using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// ICD-10 tanı kodu kataloğu.
/// Type: 1 = Diş Hekimliği (K00-K14, S02, M27, Z01 vb.)
/// Platform geneli seed verisi; şube/şirket bağımsız.
/// </summary>
public class IcdCode : BaseEntity
{
    /// <summary>Örn. "K02.1", "K05.3"</summary>
    public string Code { get; private set; } = default!;

    /// <summary>Türkçe açıklama</summary>
    public string Description { get; private set; } = default!;

    /// <summary>Üst kategori kodu — örn. "K02", "K05"</summary>
    public string Category { get; private set; } = default!;

    /// <summary>1 = Diş Hekimliği</summary>
    public int Type { get; private set; }

    public bool IsActive { get; private set; } = true;

    private IcdCode() { }

    public static IcdCode Create(string code, string description, string category, int type = 1)
        => new() { Code = code, Description = description, Category = category, Type = type };
}
