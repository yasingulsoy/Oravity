namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hasta portalı oturumu — refresh token tabanlı.
/// BaseEntity türemez; public_id / is_deleted gerekmez.
/// token_hash: SHA-256 ile hash'lenmiş refresh token.
/// </summary>
public class PatientPortalSession
{
    public long Id { get; private set; }

    public long AccountId { get; private set; }
    public PatientPortalAccount Account { get; private set; } = default!;

    /// <summary>SHA-256 ile hash'lenmiş refresh token.</summary>
    public string TokenHash { get; private set; } = default!;

    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }

    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private PatientPortalSession() { }

    public static PatientPortalSession Create(
        long accountId, string tokenHash,
        string? ipAddress = null, string? userAgent = null,
        int refreshDays = 30)
    {
        return new PatientPortalSession
        {
            AccountId = accountId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            IsRevoked = false,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Revoke() => IsRevoked = true;

    public bool IsValid() => !IsRevoked && ExpiresAt > DateTime.UtcNow;
}
