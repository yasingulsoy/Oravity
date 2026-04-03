using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Core.Localization.Application;
using Oravity.Core.Modules.Core.Localization.Application.Commands;
using Oravity.Core.Modules.Core.Localization.Application.Queries;
using Oravity.Core.Modules.Core.Localization.Application.Services;

namespace Oravity.Core.Controllers;

[ApiController]
[Route("api/localization")]
[Tags("Lokalizasyon")]
public class LocalizationController : ControllerBase
{
    private readonly IMediator         _mediator;
    private readonly TranslationService _translationService;

    public LocalizationController(IMediator mediator, TranslationService translationService)
    {
        _mediator           = mediator;
        _translationService = translationService;
    }

    /// <summary>
    /// Belirtilen dil koduna ait tüm çevirileri flat dictionary olarak döndürür.
    /// Frontend i18n bundle cache için optimize edilmiştir.
    /// Cache-Control: public, max-age=3600
    /// </summary>
    [HttpGet("{langCode}/all")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Dictionary<string, string>), 200)]
    public async Task<IActionResult> GetAll(string langCode, CancellationToken ct)
    {
        var dict = await _translationService.GetAll(langCode, ct);
        Response.Headers["Cache-Control"] = "public, max-age=3600";
        return Ok(dict);
    }

    /// <summary>
    /// Sayfalı çeviri listesi. category ve missingOnly filtreleri opsiyoneldir.
    /// </summary>
    [HttpGet("{langCode}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TranslationsPagedResult), 200)]
    public async Task<IActionResult> GetTranslations(
        string langCode,
        [FromQuery] string? category    = null,
        [FromQuery] bool    missingOnly = false,
        [FromQuery] int     page        = 1,
        [FromQuery] int     pageSize    = 200,
        CancellationToken ct = default)
    {
        Response.Headers["Cache-Control"] = "public, max-age=3600";
        var result = await _mediator.Send(
            new GetTranslationsQuery(langCode, category, missingOnly, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Tek bir çeviriyi ekler veya günceller (Upsert).
    /// Cache otomatik temizlenir.
    /// </summary>
    [HttpPut("{langCode}/{*key}")]
    [Authorize]
    [RequirePermission("translations:manage")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpsertTranslation(
        string langCode,
        string key,
        [FromBody] UpsertTranslationBody body,
        CancellationToken ct)
    {
        await _mediator.Send(new UpsertTranslationCommand(
            Key:        key,
            Category:   body.Category,
            Description:body.Description,
            LangCode:   langCode,
            Value:      body.Value,
            IsReviewed: body.IsReviewed), ct);

        return NoContent();
    }

    public record UpsertTranslationBody(
        string  Category,
        string  Value,
        string? Description  = null,
        bool    IsReviewed   = false);
}
