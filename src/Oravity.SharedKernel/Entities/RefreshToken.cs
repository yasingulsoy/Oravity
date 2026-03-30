namespace Oravity.SharedKernel.Entities;

public class RefreshToken
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public User User { get; private set; } = default!;
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? IpAddress { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(long userId, string tokenHash, DateTime expiresAt, string? ipAddress)
    {
        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        };
    }

    public bool IsValid => !IsRevoked && ExpiresAt > DateTime.UtcNow;
    public void Revoke() => IsRevoked = true;
}
