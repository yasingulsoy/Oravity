using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Bir laboratuvarın hangi şube(ler) ile çalıştığı eşlemesi ve öncelik sırası.
/// </summary>
public class LaboratoryBranchAssignment : AuditableEntity
{
    public long LaboratoryId { get; private set; }
    public Laboratory Laboratory { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    /// <summary>Birden fazla lab varsa tercih sırası (0 = en öncelikli).</summary>
    public int Priority { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTime AssignedAt { get; private set; } = DateTime.UtcNow;

    private LaboratoryBranchAssignment() { }

    public static LaboratoryBranchAssignment Create(long laboratoryId, long branchId, int priority)
    {
        return new LaboratoryBranchAssignment
        {
            LaboratoryId = laboratoryId,
            BranchId     = branchId,
            Priority     = priority,
            IsActive     = true,
            AssignedAt   = DateTime.UtcNow,
        };
    }

    public void Update(int priority, bool isActive)
    {
        Priority = priority;
        IsActive = isActive;
        MarkUpdated();
    }
}
