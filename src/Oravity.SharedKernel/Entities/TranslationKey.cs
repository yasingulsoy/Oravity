namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Çeviri anahtar kataloğu (SPEC §ÇOKLU DİL §2).
/// key örnekleri: "common.save", "patient.title", "appointment.status.confirmed"
/// Noktalı hiyerarşi — kategori, bağlama ve durum kodunu içerir.
/// </summary>
public class TranslationKey
{
    public long Id { get; private set; }

    /// <summary>Benzersiz noktalı anahtar, ör. "common.save"</summary>
    public string Key { get; private set; } = default!;

    /// <summary>
    /// Kategori: common | patient | appointment | payment |
    ///           treatment | notification | report | invoice
    /// </summary>
    public string Category { get; private set; } = default!;

    /// <summary>Geliştirici notu — anahtarın ne için kullanıldığını açıklar.</summary>
    public string? Description { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public ICollection<Translation> Translations { get; private set; } = [];

    private TranslationKey() { }

    public static TranslationKey Create(string key, string category, string? description = null)
    {
        return new TranslationKey
        {
            Key         = key.ToLowerInvariant().Trim(),
            Category    = category.ToLowerInvariant().Trim(),
            Description = description,
            CreatedAt   = DateTime.UtcNow
        };
    }
}
