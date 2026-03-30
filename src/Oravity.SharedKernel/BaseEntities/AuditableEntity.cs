namespace Oravity.SharedKernel.BaseEntities;

public abstract class AuditableEntity : BaseEntity
{
    public long? CreatedByUserId { get; protected set; }
    public long? UpdatedByUserId { get; protected set; }
    public long TenantId { get; protected set; }

    public void SetCreatedBy(long userId, long tenantId)
    {
        CreatedByUserId = userId;
        TenantId = tenantId;
    }

    public void SetUpdatedBy(long userId)
    {
        UpdatedByUserId = userId;
        MarkUpdated();
    }
}
