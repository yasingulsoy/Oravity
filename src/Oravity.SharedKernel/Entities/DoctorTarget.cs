using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hekim × Şube × Ay bazında ciro hedefi. Hedefe ulaşılınca şablondaki bonus
/// prim oranı devreye girer.
/// </summary>
public class DoctorTarget : AuditableEntity
{
    public long DoctorId { get; private set; }
    public User Doctor { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public int Year { get; private set; }
    public int Month { get; private set; } // 1–12

    public decimal TargetAmount { get; private set; }

    private DoctorTarget() { }

    public static DoctorTarget Create(long doctorId, long branchId, int year, int month, decimal amount)
    {
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));
        if (amount < 0)              throw new ArgumentOutOfRangeException(nameof(amount));

        return new DoctorTarget
        {
            DoctorId     = doctorId,
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
