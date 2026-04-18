namespace Oravity.SharedKernel.Entities;

public enum AllocationMethod
{
    Automatic = 1, // Otomatik dağıtım
    Manual    = 2  // Yetkili onayı ile manuel
}

/// <summary>
/// Ödemenin tedavi kalemlerine dağıtımı — muhasebe köprü tablosu.
/// Kaynağı hasta (Payment) veya kurum (InstitutionPayment) olabilir.
/// BaseEntity'den türemez (public_id/is_deleted gerekmez).
/// </summary>
public class PaymentAllocation
{
    public long Id { get; private set; }

    /// <summary>Hasta ödemesi kaynağı — Source == Patient ise dolu.</summary>
    public long? PaymentId { get; private set; }
    public Payment? Payment { get; private set; }

    /// <summary>Kurum ödemesi kaynağı — Source == Institution ise dolu.</summary>
    public long? InstitutionPaymentId { get; private set; }
    public InstitutionPayment? InstitutionPayment { get; private set; }

    public long TreatmentPlanItemId { get; private set; }
    public TreatmentPlanItem TreatmentPlanItem { get; private set; } = default!;

    public long BranchId { get; private set; }

    public AllocationSource Source { get; private set; }
    public AllocationMethod Method { get; private set; }

    public decimal AllocatedAmount { get; private set; }

    /// <summary>Manuel dağıtım ise ilgili onay kaydı.</summary>
    public long? ApprovalId { get; private set; }
    public AllocationApproval? Approval { get; private set; }

    public long AllocatedByUserId { get; private set; }
    public string? Notes { get; private set; }

    public bool IsRefunded { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PaymentAllocation() { }

    /// <summary>Hasta ödemesinden otomatik dağıtım.</summary>
    public static PaymentAllocation CreateFromPatient(
        long paymentId,
        long treatmentPlanItemId,
        long branchId,
        decimal allocatedAmount,
        long allocatedByUserId,
        AllocationMethod method = AllocationMethod.Automatic,
        long? approvalId = null,
        string? notes = null)
    {
        if (allocatedAmount <= 0)
            throw new ArgumentException("Dağıtım tutarı sıfırdan büyük olmalıdır.");

        return new PaymentAllocation
        {
            PaymentId           = paymentId,
            TreatmentPlanItemId = treatmentPlanItemId,
            BranchId            = branchId,
            Source              = AllocationSource.Patient,
            Method              = method,
            AllocatedAmount     = allocatedAmount,
            ApprovalId          = approvalId,
            AllocatedByUserId   = allocatedByUserId,
            Notes               = notes,
            IsRefunded          = false,
            CreatedAt           = DateTime.UtcNow
        };
    }

    /// <summary>Kurum ödemesinden dağıtım.</summary>
    public static PaymentAllocation CreateFromInstitution(
        long institutionPaymentId,
        long treatmentPlanItemId,
        long branchId,
        decimal allocatedAmount,
        long allocatedByUserId,
        AllocationMethod method = AllocationMethod.Automatic,
        long? approvalId = null,
        string? notes = null)
    {
        if (allocatedAmount <= 0)
            throw new ArgumentException("Dağıtım tutarı sıfırdan büyük olmalıdır.");

        return new PaymentAllocation
        {
            InstitutionPaymentId = institutionPaymentId,
            TreatmentPlanItemId  = treatmentPlanItemId,
            BranchId             = branchId,
            Source               = AllocationSource.Institution,
            Method               = method,
            AllocatedAmount      = allocatedAmount,
            ApprovalId           = approvalId,
            AllocatedByUserId    = allocatedByUserId,
            Notes                = notes,
            IsRefunded           = false,
            CreatedAt            = DateTime.UtcNow
        };
    }

    /// <summary>Geriye uyumluluk için eski imza — hasta ödemesi otomatik dağıtım.</summary>
    public static PaymentAllocation Create(
        long paymentId,
        long treatmentPlanItemId,
        decimal allocatedAmount)
        => CreateFromPatient(paymentId, treatmentPlanItemId, branchId: 0, allocatedAmount, allocatedByUserId: 0);

    public void MarkRefunded() => IsRefunded = true;
}
