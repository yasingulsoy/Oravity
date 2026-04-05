using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Kullanıcının güvenilen cihazı.
/// 2FA kodu istenmeksizin giriş yapılabilir (ExpiresAt dolmadığı sürece).
/// </summary>
public class TrustedDevice : BaseEntity
{
    public long UserId { get; private set; }
    public User User { get; private set; } = default!;

    /// <summary>Cihaza özgü rastgele token (cookie veya header'da saklanır).</summary>
    public string DeviceToken { get; private set; } = default!;

    public string? DeviceName { get; private set; }
    public string? IpAddress { get; private set; }

    public DateTime TrustedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    private TrustedDevice() { }

    public static TrustedDevice Create(
        long userId,
        string deviceToken,
        string? deviceName,
        string? ipAddress,
        int expiryDays = 30)
    {
        if (string.IsNullOrWhiteSpace(deviceToken))
            throw new ArgumentException("Cihaz token boş olamaz.", nameof(deviceToken));

        return new TrustedDevice
        {
            UserId      = userId,
            DeviceToken = deviceToken,
            DeviceName  = deviceName,
            IpAddress   = ipAddress,
            TrustedAt   = DateTime.UtcNow,
            ExpiresAt   = DateTime.UtcNow.AddDays(expiryDays)
        };
    }

    public void MarkUsed()
    {
        LastUsedAt = DateTime.UtcNow;
        MarkUpdated();
    }
}
