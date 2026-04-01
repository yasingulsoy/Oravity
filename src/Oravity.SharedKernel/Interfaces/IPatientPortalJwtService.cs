using Oravity.SharedKernel.Entities;

namespace Oravity.SharedKernel.Interfaces;

public record PortalTokenPair(string AccessToken, string RefreshToken, int ExpiresIn);

/// <summary>
/// Klinik JWT'den tamamen bağımsız hasta portalı token servisi.
/// Scheme: "PatientPortal", Secret: "Jwt:PortalSecret".
/// Access: 60 dk, Refresh: 30 gün.
/// </summary>
public interface IPatientPortalJwtService
{
    PortalTokenPair GenerateTokenPair(PatientPortalAccount account);
    string GenerateRefreshToken();
    string HashToken(string token);
}
