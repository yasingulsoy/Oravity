using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oravity.Core.Modules.Core.Localization.Application.Services;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Core.Localization.Application.Commands;

/// <summary>
/// Tek bir anahtar+dil kombinasyonu için çeviri ekler veya günceller.
/// Key mevcut değilse TranslationKey de oluşturulur.
/// Cache invalidation yapılır.
/// </summary>
public record UpsertTranslationCommand(
    string  Key,
    string  Category,
    string? Description,
    string  LangCode,
    string  Value,
    bool    IsReviewed = false) : IRequest<Unit>;

public class UpsertTranslationCommandHandler : IRequestHandler<UpsertTranslationCommand, Unit>
{
    private readonly AppDbContext          _db;
    private readonly TranslationService    _translationService;
    private readonly ILogger<UpsertTranslationCommandHandler> _logger;

    public UpsertTranslationCommandHandler(
        AppDbContext       db,
        TranslationService translationService,
        ILogger<UpsertTranslationCommandHandler> logger)
    {
        _db                 = db;
        _translationService = translationService;
        _logger             = logger;
    }

    public async Task<Unit> Handle(UpsertTranslationCommand request, CancellationToken ct)
    {
        // 1. Dil kontrolü
        var lang = await _db.Languages
            .FirstOrDefaultAsync(l => l.Code == request.LangCode && l.IsActive, ct)
            ?? throw new NotFoundException($"Dil bulunamadı veya aktif değil: {request.LangCode}");

        // 2. TranslationKey — varsa al, yoksa oluştur
        var tKey = await _db.TranslationKeys
            .FirstOrDefaultAsync(k => k.Key == request.Key.ToLowerInvariant().Trim(), ct);

        if (tKey is null)
        {
            tKey = TranslationKey.Create(request.Key, request.Category, request.Description);
            _db.TranslationKeys.Add(tKey);
            await _db.SaveChangesAsync(ct); // ID almak için flush
        }

        // 3. Translation — varsa güncelle, yoksa ekle
        var translation = await _db.Translations
            .FirstOrDefaultAsync(t => t.KeyId == tKey.Id && t.LanguageId == lang.Id, ct);

        if (translation is null)
        {
            translation = Translation.Create(tKey.Id, lang.Id, request.Value, request.IsReviewed);
            _db.Translations.Add(translation);
        }
        else
        {
            translation.Update(request.Value, request.IsReviewed);
        }

        await _db.SaveChangesAsync(ct);

        // 4. Cache temizle
        _translationService.InvalidateCache(request.LangCode, request.Key.ToLowerInvariant().Trim());

        _logger.LogInformation(
            "Çeviri upsert: key={Key} lang={Lang} isReviewed={IsReviewed}",
            request.Key, request.LangCode, request.IsReviewed);

        return Unit.Value;
    }
}
