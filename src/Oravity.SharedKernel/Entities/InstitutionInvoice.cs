using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum InstitutionInvoiceStatus
{
    Issued        = 1, // Fatura kesildi, ödeme bekliyor
    Paid          = 2, // Tam ödendi
    PartiallyPaid = 3, // Kısmi ödendi
    Rejected      = 4, // Kurum ödemeyi reddetti
    Overdue       = 5, // Vade geçti
    InFollowUp    = 6, // Yasal takip
    Cancelled     = 7  // İptal edildi (kliniğin inisiyatifi)
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

    /// <summary>Matrah (KDV hariç hizmet bedeli)</summary>
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";

    // KDV & Tevkifat — fatura kesilirken kurumdan kopyalanır, sonradan değişmez
    public decimal KdvRate { get; private set; } = 0.20m;
    public decimal KdvAmount { get; private set; }
    public bool WithholdingApplies { get; private set; } = false;
    public string? WithholdingCode { get; private set; }
    public int WithholdingNumerator { get; private set; } = 5;
    public int WithholdingDenominator { get; private set; } = 10;
    /// <summary>KDV'nin tevkifata düşen kısmı — kurum vergi dairesine öder.</summary>
    public decimal WithholdingAmount { get; private set; }
    /// <summary>Kurumun kliniğe ödeyeceği net tutar = Matrah + KDV − Tevkifat</summary>
    public decimal NetPayableAmount { get; private set; }

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

    // ── E-Fatura / Entegratör alanları ───────────────────────────────────────
    /// <summary>
    /// Entegratörden dönen GIB UUID'si (e-fatura / e-arşiv).
    /// Yerel modda null.
    /// </summary>
    public string? ExternalUuid { get; private set; }

    /// <summary>
    /// Entegratör gönderim durumu: null (gönderilmedi), "SENT", "ACCEPTED", "ERROR".
    /// Yerel modda null.
    /// </summary>
    public string? IntegratorStatus { get; private set; }

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
        string? notes,
        decimal kdvRate = 0.20m,
        bool withholdingApplies = false,
        string? withholdingCode = null,
        int withholdingNumerator = 5,
        int withholdingDenominator = 10)
    {
        if (string.IsNullOrWhiteSpace(invoiceNo))
            throw new ArgumentException("Fatura numarası boş olamaz.", nameof(invoiceNo));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (dueDate < invoiceDate) throw new ArgumentException("Vade faturadan önce olamaz.");

        // amount = KDV dahil brüt tutar (sistemde fiyatlar KDV dahil girilir)
        var matrah = Math.Round(amount / (1 + kdvRate), 2);
        var kdvAmount = Math.Round(amount - matrah, 2);
        var withholdingAmount = withholdingApplies && withholdingDenominator > 0
            ? Math.Round(kdvAmount * withholdingNumerator / withholdingDenominator, 2)
            : 0m;
        var netPayable = amount - withholdingAmount;

        return new InstitutionInvoice
        {
            PatientId              = patientId,
            InstitutionId          = institutionId,
            BranchId               = branchId,
            InvoiceNo              = invoiceNo.Trim(),
            InvoiceDate            = invoiceDate,
            DueDate                = dueDate,
            Amount                 = matrah,
            Currency               = currency,
            KdvRate                = kdvRate,
            KdvAmount              = kdvAmount,
            WithholdingApplies     = withholdingApplies,
            WithholdingCode        = withholdingCode?.Trim(),
            WithholdingNumerator   = withholdingNumerator,
            WithholdingDenominator = withholdingDenominator,
            WithholdingAmount      = withholdingAmount,
            NetPayableAmount       = netPayable,
            Status                 = InstitutionInvoiceStatus.Issued,
            PaidAmount             = 0,
            TreatmentItemIdsJson   = treatmentItemIdsJson,
            Notes                  = notes,
            FollowUpStatus         = InstitutionInvoiceFollowUp.None
        };
    }

    /// <summary>Entegratörden dönen UUID ve durumu kaydeder.</summary>
    public void SetExternalInvoiceData(string uuid, string status = "SENT")
    {
        ExternalUuid      = uuid;
        IntegratorStatus  = status;
        MarkUpdated();
    }

    public void UpdateIntegratorStatus(string status)
    {
        IntegratorStatus = status;
        MarkUpdated();
    }

    public void RegisterPayment(decimal paidNow, DateOnly paymentDate, string? referenceNo)
    {
        if (paidNow <= 0) throw new ArgumentOutOfRangeException(nameof(paidNow));

        PaidAmount        += paidNow;
        PaymentDate        = paymentDate;
        PaymentReferenceNo = referenceNo;

        if (PaidAmount >= NetPayableAmount - 0.005m)
            Status = InstitutionInvoiceStatus.Paid;
        else
            Status = InstitutionInvoiceStatus.PartiallyPaid;
        MarkUpdated();
    }

    public void Cancel(string reason)
    {
        if (Status is InstitutionInvoiceStatus.Paid)
            throw new InvalidOperationException("Ödenmiş fatura iptal edilemez.");
        if (Status is InstitutionInvoiceStatus.PartiallyPaid)
            throw new InvalidOperationException("Kısmi ödemesi alınmış fatura iptal edilemez. Önce ödemeleri iade edin.");
        if (Status is InstitutionInvoiceStatus.Cancelled)
            throw new InvalidOperationException("Fatura zaten iptal edilmiş.");

        Status = InstitutionInvoiceStatus.Cancelled;
        Notes  = string.IsNullOrWhiteSpace(Notes) ? $"[İPTAL] {reason}" : $"{Notes}\n[İPTAL] {reason}";
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
