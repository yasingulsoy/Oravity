using MediatR;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Modules.Survey.Application.Commands;
using Oravity.Core.Modules.Survey.Application.Queries;

namespace Oravity.Core.Controllers;

/// <summary>
/// Anonim erişim — hasta anketi doldurma.
/// JWT gerektirmez, token ile doğrulama yapılır.
/// </summary>
[ApiController]
[Route("api/public/survey")]
[Tags("Anonim — Anket")]
public class PublicSurveyController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicSurveyController(IMediator mediator) => _mediator = mediator;

    /// <summary>Token ile anket sorularını getirir.</summary>
    [HttpGet("{token}")]
    public async Task<IActionResult> GetSurvey(
        string token,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSurveyByTokenQuery(token), ct);
        return Ok(result);
    }

    /// <summary>Anket cevaplarını gönderir.</summary>
    [HttpPost("{token}/submit")]
    public async Task<IActionResult> SubmitSurvey(
        string token,
        [FromBody] SubmitBody body,
        CancellationToken ct = default)
    {
        await _mediator.Send(new SubmitSurveyResponseCommand(token, body.Answers), ct);
        return Ok(new { Message = "Anketiniz tamamlandı. Teşekkür ederiz!" });
    }

    public record SubmitBody(IReadOnlyList<AnswerInput> Answers);
}
