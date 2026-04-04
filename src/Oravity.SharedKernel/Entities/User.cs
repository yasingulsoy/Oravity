using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;

    /// <summary>
    /// Null = şube diline miras bırakır.
    /// </summary>
    public string? PreferredLanguageCode { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsPlatformAdmin { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>SSO sağlayıcısı: 'microsoft', 'google', 'okta' vb.</summary>
    public string? SsoProvider { get; private set; }
    /// <summary>Sağlayıcıdaki benzersiz kullanıcı kimliği (subject).</summary>
    public string? SsoSubject { get; private set; }
    /// <summary>Sağlayıcıdan gelen e-posta (isteğe bağlı, denormalize).</summary>
    public string? SsoEmail { get; private set; }

    public ICollection<UserRoleAssignment> RoleAssignments { get; private set; } = [];
    public ICollection<UserPermissionOverride> PermissionOverrides { get; private set; } = [];

    private User() { }

    public static User Create(string email, string fullName, string passwordHash, bool isPlatformAdmin = false)
    {
        return new User
        {
            Email = email.ToLowerInvariant(),
            FullName = fullName,
            PasswordHash = passwordHash,
            IsActive = true,
            IsPlatformAdmin = isPlatformAdmin
        };
    }

    public void SetPreferredLanguage(string? code) => PreferredLanguageCode = code;
    public void SetActive(bool value) => IsActive = value;
    public void UpdatePasswordHash(string hash) => PasswordHash = hash;
    public void SetPlatformAdmin(bool value) => IsPlatformAdmin = value;
    public void SetLastLoginAt() => LastLoginAt = DateTime.UtcNow;

    /// <summary>SSO ile oluşturulan hesap — parola ile giriş kullanılmaz.</summary>
    public static User CreateForSso(
        string email,
        string fullName,
        string passwordHashPlaceholder,
        string ssoProvider,
        string ssoSubject,
        string? ssoEmail)
    {
        return new User
        {
            Email       = email.ToLowerInvariant(),
            FullName    = fullName,
            PasswordHash = passwordHashPlaceholder,
            IsActive    = true,
            SsoProvider = ssoProvider,
            SsoSubject  = ssoSubject,
            SsoEmail    = ssoEmail
        };
    }

    /// <summary>Mevcut hesaba SSO kimliği bağlar (ör. ilk SSO öncesi yerel hesap).</summary>
    public void LinkSsoIdentity(string ssoProvider, string ssoSubject, string? ssoEmail)
    {
        SsoProvider = ssoProvider;
        SsoSubject  = ssoSubject;
        SsoEmail    = ssoEmail;
        MarkUpdated();
    }
}
