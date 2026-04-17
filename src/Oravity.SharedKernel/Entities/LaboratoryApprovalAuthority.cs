using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Laboratuvar iş emri onay/red yetkisine sahip kullanıcı (şube bazında veya global).
/// </summary>
public class LaboratoryApprovalAuthority : AuditableEntity
{
    public long UserId { get; private set; }
    public User User { get; private set; } = default!;

    /// <summary>null = tüm şubelerde onay yetkili.</summary>
    public long? BranchId { get; private set; }
    public Branch? Branch { get; private set; }

    public bool CanApprove { get; private set; } = true;
    public bool CanReject { get; private set; } = true;
    public bool NotificationEnabled { get; private set; } = true;

    private LaboratoryApprovalAuthority() { }

    public static LaboratoryApprovalAuthority Create(
        long userId,
        long? branchId,
        bool canApprove,
        bool canReject,
        bool notificationEnabled)
    {
        return new LaboratoryApprovalAuthority
        {
            UserId              = userId,
            BranchId            = branchId,
            CanApprove          = canApprove,
            CanReject           = canReject,
            NotificationEnabled = notificationEnabled,
        };
    }

    public void Update(bool canApprove, bool canReject, bool notificationEnabled)
    {
        CanApprove          = canApprove;
        CanReject           = canReject;
        NotificationEnabled = notificationEnabled;
        MarkUpdated();
    }
}
