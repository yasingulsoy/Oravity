namespace Oravity.Core.Modules.Core.Localization.Application;

// ─── Response ─────────────────────────────────────────────────────────────
public record TranslationKeyResponse(
    long   Id,
    string Key,
    string Category,
    string? Description,
    DateTime CreatedAt);

public record TranslationResponse(
    long   Id,
    string Key,
    string Category,
    string LangCode,
    string Value,
    bool   IsReviewed,
    DateTime UpdatedAt);

public record TranslationsPagedResult(
    IReadOnlyList<TranslationResponse> Items,
    int Total,
    int Page,
    int PageSize);
