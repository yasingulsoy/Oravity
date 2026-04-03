using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Localization.Application;
using Oravity.Infrastructure.Database;

namespace Oravity.Core.Modules.Core.Localization.Application.Queries;

/// <summary>
/// Dil koduna göre tüm çevirileri döndürür.
/// category ve missingOnly filtreleri opsiyoneldir.
/// missingOnly=true → value IS NULL olan (henüz çevrilmemiş) anahtarları listeler.
/// </summary>
public record GetTranslationsQuery(
    string  LangCode,
    string? Category    = null,
    bool    MissingOnly = false,
    int     Page        = 1,
    int     PageSize    = 200) : IRequest<TranslationsPagedResult>;

public class GetTranslationsQueryHandler : IRequestHandler<GetTranslationsQuery, TranslationsPagedResult>
{
    private readonly AppDbContext _db;

    public GetTranslationsQueryHandler(AppDbContext db) => _db = db;

    public async Task<TranslationsPagedResult> Handle(GetTranslationsQuery request, CancellationToken ct)
    {
        var langCode = request.LangCode.ToLowerInvariant().Trim();

        if (request.MissingOnly)
        {
            // Eksik çeviriler: key var ama bu dil için translation yok
            var keysWithoutTranslation = _db.TranslationKeys
                .Where(k => !_db.Translations.Any(t =>
                    t.KeyId == k.Id &&
                    t.Language.Code == langCode));

            if (!string.IsNullOrWhiteSpace(request.Category))
                keysWithoutTranslation = keysWithoutTranslation.Where(k => k.Category == request.Category);

            var total = await keysWithoutTranslation.CountAsync(ct);
            var items = await keysWithoutTranslation
                .OrderBy(k => k.Category).ThenBy(k => k.Key)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(k => new TranslationResponse(
                    k.Id,
                    k.Key,
                    k.Category,
                    langCode,
                    string.Empty,
                    false,
                    default))
                .ToListAsync(ct);

            return new TranslationsPagedResult(items, total, request.Page, request.PageSize);
        }
        else
        {
            var query = _db.Translations
                .Where(t => t.Language.Code == langCode)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Category))
                query = query.Where(t => t.TranslationKey.Category == request.Category);

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderBy(t => t.TranslationKey.Category).ThenBy(t => t.TranslationKey.Key)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new TranslationResponse(
                    t.Id,
                    t.TranslationKey.Key,
                    t.TranslationKey.Category,
                    t.Language.Code,
                    t.Value,
                    t.IsReviewed,
                    t.UpdatedAt))
                .ToListAsync(ct);

            return new TranslationsPagedResult(items, total, request.Page, request.PageSize);
        }
    }
}
