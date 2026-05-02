using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Consent.Application;
using Oravity.Core.Modules.Consent.Application.Commands;
using Oravity.Core.Modules.Consent.Application.Queries;
using Oravity.Core.Services;

namespace Oravity.Core.Controllers;

/// <summary>
/// Dijital onam formu yönetimi.
/// Şablon CRUD (authenticated) + imzalama (public).
/// </summary>
[ApiController]
[Produces("application/json")]
public class ConsentController : ControllerBase
{
    private readonly IMediator        _mediator;
    private readonly ConsentPdfService _pdfService;

    public ConsentController(IMediator mediator, ConsentPdfService pdfService)
    {
        _mediator   = mediator;
        _pdfService = pdfService;
    }

    // ── Şablon Sorguları ──────────────────────────────────────────────────

    /// <summary>Şirkete ait onam formu şablonlarını listeler.</summary>
    [HttpGet("api/consent-forms")]
    [Authorize]
    [RequirePermission("consent_form:view")]
    [ProducesResponseType(typeof(IReadOnlyList<ConsentFormTemplateSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTemplates([FromQuery] bool activeOnly = false)
    {
        var result = await _mediator.Send(new GetConsentFormTemplatesQuery(activeOnly));
        return Ok(result);
    }

    /// <summary>Onam formu şablonu detayını getirir.</summary>
    [HttpGet("api/consent-forms/{id:guid}")]
    [Authorize]
    [RequirePermission("consent_form:view")]
    [ProducesResponseType(typeof(ConsentFormTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate(Guid id)
    {
        var result = await _mediator.Send(new GetConsentFormTemplateByIdQuery(id));
        return Ok(result);
    }

    // ── Şablon Komutları ──────────────────────────────────────────────────

    /// <summary>Yeni onam formu şablonu oluşturur.</summary>
    [HttpPost("api/consent-forms")]
    [Authorize]
    [RequirePermission("consent_form:manage")]
    [ProducesResponseType(typeof(ConsentFormTemplateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateConsentFormTemplateRequest request)
    {
        var result = await _mediator.Send(new CreateConsentFormTemplateCommand(
            request.Code,
            request.Name,
            request.Language,
            request.Version,
            request.ContentHtml,
            request.CheckboxesJson,
            request.AppliesToAllTreatments,
            request.TreatmentCategoryIdsJson,
            request.ShowDentalChart,
            request.ShowTreatmentTable,
            request.RequireDoctorSignature));

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Onam formu şablonunu günceller.</summary>
    [HttpPut("api/consent-forms/{id:guid}")]
    [Authorize]
    [RequirePermission("consent_form:manage")]
    [ProducesResponseType(typeof(ConsentFormTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateConsentFormTemplateRequest request)
    {
        var result = await _mediator.Send(new UpdateConsentFormTemplateCommand(
            id,
            request.Name,
            request.Language,
            request.Version,
            request.ContentHtml,
            request.CheckboxesJson,
            request.AppliesToAllTreatments,
            request.TreatmentCategoryIdsJson,
            request.ShowDentalChart,
            request.ShowTreatmentTable,
            request.RequireDoctorSignature));

        return Ok(result);
    }

    /// <summary>Onam formu şablonunu siler.</summary>
    [HttpDelete("api/consent-forms/{id:guid}")]
    [Authorize]
    [RequirePermission("consent_form:manage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        await _mediator.Send(new DeleteConsentFormTemplateCommand(id));
        return NoContent();
    }

    // ── Onam Örneği ───────────────────────────────────────────────────────

    /// <summary>Onam örneği oluşturur. Plan bağlı veya standalone (sadece hasta publicId) olabilir.</summary>
    [HttpPost("api/consent-instances")]
    [Authorize]
    [RequirePermission("treatment_plan:complete")]
    [ProducesResponseType(typeof(ConsentInstanceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInstance([FromBody] CreateConsentInstanceRequest request)
    {
        var result = await _mediator.Send(new CreateConsentInstanceCommand(
            request.TreatmentPlanPublicId,
            request.FormTemplatePublicId,
            request.ItemPublicIds,
            request.DeliveryMethod,
            request.PatientPublicId));

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Onam örneğini iptal eder.</summary>
    [HttpPut("api/consent-instances/{id:guid}/cancel")]
    [Authorize]
    [RequirePermission("consent_form:manage")]
    [ProducesResponseType(typeof(ConsentInstanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelInstance(Guid id)
    {
        var result = await _mediator.Send(new CancelConsentInstanceCommand(id));
        return Ok(result);
    }

    /// <summary>Tedavi planına ait onam örneklerini listeler.</summary>
    [HttpGet("api/treatment-plans/{planId:guid}/consent-instances")]
    [Authorize]
    [RequirePermission("treatment_plan:view")]
    [ProducesResponseType(typeof(IReadOnlyList<ConsentInstanceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInstancesByPlan(Guid planId)
    {
        var result = await _mediator.Send(new GetConsentInstancesByPlanQuery(planId));
        return Ok(result);
    }

    /// <summary>Hastaya ait tüm onam örneklerini listeler (standalone dahil).</summary>
    [HttpGet("api/patients/{patientId:guid}/consent-instances")]
    [Authorize]
    [RequirePermission("treatment_plan:view")]
    [ProducesResponseType(typeof(IReadOnlyList<ConsentInstanceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInstancesByPatient(Guid patientId)
    {
        var result = await _mediator.Send(new GetPatientConsentInstancesQuery(patientId));
        return Ok(result);
    }

    /// <summary>İmzalanan onam formunu PDF olarak indirir.</summary>
    [HttpGet("api/consent-instances/{id:guid}/pdf")]
    [Authorize]
    [RequirePermission("consent_form:view")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var bytes = await _pdfService.GenerateAsync(id);
        return File(bytes, "application/pdf", $"onam-formu-{id:N}.pdf");
    }

    // ── Public (anonim) imzalama ──────────────────────────────────────────

    /// <summary>Token ile onam formunu getirir — hasta imzalamadan önce okur.</summary>
    [HttpGet("api/public/consent/{token}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ConsentPublicDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicForm(string token)
    {
        var result = await _mediator.Send(new GetConsentByTokenQuery(token));
        if (result is null) return NotFound(new { message = "Geçersiz form bağlantısı." });
        return Ok(result);
    }

    /// <summary>Onam formunu imzalar — public endpoint, auth gerekmez.</summary>
    [HttpPost("api/public/consent/{token}/sign")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SignConsentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SignForm(string token, [FromBody] SignConsentRequest request)
    {
        var signerIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var signerDevice = Request.Headers.UserAgent.ToString();

        var result = await _mediator.Send(new SignConsentInstanceCommand(
            token,
            request.SignerName,
            request.SignatureDataBase64,
            request.DoctorSignatureDataBase64,
            request.CheckboxAnswersJson,
            signerIp,
            signerDevice));

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }
}

// ─── Request DTO'lar ──────────────────────────────────────────────────────────

public record CreateConsentFormTemplateRequest(
    string  Code,
    string  Name,
    string  Language,
    string  Version,
    string  ContentHtml,
    string  CheckboxesJson,
    bool    AppliesToAllTreatments,
    string? TreatmentCategoryIdsJson,
    bool    ShowDentalChart,
    bool    ShowTreatmentTable,
    bool    RequireDoctorSignature
);

public record UpdateConsentFormTemplateRequest(
    string  Name,
    string  Language,
    string  Version,
    string  ContentHtml,
    string  CheckboxesJson,
    bool    AppliesToAllTreatments,
    string? TreatmentCategoryIdsJson,
    bool    ShowDentalChart,
    bool    ShowTreatmentTable,
    bool    RequireDoctorSignature
);

public record CreateConsentInstanceRequest(
    Guid?        TreatmentPlanPublicId,
    Guid         FormTemplatePublicId,
    List<string> ItemPublicIds,
    string       DeliveryMethod,
    Guid?        PatientPublicId = null
);

public record SignConsentRequest(
    string? SignerName,
    string? SignatureDataBase64,
    string? DoctorSignatureDataBase64,
    string? CheckboxAnswersJson
);
