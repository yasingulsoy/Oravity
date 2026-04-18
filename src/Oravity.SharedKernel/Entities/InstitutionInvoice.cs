using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum InstitutionInvoiceStatus
{
    Issued        = 1, // Fatura kesildi, ödeme bekliyor
    Paid          = 2, // Tam ödendi
    PartiallyPaid = 3, // Kısmi ödendi
    Rejected      = 4, // Kurum ödemeyi reddetti
    Overdue       = 5, // Vade geçti
    InFollowUp    = 6  // Yasal takip
}

public enum InstitutionInvoiceFollowUp
{
    None            = 1,
    FirstReminder   = 2,
    SecondReminder  = 3,
    Legal           = 4
}

/// <summary>
/// Anlaşmalı kuruma kesilen fatura. Hasta × Kurum × Şube bazında tutulur.
/// İlişkili tedavi kalemleri TreatmentItemIdsJson alanında saklanır.
/// </summary>
public class InstitutionInvoice : AuditableEntity
{
    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    public long InstitutionId { get; private set; }
    public Institution Institution { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public string InvoiceNo { get; private set; } = default!;
    public DateOnly InvoiceDate { get; private set; }
    public DateOnly DueDate { get; private set; }

    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";

    public InstitutionInvoiceStatus Status { get; private set; } = InstitutionInvoiceStatus.Issued;

    public decimal PaidAmount { get; private set; }
    public DateOnly? PaymentDate { get; private set; }
    public string? PaymentReferenceNo { get; private set; }

    /// <summary>JSON array — ilişkili TreatmentPlanItem ID'leri. Örn: "[123, 456]".</summary>
    public string? TreatmentItemIdsJson { get; private set; }

    public InstitutionInvoiceFollowUp FollowUpStatus { get; private set; } = InstitutionInvoiceFollowUp.None;
    public DateOnly? LastFollowUpDate { get; private set; }
    public DateOnly? NextFollowUpDate { get; private set; }

    public string? Notes { get; private set; }

    public ICollection<InstitutionPayment> Payments { get; private set; } = [];

    private InstitutionInvoice() { }

    public static InstitutionInvoice Create(
        long patientId,
        long institutionId,
        long branchId,
        string invoiceNo,
        DateOnly invoiceDate,
        DateOnly dueDate,
        decimal amount,
        string currency,
        string? treatmentItemIdsJson,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(invoiceNo))
            throw new ArgumentException("Fatura numarası boş olamaz.", nameof(invoiceNo));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (dueDate < invoiceDate) throw new ArgumentException("Vade faturadan önce olamaz.");

        return new InstitutionInvoice
        {
            PatientId            = patientId,
            InstitutionId        = institutionId,
            BranchId             = branchId,
            InvoiceNo            = invoiceNo.Trim(),
            InvoiceDate          = invoiceDate,
            DueDate              = dueDate,
            Amount               = amount,
            Currency             = currency,
            Status               = InstitutionInvoiceStatus.Issued,
            PaidAmount           = 0,
            TreatmentItemIdsJson = treatmentItemIdsJson,
            Notes                = notes,
            FollowUpStatus       = InstitutionInvoiceFollowUp.None
        };
    }

    public void RegisterPayment(decimal paidNow, DateOnly paymentDate, string? referenceNo)
    {
        if (paidNow <= 0) throw new ArgumentOutOfRangeException(nameof(paidNow));

        PaidAmount        += paidNow;
        PaymentDate        = paymentDate;
        PaymentReferenceNo = referenceNo;

        if (PaidAmount >= Amount)
            Status = InstitutionInvoiceStatus.Paid;
        else
            Status = InstitutionInvoiceStatus.PartiallyPaid;
        MarkUpdated();
    }

    public void MarkRejected(string reason)
    {
        Status       = InstitutionInvoiceStatus.Rejected;
        Notes        = string.IsNullOrWhiteSpace(Notes) ? reason : $"{Notes}\n[RED] {reason}";
        MarkUpdated();
    }

    public void MarkOverdue()
    {
        if (Status == InstitutionInvoiceStatus.Paid) return;
        Status = InstitutionInvoiceStatus.Overdue;
        MarkUpdated();
    }

    public void StartFollowUp(InstitutionInvoiceFollowUp level, DateOnly onDate, DateOnly? nextDate)
    {
        FollowUpStatus    = level;
        LastFollowUpDate  = onDate;
        NextFollowUpDate  = nextDate;
        if (level == InstitutionInvoiceFollowUp.Legal)
            Status = InstitutionInvoiceStatus.InFollowUp;
        MarkUpdated();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        MarkUpdated();
    }
}
