using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public class UserRoleAssignment : BaseEntity
{
    public long UserId { get; private set; }
    public long RoleTemplateId { get; private set; }

    /// <summary>
    /// Null = platform admin (tüm şirketler). Dolu = belirli şirket.
    /// </summary>
    public long? CompanyId { get; private set; }

    /// <summary>
    /// Null = tüm şubeler (firma admin). Dolu = belirli şube.
    /// </summary>
    public long? BranchId { get; private set; }

    public bool IsActive { get; private set; } = true;
    public DateTime AssignedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; private set; }

    public User User { get; private set; } = default!;
    public RoleTemplate RoleTemplate { get; private set; } = default!;
    public Company? Company { get; private set; }
    public Branch? Branch { get; private set; }

    private UserRoleAssignment() { }

    public static UserRoleAssignment Create(
        long userId,
        long roleTemplateId,
        long? companyId = null,
        long? branchId = null,
        DateTime? expiresAt = null)
    {
        return new UserRoleAssignment
        {
            UserId = userId,
            RoleTemplateId = roleTemplateId,
            CompanyId = companyId,
            BranchId = branchId,
            IsActive = true,
            AssignedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    public void Revoke() => IsActive = false;
}
