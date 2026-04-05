namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Şubeye özel güvenlik politikası.
/// BranchId birincil anahtar — her şubeye bir kayıt.
/// </summary>
public class BranchSecurityPolicy
{
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public bool TwoFaRequired { get; private set; }

    /// <summary>İç IP aralıklarından giriş için 2FA atlanabilir.</summary>
    public bool TwoFaSkipInternalIp { get; private set; } = true;

    /// <summary>JSONB — izin verilen IP aralıkları listesi. Örn: '["192.168.1.0/24"]'</summary>
    public string? AllowedIpRanges { get; private set; }

    /// <summary>Oturum zaman aşımı (dakika). Varsayılan: 8 saat.</summary>
    public int SessionTimeoutMinutes { get; private set; } = 480;

    public int MaxFailedAttempts { get; private set; } = 5;

    /// <summary>Hesap kilitleme süresi (dakika).</summary>
    public int LockoutMinutes { get; private set; } = 30;

    private BranchSecurityPolicy() { }

    public static BranchSecurityPolicy CreateDefault(long branchId)
        => new()
        {
            BranchId               = branchId,
            TwoFaRequired          = false,
            TwoFaSkipInternalIp    = true,
            SessionTimeoutMinutes  = 480,
            MaxFailedAttempts      = 5,
            LockoutMinutes         = 30
        };

    public void Update(
        bool twoFaRequired,
        bool twoFaSkipInternalIp,
        string? allowedIpRanges,
        int sessionTimeoutMinutes,
        int maxFailedAttempts,
        int lockoutMinutes)
    {
        TwoFaRequired         = twoFaRequired;
        TwoFaSkipInternalIp   = twoFaSkipInternalIp;
        AllowedIpRanges       = allowedIpRanges;
        SessionTimeoutMinutes = sessionTimeoutMinutes;
        MaxFailedAttempts     = maxFailedAttempts;
        LockoutMinutes        = lockoutMinutes;
    }
}
