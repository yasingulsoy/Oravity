namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Ödemenin tedavi kalemlerine dağıtımı — muhasebe köprü tablosu.
/// BaseEntity'den türemez (public_id/is_deleted gerekmez).
/// </summary>
public class PaymentAllocation
{
    public long Id { get; private set; }
    public long PaymentId { get; private set; }
    public Payment Payment { get; private set; } = default!;

    public long TreatmentPlanItemId { get; private set; }
    public TreatmentPlanItem TreatmentPlanItem { get; private set; } = default!;

    public decimal AllocatedAmount { get; private set; }
    public bool IsRefunded { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PaymentAllocation() { }

    public static PaymentAllocation Create(
        long paymentId,
        long treatmentPlanItemId,
        decimal allocatedAmount)
    {
        if (allocatedAmount <= 0)
            throw new ArgumentException("Dağıtım tutarı sıfırdan büyük olmalıdır.");

        return new PaymentAllocation
        {
            PaymentId           = paymentId,
            TreatmentPlanItemId = treatmentPlanItemId,
            AllocatedAmount     = allocatedAmount,
            IsRefunded          = false,
            CreatedAt           = DateTime.UtcNow
        };
    }

    public void MarkRefunded() => IsRefunded = true;
}
