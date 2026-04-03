namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Dil bazlı çeviri değeri (SPEC §ÇOKLU DİL §2).
/// UNIQUE(key_id, language_id) — her anahtar+dil kombinasyonu bir kez.
/// is_reviewed: profesyonel çevirmen onayı (false = makine/taslak).
/// </summary>
public class Translation
{
    public long Id { get; private set; }

    public long KeyId { get; private set; }
    public TranslationKey TranslationKey { get; private set; } = default!;

    public long LanguageId { get; private set; }
    public Language Language { get; private set; } = default!;

    /// <summary>Çeviri metni, örn. "Kaydet" veya "Save"</summary>
    public string Value { get; private set; } = default!;

    /// <summary>true = insan / uzman tarafından onaylandı.</summary>
    public bool IsReviewed { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private Translation() { }

    public static Translation Create(long keyId, long languageId, string value, bool isReviewed = false)
    {
        return new Translation
        {
            KeyId      = keyId,
            LanguageId = languageId,
            Value      = value,
            IsReviewed = isReviewed,
            UpdatedAt  = DateTime.UtcNow
        };
    }

    public void Update(string value, bool isReviewed = false)
    {
        Value      = value;
        IsReviewed = isReviewed;
        UpdatedAt  = DateTime.UtcNow;
    }
}
