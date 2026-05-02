using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Auth.Application.Commands;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public AuthController(IMediator mediator, AppDbContext db, ITenantContext tenant)
    {
        _mediator = mediator;
        _db       = db;
        _tenant   = tenant;
    }

    /// <summary>Kullanıcı girişi — access + refresh token döner</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password, ip, request.BranchId));
        return Ok(result);
    }

    /// <summary>Refresh token ile yeni access token al</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken, ip));
        return Ok(result);
    }

    /// <summary>Çıkış yap — refresh token'ı iptal eder</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        await _mediator.Send(new LogoutCommand(request.RefreshToken));
        return NoContent();
    }

    /// <summary>
    /// Microsoft hesabı ile SSO başlatır (tarayıcı). Dönüş yolu: <c>/api/auth/sso/callback/microsoft</c>
    /// — yanıtı OpenIdConnect middleware üretir (access + refresh token JSON).
    /// </summary>
    [HttpGet("sso/microsoft")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult SsoMicrosoft([FromServices] IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration["Sso:Microsoft:Authority"]) ||
            string.IsNullOrWhiteSpace(configuration["Sso:Microsoft:ClientId"]))
            return NotFound();

        return Challenge(new AuthenticationProperties(), "Microsoft");
    }

    /// <summary>Mevcut kullanıcı bilgisi (JWT claim'leri)</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Me()
    {
        var claims = User.Claims
            .Select(c => new { c.Type, c.Value })
            .ToList();
        return Ok(new { claims });
    }

    /// <summary>Oturumdaki kullanıcının sahip olduğu tüm izin kodlarını döner.</summary>
    [HttpGet("my-permissions")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyPermissions()
    {
        if (_tenant.IsPlatformAdmin)
        {
            // Platform admin her şeyi yapabilir; tüm izin kodlarını döndür
            var all = await _db.Permissions.AsNoTracking()
                .Select(p => p.Code)
                .ToListAsync();
            return Ok(all);
        }

        var permissions = await _db.UserRoleAssignments
            .Where(a => a.UserId == _tenant.UserId
                        && a.IsActive
                        && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
            .SelectMany(a => a.RoleTemplate.RoleTemplatePermissions)
            .Select(rtp => rtp.Permission.Code)
            .Distinct()
            .ToListAsync();

        // UserPermissionOverrides (bireysel grant)
        var overrides = await _db.UserPermissionOverrides
            .Where(o => o.UserId == _tenant.UserId && o.IsGranted)
            .Select(o => o.Permission.Code)
            .ToListAsync();

        var merged = permissions.Union(overrides).Distinct().ToList();
        return Ok(merged);
    }
}

public record LoginRequest(string Email, string Password, long? BranchId = null);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
