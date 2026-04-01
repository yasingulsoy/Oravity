using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientPortal.Infrastructure.Services;

/// <summary>
/// Hasta portalı JWT servisi — klinik personel token'ından ayrı.
/// Config: Jwt:PortalSecret, Jwt:Issuer, Jwt:Audience.
/// Claims: portal_account_id, patient_id, company_id, branch_id.
/// Access: 60 dk | Refresh: 30 gün.
/// </summary>
public class PatientPortalJwtService : IPatientPortalJwtService
{
    private readonly IConfiguration _config;

    public PatientPortalJwtService(IConfiguration config)
    {
        _config = config;
    }

    public PortalTokenPair GenerateTokenPair(PatientPortalAccount account)
    {
        var accessToken  = GenerateAccessToken(account);
        var refreshToken = GenerateRefreshToken();
        return new PortalTokenPair(accessToken, refreshToken, 3600);
    }

    private string GenerateAccessToken(PatientPortalAccount account)
    {
        var secret = _config["Jwt:PortalSecret"]
            ?? throw new InvalidOperationException("Jwt:PortalSecret ayarı eksik.");

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,  account.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, account.Email),
            new(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
            new("portal_account_id", account.Id.ToString()),
            new("patient_id",        account.PatientId?.ToString() ?? ""),
            new("public_id",         account.PublicId.ToString()),
            new("preferred_lang",    account.PreferredLanguageCode),
            // company_id / branch_id patient kaydından çözümlenir;
            // portal JWT ayrı bir kompanya/şube scope'u taşımaz.
        };

        var token = new JwtSecurityToken(
            issuer:            _config["Jwt:Issuer"],
            audience:          _config["Jwt:Audience"],
            claims:            claims,
            notBefore:         DateTime.UtcNow,
            expires:           DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
