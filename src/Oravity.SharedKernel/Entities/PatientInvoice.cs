using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum PatientInvoiceStatus
{
    Issued        = 1, // Kesildi
    Paid          = 2, // Ödendi
    PartiallyPaid = 3, // Kısmi ödendi
    Cancelled     = 4  // İptal
}

public enum InvoiceRecipientType
{
    IndividualTc = 1, // Bireysel — TC kimlik no
    CompanyVkn   = 2  // Kurumsal — VKN
}

/// <summary>
/// Hastaya kesilen fatura (e-Arşiv veya e-Fatura).
/// Alıcı TC kimlik no (bireysel) veya VKN (hastalıya ait şirket) olabilir.
/// </summary>
public class PatientInvoice : AuditableEntity
{
    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public string InvoiceNo { get; private set; } = default!;
    /// <summary>EARCHIVE veya EINVOICE</summary>
    public string InvoiceType { get; private set; } = "EARCHIVE";
    public DateOnly InvoiceDate { get; private set; }
    public DateOnly DueDate { get; private set; }

    /// <summary>Matrah (KDV hariç)</summary>
    public decimal Amount { get; private set; }
    public decimal KdvRate { get; private set; } = 0.10m;
    public decimal KdvAmount { get; private set; }
    /// <summary>Matrah + KDV</summary>
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "TRY";

    public PatientInvoiceStatus Status { get; private set; } = PatientInvoiceStatus.Issued;
    public decimal PaidAmount { get; private set; }

    // ── Alıcı bilgileri ──────────────────────────────────────────────────────
    public InvoiceRecipientType RecipientType { get; private set; }
    public string RecipientName { get; private set; } = default!;
    /// <summary>Bireysel fatura için TC kimlik numarası (11 hane).</summary>
    public string? RecipientTcNo { get; private set; }
    /// <summary>Kurumsal fatura için vergi kimlik numarası (10 hane).</summary>
    public string? RecipientVkn { get; private set; }
    /// <summary>Kurumsal fatura için vergi dairesi adı.</summary>
    public string? RecipientTaxOffice { get; private set; }

    /// <summary>JSON array — ilişkili TreatmentPlanItem ID'leri.</summary>
    public string? TreatmentItemIdsJson { get; private set; }

    public string? Notes { get; private set; }

    // ── Entegratör alanları ──────────────────────────────────────────────────
    public string? ExternalUuid { get; private set; }
    public string? IntegratorStatus { get; private set; }

    private PatientInvoice() { }

    public static PatientInvoice Create(
        long patientId,
        long branchId,
        string invoiceNo,
        string invoiceType,
        DateOnly invoiceDate,
        DateOnly dueDate,
        decimal amount,
        decimal kdvRate,
        string currency,
        InvoiceRecipientType recipientType,
        string recipientName,
        string? recipientTcNo,
        string? recipientVkn,
        string? recipientTaxOffice,
        string? treatmentItemIdsJson,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(invoiceNo))
            throw new ArgumentException("Fatura numarası boş olamaz.", nameof(invoiceNo));
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Fatura tutarı sıfırdan büyük olmalıdır.");
        if (dueDate < invoiceDate)
            throw new ArgumentException("Vade tarihi fatura tarihinden önce olamaz.");
        if (string.IsNullOrWhiteSpace(recipientName))
            throw new ArgumentException("Alıcı adı boş olamaz.", nameof(recipientName));
        if (recipientType == InvoiceRecipientType.IndividualTc && string.IsNullOrWhiteSpace(recipientTcNo))
            throw new ArgumentException("Bireysel fatura için TC kimlik no gereklidir.", nameof(recipientTcNo));
        if (recipientType == InvoiceRecipientType.CompanyVkn && string.IsNullOrWhiteSpace(recipientVkn))
            throw new ArgumentException("Kurumsal fatura için VKN gereklidir.", nameof(recipientVkn));

        // amount = KDV dahil brüt tutar (sistemde fiyatlar KDV dahil girilir)
        var matrah = Math.Round(amount / (1 + kdvRate), 2);
        var kdvAmount = Math.Round(amount - matrah, 2);

        return new PatientInvoice
        {
            PatientId           = patientId,
            BranchId            = branchId,
            InvoiceNo           = invoiceNo.Trim(),
            InvoiceType         = invoiceType.ToUpperInvariant(),
            InvoiceDate         = invoiceDate,
            DueDate             = dueDate,
            Amount              = matrah,
            KdvRate             = kdvRate,
            KdvAmount           = kdvAmount,
            TotalAmount         = amount,
            Currency            = currency,
            Status              = PatientInvoiceStatus.Issued,
            PaidAmount          = 0,
            RecipientType       = recipientType,
            RecipientName       = recipientName.Trim(),
            RecipientTcNo       = recipientTcNo?.Trim(),
            RecipientVkn        = recipientVkn?.Trim(),
            RecipientTaxOffice  = recipientTaxOffice?.Trim(),
            TreatmentItemIdsJson = treatmentItemIdsJson,
            Notes               = notes
        };
    }

    public void RegisterPayment(decimal paidNow)
    {
        if (paidNow <= 0) throw new ArgumentOutOfRangeException(nameof(paidNow));

        PaidAmount += paidNow;
        Status = PaidAmount >= TotalAmount
            ? PatientInvoiceStatus.Paid
            : PatientInvoiceStatus.PartiallyPaid;
        MarkUpdated();
    }

    public void Cancel(string? reason)
    {
        if (Status == PatientInvoiceStatus.Paid)
            throw new InvalidOperationException("Ödenmiş fatura iptal edilemez.");
        Status = PatientInvoiceStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
            Notes = string.IsNullOrWhiteSpace(Notes) ? reason : $"{Notes}\n[İPTAL] {reason}";
        MarkUpdated();
    }

    public void SetExternalInvoiceData(string uuid, string status = "SENT")
    {
        ExternalUuid     = uuid;
        IntegratorStatus = status;
        MarkUpdated();
    }

    public void UpdateIntegratorStatus(string status)
    {
        IntegratorStatus = status;
        MarkUpdated();
    }
}
