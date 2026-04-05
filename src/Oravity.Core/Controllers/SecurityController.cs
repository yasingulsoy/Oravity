using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Modules.Core.Security.Application;
using Oravity.Core.Modules.Core.Security.Application.Commands;
using Oravity.Core.Modules.Core.Security.Application.Queries;

namespace Oravity.Core.Controllers;

/// <summary>
/// Güvenlik yönetimi — iki faktörlü doğrulama (2FA).
/// Tüm endpoint'ler JWT [Authorize] koruması altındadır.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class SecurityController : ControllerBase
{
    private readonly IMediator _mediator;

    public SecurityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Kullanıcının 2FA durumunu getirir.</summary>
    [HttpGet("api/security/2fa/status")]
    [ProducesResponseType(typeof(TwoFAStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStatus()
    {
        var result = await _mediator.Send(new Get2FAStatusQuery());
        return Ok(result);
    }

    /// <summary>
    /// TOTP kurulumu başlatır.
    /// QR kod için otpauth URI ve manuel giriş için base32 secret döner.
    /// Ardından /verify-setup ile doğrulanana kadar etkinleşmez.
    /// </summary>
    [HttpPost("api/security/2fa/setup")]
    [ProducesResponseType(typeof(Setup2FAResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Setup()
    {
        var result = await _mediator.Send(new Setup2FACommand());
        return Ok(result);
    }

    /// <summary>
    /// TOTP kodunu doğrular ve 2FA'yı etkinleştirir.
    /// Başarılı olursa yedek kodlar üretilir.
    /// </summary>
    [HttpPost("api/security/2fa/verify-setup")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifySetup([FromBody] VerifySetupRequest request)
    {
        await _mediator.Send(new Verify2FASetupCommand(request.TotpCode));
        return NoContent();
    }

    /// <summary>2FA'yı devre dışı bırakır.</summary>
    [HttpDelete("api/security/2fa")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Disable()
    {
        await _mediator.Send(new Disable2FACommand());
        return NoContent();
    }
}

// ─── Request DTO'lar ───────────────────────────────────────────────────────

public record VerifySetupRequest(string TotpCode);
