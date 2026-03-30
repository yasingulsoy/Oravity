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
}
