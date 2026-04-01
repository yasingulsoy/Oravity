using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Modules.Appointment.OnlineBooking.Application.Commands;
using Oravity.Core.Modules.Core.PatientPortal.Application;
using Oravity.Core.Modules.Core.PatientPortal.Application.Commands;
using Oravity.Core.Modules.Core.PatientPortal.Application.Queries;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Controllers;

/// <summary>
/// Hasta portalı API — klinik personel JWT'inden ayrı "PatientPortal" scheme kullanır.
/// Anonim endpoint'ler (register / verify-email / login) herkes tarafından erişilebilir.
/// Korumalı endpoint'ler [Authorize(AuthenticationSchemes = "PatientPortal")] ile kısıtlıdır.
/// </summary>
[ApiController]
[Route("api/portal")]
[Tags("Patient Portal")]
public class PatientPortalController : ControllerBase
{
    private const string PortalScheme = "PatientPortal";

    private readonly IMediator _mediator;
    private readonly ICurrentPortalUser _portalUser;

    public PatientPortalController(IMediator mediator, ICurrentPortalUser portalUser)
    {
        _mediator   = mediator;
        _portalUser = portalUser;
    }

    // ── Anonim endpoint'ler ───────────────────────────────────────────────

    /// <summary>
    /// Yeni hasta portalı hesabı oluştur.
    /// E-posta doğrulama linki gönderilir (gerçek ortamda).
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterBody body,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new RegisterPatientPortalCommand(body.Email, body.Phone, body.Password, body.PatientId), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// E-posta adresini doğrula (link üzerinden gelen token).
    /// </summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailBody body,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new VerifyPatientPortalEmailCommand(body.Token), ct);
        return Ok(result);
    }

    /// <summary>
    /// Hasta portalı girişi — "PatientPortal" scheme JWT döner.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginBody body,
        CancellationToken ct = default)
    {
        var ip        = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _mediator.Send(
            new PatientPortalLoginCommand(body.Email, body.Password, ip, userAgent), ct);
        return Ok(result);
    }

    // ── Korumalı endpoint'ler ─────────────────────────────────────────────

    /// <summary>
    /// Çıkış — refresh token revoke edilir.
    /// </summary>
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = PortalScheme)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutBody body,
        CancellationToken ct = default)
    {
        await _mediator.Send(new PatientPortalLogoutCommand(body.RefreshToken), ct);
        return NoContent();
    }

    /// <summary>
    /// Oturum açmış hastanın profil bilgisi.
    /// </summary>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = PortalScheme)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe(CancellationToken ct = default)
    {
        var account = await _mediator.Send(
            new GetMeQuery(_portalUser.AccountId), ct);
        return Ok(account);
    }

    /// <summary>
    /// Hastanın randevuları. futureOnly=true → gelecek, false → geçmiş, null → hepsi.
    /// </summary>
    [HttpGet("appointments")]
    [Authorize(AuthenticationSchemes = PortalScheme)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAppointments(
        [FromQuery] bool? futureOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetMyAppointmentsQuery(futureOnly, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Hastanın cari bakiyesi ve ödeme geçmişi.
    /// </summary>
    [HttpGet("balance")]
    [Authorize(AuthenticationSchemes = PortalScheme)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetMyBalanceQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Hastanın tedavi planları. completedOnly=true → tamamlanan, false → devam eden, null → hepsi.
    /// </summary>
    [HttpGet("treatment-plans")]
    [Authorize(AuthenticationSchemes = PortalScheme)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTreatmentPlans(
        [FromQuery] bool? completedOnly = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetMyTreatmentPlansQuery(completedOnly), ct);
        return Ok(result);
    }

    /// <summary>
    /// Hastanın dosyaları (röntgen, ONAM, raporlar). fileType filtresi opsiyonel.
    /// </summary>
    [HttpGet("files")]
    [Authorize(AuthenticationSchemes = PortalScheme)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFiles(
        [FromQuery] PatientFileType? fileType = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetMyFilesQuery(fileType), ct);
        return Ok(result);
    }

    /// <summary>
    /// Online randevu talebi oluştur (mevcut OnlineBooking modülünü kullanır).
    /// </summary>
    [HttpPost("appointments/request")]
    [Authorize(AuthenticationSchemes = PortalScheme)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> RequestAppointment(
        [FromBody] AppointmentRequestBody body,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CreateOnlineBookingRequestCommand(
            BranchSlug:    body.BranchSlug,
            DoctorId:      body.DoctorId,
            PatientType:   BookingPatientType.Existing,
            RequestedDate: body.RequestedDate,
            RequestedTime: body.RequestedTime,
            SlotDuration:  body.SlotDuration,
            Source:        BookingSource.Portal,
            PatientId:     _portalUser.PatientId,
            PatientNote:   body.PatientNote), ct);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Şifre değiştir — tüm aktif oturumlar sonlandırılır.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize(AuthenticationSchemes = PortalScheme)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordBody body,
        CancellationToken ct = default)
    {
        await _mediator.Send(
            new ChangePatientPortalPasswordCommand(body.CurrentPassword, body.NewPassword), ct);
        return NoContent();
    }
}

// ── Request body record'ları ─────────────────────────────────────────────
public record RegisterBody(
    string Email,
    string Phone,
    string Password,
    long? PatientId = null
);

public record VerifyEmailBody(string Token);

public record LoginBody(string Email, string Password);

public record LogoutBody(string RefreshToken);

public record ChangePasswordBody(string CurrentPassword, string NewPassword);

public record AppointmentRequestBody(
    string BranchSlug,
    long DoctorId,
    DateOnly RequestedDate,
    TimeOnly RequestedTime,
    int SlotDuration,
    string? PatientNote = null
);
