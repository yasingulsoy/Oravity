using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>Şube × Ay bazında klinik ciro hedefi.</summary>
public class BranchTarget : AuditableEntity
{
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public int Year { get; private set; }
    public int Month { get; private set; } // 1–12

    public decimal TargetAmount { get; private set; }

    private BranchTarget() { }

    public static BranchTarget Create(long branchId, int year, int month, decimal amount)
    {
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));
        if (amount < 0)              throw new ArgumentOutOfRangeException(nameof(amount));

        return new BranchTarget
        {
            BranchId     = branchId,
            Year         = year,
            Month        = month,
            TargetAmount = amount,
        };
    }

    public void SetAmount(decimal amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        TargetAmount = amount;
        MarkUpdated();
    }
}
