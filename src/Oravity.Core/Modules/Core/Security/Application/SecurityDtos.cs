namespace Oravity.Core.Modules.Core.Security.Application;

// ─── Response DTO'lar ──────────────────────────────────────────────────────

public record TwoFAStatusResponse(
    bool    TotpEnabled,
    bool    SmsEnabled,
    bool    EmailEnabled,
    string? PreferredMethod,
    DateTime? Last2faAt
);

public record Setup2FAResponse(
    /// <summary>QR kod oluşturmak için otpauth URI'si.</summary>
    string OtpauthUri,
    /// <summary>Manuel giriş için base32 secret.</summary>
    string Secret
);
