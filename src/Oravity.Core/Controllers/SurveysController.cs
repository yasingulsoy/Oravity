using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Survey.Application;
using Oravity.Core.Modules.Survey.Application.Commands;
using Oravity.Core.Modules.Survey.Application.Queries;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Controllers;

[ApiController]
[Route("api/surveys")]
[Authorize]
[Tags("Anket Yönetimi")]
public class SurveysController : ControllerBase
{
    private readonly IMediator _mediator;

    public SurveysController(IMediator mediator) => _mediator = mediator;

    /// <summary>Anket şablonlarını listeler.</summary>
    [HttpGet("templates")]
    [RequirePermission("survey:manage")]
    public async Task<IActionResult> GetTemplates(CancellationToken ct = default)
    {
        var query  = new GetSurveyTemplatesQuery();
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>Yeni anket şablonu oluşturur.</summary>
    [HttpPost("templates")]
    [RequirePermission("survey:manage")]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreateTemplateBody body,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CreateSurveyTemplateCommand(
            body.Name, body.TriggerType, body.TriggerDelayHours, body.Description), ct);
        return Created($"api/surveys/templates/{result.PublicId}", result);
    }

    /// <summary>Belirli bir hastaya anket gönderir.</summary>
    [HttpPost("send")]
    [RequirePermission("survey:manage")]
    public async Task<IActionResult> SendSurvey(
        [FromBody] SendSurveyBody body,
        CancellationToken ct = default)
    {
        var publicId = await _mediator.Send(new SendSurveyCommand(
            body.TemplateId, body.PatientId, body.BranchId,
            body.CompanyId, body.Channel, body.AppointmentId), ct);
        return Ok(new { PublicId = publicId });
    }

    /// <summary>Anket istatistiklerini döner — NPS, ortalama, dağılım.</summary>
    [HttpGet("results")]
    [RequirePermission("survey:view_results")]
    public async Task<IActionResult> GetResults(
        [FromQuery] long templateId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSurveyResultsQuery(templateId, from, to), ct);
        return Ok(result);
    }

    // ── request bodies ─────────────────────────────────────────────────────

    public record CreateTemplateBody(
        string Name,
        SurveyTriggerType TriggerType,
        int TriggerDelayHours = 24,
        string? Description = null
    );

    public record SendSurveyBody(
        long TemplateId,
        long PatientId,
        long BranchId,
        long CompanyId,
        SurveyChannel Channel,
        long? AppointmentId = null
    );
}
