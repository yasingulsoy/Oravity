using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Laboratuvar iş emrinin durum geçiş geçmişi (audit trail).
/// </summary>
public class LaboratoryWorkHistory : BaseEntity
{
    public long WorkId { get; private set; }
    public LaboratoryWork Work { get; private set; } = default!;

    public string? OldStatus { get; private set; }
    public string NewStatus { get; private set; } = default!;

    public DateTime ChangedAt { get; private set; } = DateTime.UtcNow;
    public long ChangedByUserId { get; private set; }

    public string? Notes { get; private set; }

    private LaboratoryWorkHistory() { }

    public static LaboratoryWorkHistory Create(
        long workId,
        string? oldStatus,
        string newStatus,
        long changedByUserId,
        string? notes)
    {
        return new LaboratoryWorkHistory
        {
            WorkId          = workId,
            OldStatus       = oldStatus,
            NewStatus       = newStatus,
            ChangedByUserId = changedByUserId,
            ChangedAt       = DateTime.UtcNow,
            Notes           = notes,
        };
    }
}
