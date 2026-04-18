using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum AllocationApprovalStatus
{
    Pending   = 1, // Onay bekliyor
    Approved  = 2, // Onaylandı
    Rejected  = 3, // Reddedildi
    Cancelled = 4  // İptal edildi
}

public enum AllocationSource
{
    Patient     = 1,
    Institution = 2
}

/// <summary>
/// Manuel dağıtım onay talebi. Klinik Müdürü onayı bekleyen dağıtım isteği.
/// Onaylandığında PaymentAllocation kaydı oluşturulur ve ApprovalId bağlanır.
/// </summary>
public class AllocationApproval : AuditableEntity
{
    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    public long BranchId { get; private set; }

    public long TreatmentPlanItemId { get; private set; }
    public TreatmentPlanItem TreatmentPlanItem { get; private set; } = default!;

    /// <summary>Hangi ödeme kaydı dağıtılacak. Hasta kaynağı ise Payment, kurum ise InstitutionPayment olur.</summary>
    public long? PaymentId { get; private set; }
    public long? InstitutionPaymentId { get; private set; }

    public AllocationSource Source { get; private set; }
    public decimal RequestedAmount { get; private set; }

    public AllocationApprovalStatus Status { get; private set; } = AllocationApprovalStatus.Pending;

    public long RequestedByUserId { get; private set; }
    public string? RequestNotes { get; private set; }

    public long? ApprovedByUserId { get; private set; }
    public DateTime? ApprovalDate { get; private set; }
    public string? ApprovalNotes { get; private set; }
    public string? RejectionReason { get; private set; }

    /// <summary>Onaylandığında oluşturulan PaymentAllocation kaydı.</summary>
    public long? PaymentAllocationId { get; private set; }

    private AllocationApproval() { }

    public static AllocationApproval Create(
        long patientId,
        long branchId,
        long treatmentPlanItemId,
        AllocationSource source,
        long? paymentId,
        long? institutionPaymentId,
        decimal requestedAmount,
        long requestedByUserId,
        string? notes)
    {
        if (requestedAmount <= 0) throw new ArgumentException("Tutar sıfırdan büyük olmalı.");
        if (source == AllocationSource.Patient && paymentId is null)
            throw new ArgumentException("Hasta kaynaklı dağıtımda paymentId zorunlu.");
        if (source == AllocationSource.Institution && institutionPaymentId is null)
            throw new ArgumentException("Kurum kaynaklı dağıtımda institutionPaymentId zorunlu.");

        return new AllocationApproval
        {
            PatientId            = patientId,
            BranchId             = branchId,
            TreatmentPlanItemId  = treatmentPlanItemId,
            PaymentId            = paymentId,
            InstitutionPaymentId = institutionPaymentId,
            Source               = source,
            RequestedAmount      = requestedAmount,
            RequestedByUserId    = requestedByUserId,
            RequestNotes         = notes,
            Status               = AllocationApprovalStatus.Pending,
        };
    }

    public void Approve(long approvedBy, string? notes, long paymentAllocationId)
    {
        if (Status != AllocationApprovalStatus.Pending)
            throw new InvalidOperationException("Bu talep zaten sonuçlanmış.");

        Status              = AllocationApprovalStatus.Approved;
        ApprovedByUserId    = approvedBy;
        ApprovalDate        = DateTime.UtcNow;
        ApprovalNotes       = notes;
        PaymentAllocationId = paymentAllocationId;
        MarkUpdated();
    }

    public void Reject(long approvedBy, string reason)
    {
        if (Status != AllocationApprovalStatus.Pending)
            throw new InvalidOperationException("Bu talep zaten sonuçlanmış.");

        Status           = AllocationApprovalStatus.Rejected;
        ApprovedByUserId = approvedBy;
        ApprovalDate     = DateTime.UtcNow;
        RejectionReason  = reason;
        MarkUpdated();
    }

    public void Cancel()
    {
        if (Status != AllocationApprovalStatus.Pending)
            throw new InvalidOperationException("Yalnızca bekleyen talepler iptal edilebilir.");

        Status = AllocationApprovalStatus.Cancelled;
        MarkUpdated();
    }
}
