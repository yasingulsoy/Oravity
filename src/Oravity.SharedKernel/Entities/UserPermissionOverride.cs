using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Kullanıcıya rol şablonunun dışında izin ver ya da iptal et.
/// </summary>
public class UserPermissionOverride : BaseEntity
{
    public long UserId { get; private set; }
    public long PermissionId { get; private set; }

    public long? CompanyId { get; private set; }
    public long? BranchId { get; private set; }

    /// <summary>
    /// True = izin verildi (grant), False = izin iptal edildi (revoke).
    /// </summary>
    public bool IsGranted { get; private set; }

    public User User { get; private set; } = default!;
    public Permission Permission { get; private set; } = default!;
    public Company? Company { get; private set; }
    public Branch? Branch { get; private set; }

    private UserPermissionOverride() { }

    public static UserPermissionOverride Grant(long userId, long permissionId, long? companyId = null, long? branchId = null)
        => Create(userId, permissionId, companyId, branchId, isGranted: true);

    public static UserPermissionOverride Revoke(long userId, long permissionId, long? companyId = null, long? branchId = null)
        => Create(userId, permissionId, companyId, branchId, isGranted: false);

    private static UserPermissionOverride Create(long userId, long permissionId, long? companyId, long? branchId, bool isGranted)
    {
        return new UserPermissionOverride
        {
            UserId = userId,
            PermissionId = permissionId,
            CompanyId = companyId,
            BranchId = branchId,
            IsGranted = isGranted
        };
    }
}
