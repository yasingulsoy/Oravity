using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Infrastructure.Services;

/// <summary>
/// SSO (OpenID Connect) callback sonrası kullanıcı eşleme ve Oravity JWT üretimi.
/// </summary>
public class SsoCallbackHandler
{
    private readonly AppDbContext _db;
    private readonly IJwtService   _jwtService;

    public SsoCallbackHandler(AppDbContext db, IJwtService jwtService)
    {
        _db         = db;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Claim'lerden e-posta ve subject okur; kullanıcıyı bulur veya oluşturur; refresh token ile JWT döner.
    /// </summary>
    public async Task<SsoLoginResponse> HandleCallback(
        string provider,
        ClaimsPrincipal principal,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var email   = ResolveEmail(principal);
        var subject = ResolveSubject(principal);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(subject))
            throw new InvalidOperationException("SSO: gerekli claim'ler eksik (email veya subject).");

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var providerKey     = provider.Trim().ToLowerInvariant();

        var existingBySso = await _db.Users
            .FirstOrDefaultAsync(
                u => u.SsoProvider == providerKey && u.SsoSubject == subject,
                ct);

        User user;

        if (existingBySso != null)
            user = existingBySso;
        else
        {
            var byEmail = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
            if (byEmail != null)
            {
                if (!string.IsNullOrEmpty(byEmail.SsoSubject) && byEmail.SsoSubject != subject)
                    throw new InvalidOperationException(
                        "Bu e-posta adresi farklı bir SSO hesabına bağlı.");

                if (string.IsNullOrEmpty(byEmail.SsoProvider))
                    byEmail.LinkSsoIdentity(providerKey, subject, email);

                user = byEmail;
            }
            else
            {
                var displayName = principal.FindFirstValue(ClaimTypes.Name)
                    ?? principal.FindFirstValue("name")
                    ?? normalizedEmail.Split('@')[0];
                var dummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());
                user = User.CreateForSso(
                    normalizedEmail,
                    displayName,
                    dummyHash,
                    providerKey,
                    subject,
                    email);
                _db.Users.Add(user);
            }
        }


        if (!user.IsActive)
            throw new UnauthorizedAccessException("Hesabınız aktif değil.");

        _db.LoginAttempts.Add(LoginAttempt.Create(normalizedEmail, ipAddress, true));

        var accessToken  = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var tokenHash    = _jwtService.HashToken(refreshToken);

        _db.RefreshTokens.Add(
            RefreshToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddDays(7), ipAddress));
        user.SetLastLoginAt();

        await _db.SaveChangesAsync(ct);

        return new SsoLoginResponse(accessToken, refreshToken, 15 * 60);
    }

    private static string? ResolveEmail(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("preferred_username")
            ?? principal.FindFirstValue("email")
            ?? principal.FindFirstValue("upn");
    }

    private static string? ResolveSubject(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? principal.FindFirstValue("oid");
    }
}

/// <summary>SSO tamamlandıktan sonra dönen token çifti (LoginResponse ile aynı şekil).</summary>
public record SsoLoginResponse(string AccessToken, string RefreshToken, int ExpiresIn, string TokenType = "Bearer");
