using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hasta portalı hesabı (SPEC §HASTA PORTALI §3).
/// Klinik personel JWT'inden tamamen bağımsız; ayrı secret key ile imzalanır.
/// Giriş: email + şifre veya TC + doğum tarihi (OTP).
/// UNIQUE(patient_id) + UNIQUE(email).
/// </summary>
public class PatientPortalAccount : BaseEntity
{
    /// <summary>Bağlı hasta kaydı (nullable: henüz eşleştirilmemiş olabilir).</summary>
    public long? PatientId { get; private set; }
    public Patient? Patient { get; private set; }

    public string Email { get; private set; } = default!;
    public string Phone { get; private set; } = default!;

    public string PasswordHash { get; private set; } = default!;

    public bool IsActive { get; private set; } = true;
    public bool IsEmailVerified { get; private set; }
    public bool IsPhoneVerified { get; private set; }

    /// <summary>Email doğrulama tokeni (UUID tabanlı, tek kullanımlık).</summary>
    public string? EmailVerificationToken { get; private set; }

    /// <summary>Telefon OTP kodu (6 hane).</summary>
    public string? PhoneVerificationCode { get; private set; }

    /// <summary>Doğrulama token/kodu geçerlilik süresi.</summary>
    public DateTime? VerificationExpires { get; private set; }

    public DateTime? LastLoginAt { get; private set; }

    public string PreferredLanguageCode { get; private set; } = "tr";

    private PatientPortalAccount() { }

    public static PatientPortalAccount Create(
        string email, string phone, string passwordHash, long? patientId = null)
    {
        var account = new PatientPortalAccount
        {
            Email        = email.ToLowerInvariant().Trim(),
            Phone        = phone.Trim(),
            PasswordHash = passwordHash,
            PatientId    = patientId
        };

        // Email doğrulama tokeni hemen üret
        account.EmailVerificationToken = Guid.NewGuid().ToString("N");
        account.VerificationExpires    = DateTime.UtcNow.AddHours(24);

        return account;
    }

    /// <summary>Email adresi doğrulandığında çağrılır.</summary>
    public bool VerifyEmail(string token)
    {
        if (EmailVerificationToken != token || VerificationExpires < DateTime.UtcNow)
            return false;

        IsEmailVerified        = true;
        EmailVerificationToken = null;
        VerificationExpires    = null;
        MarkUpdated();
        return true;
    }

    /// <summary>6 haneli telefon OTP kodu üret (10 dk geçerli).</summary>
    public string GeneratePhoneVerificationCode()
    {
        PhoneVerificationCode = Random.Shared.Next(100000, 999999).ToString();
        VerificationExpires   = DateTime.UtcNow.AddMinutes(10);
        MarkUpdated();
        return PhoneVerificationCode;
    }

    public bool VerifyPhone(string code)
    {
        if (PhoneVerificationCode != code || VerificationExpires < DateTime.UtcNow)
            return false;

        IsPhoneVerified       = true;
        PhoneVerificationCode = null;
        VerificationExpires   = null;
        MarkUpdated();
        return true;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }
}
