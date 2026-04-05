namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Kullanıcının iki faktörlü doğrulama (2FA) ayarları.
/// UserId birincil anahtar — her kullanıcıya bir kayıt.
/// </summary>
public class User2FASettings
{
    public long UserId { get; private set; }
    public User User { get; private set; } = default!;

    public bool TotpEnabled { get; private set; }

    /// <summary>Şifrelenmiş TOTP secret. Etkinleştirilmeden önce geçici olarak saklanır.</summary>
    public string? TotpSecret { get; private set; }

    public bool SmsEnabled { get; private set; }
    public bool EmailEnabled { get; private set; }

    /// <summary>Tercih edilen yöntem: "totp" | "sms" | "email"</summary>
    public string? PreferredMethod { get; private set; }

    /// <summary>JSONB — yedek kodlar dizisi (her biri tek kullanımlık).</summary>
    public string? BackupCodes { get; private set; }

    /// <summary>Yedek kodların üretildiği zaman.</summary>
    public DateTime? BackupCodesAt { get; private set; }

    /// <summary>TOTP kurulumunun doğrulandığı zaman (EnableTotp çağrıldığında set edilir).</summary>
    public DateTime? TotpVerifiedAt { get; private set; }

    public DateTime? Last2faAt { get; private set; }
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private User2FASettings() { }

    public static User2FASettings CreateDefault(long userId)
        => new() { UserId = userId, UpdatedAt = DateTime.UtcNow };

    /// <summary>Kurulum aşamasında secret kaydeder (henüz etkin değil).</summary>
    public void SetupTotp(string encryptedSecret)
    {
        TotpSecret = encryptedSecret;
        UpdatedAt  = DateTime.UtcNow;
    }

    /// <summary>TOTP kodu doğrulandıktan sonra etkinleştirir.</summary>
    public void EnableTotp(string? backupCodesJson)
    {
        TotpEnabled     = true;
        TotpVerifiedAt  = DateTime.UtcNow;
        BackupCodes     = backupCodesJson;
        BackupCodesAt   = backupCodesJson is not null ? DateTime.UtcNow : null;
        PreferredMethod = "totp";
        UpdatedAt       = DateTime.UtcNow;
    }

    public void DisableTotp()
    {
        TotpEnabled = false;
        TotpSecret  = null;
        BackupCodes = null;
        if (PreferredMethod == "totp") PreferredMethod = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordUsed()
    {
        Last2faAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
