namespace Oravity.SharedKernel.Entities;

public enum EInvoiceType
{
    EArchive  = 1,  // E-Arşiv (hastaya bireysel)
    EInvoice  = 2,  // E-Fatura (GİB mükellefi kuruma)
    ESMM      = 3   // Serbest Meslek Makbuzu
}

public enum EInvoiceReceiverType
{
    Individual = 1, // Gerçek kişi (hasta)
    Corporate  = 2  // Tüzel kişi (kurum)
}

/// <summary>
/// E-fatura / e-arşiv kaydı (SPEC §E-FATURA §2).
/// GİB entegrasyonu XmlExportAdapter ile başlar;
/// PARASUT/LOGO gibi gerçek entegratörler ilerleyen aşamada eklenir.
/// </summary>
public class EInvoice
{
    public long Id { get; private set; }
    public Guid PublicId { get; private set; }

    public long CompanyId { get; private set; }
    public long BranchId { get; private set; }

    public EInvoiceType InvoiceType { get; private set; }

    // Kaynak ödeme
    public long? PaymentId { get; private set; }
    public Payment? Payment { get; private set; }

    // Fatura numarası: GBS2026000001234
    public string? EInvoiceNo { get; private set; }
    public string Series { get; private set; } = "GBS";
    public int? Sequence { get; private set; }

    // Alıcı bilgisi
    public EInvoiceReceiverType ReceiverType { get; private set; }
    public string ReceiverName { get; private set; } = default!;

    /// <summary>Gerçek kişi — TC Kimlik No (şifreli saklanmaz, fatura belgesidir)</summary>
    public string? ReceiverTc { get; private set; }
    /// <summary>Tüzel kişi — Vergi Kimlik No</summary>
    public string? ReceiverVkn { get; private set; }
    public string? ReceiverTaxOffice { get; private set; }
    public string? ReceiverAddress { get; private set; }
    public string? ReceiverEmail { get; private set; }

    // Tutar
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxableAmount { get; private set; }
    /// <summary>KDV oranı — sağlık hizmetleri %10 (varsayılan)</summary>
    public decimal TaxRate { get; private set; } = 10m;
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public string LanguageCode { get; private set; } = "tr";

    // GİB entegrasyonu
    public string? GibUuid { get; private set; }
    public string? GibStatus { get; private set; }   // WAITING | ACCEPTED | REJECTED
    public string? GibResponse { get; private set; } // JSONB
    public DateTime? SentToGibAt { get; private set; }

    // PDF
    public string? PdfPath { get; private set; }

    // E-posta gönderim
    public DateTime? SentToReceiverAt { get; private set; }

    // İptal
    public bool IsCancelled { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelReason { get; private set; }

    public DateOnly InvoiceDate { get; private set; }

    public long CreatedBy { get; private set; }
    public User Creator { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public ICollection<EInvoiceItem> Items { get; private set; } = [];

    private EInvoice() { }

    public static EInvoice Create(
        long companyId,
        long branchId,
        EInvoiceType invoiceType,
        EInvoiceReceiverType receiverType,
        string receiverName,
        decimal subtotal,
        decimal taxRate,
        string series,
        int sequence,
        long createdBy,
        long? paymentId = null,
        string? receiverTc = null,
        string? receiverVkn = null,
        string? receiverTaxOffice = null,
        string? receiverAddress = null,
        string? receiverEmail = null,
        decimal discountAmount = 0,
        string currency = "TRY",
        string languageCode = "tr",
        DateOnly? invoiceDate = null)
    {
        var taxableAmount = subtotal - discountAmount;
        var taxAmount     = Math.Round(taxableAmount * (taxRate / 100m), 2);
        var total         = taxableAmount + taxAmount;

        var now = DateTime.UtcNow;
        var year = now.Year;

        return new EInvoice
        {
            PublicId          = Guid.NewGuid(),
            CompanyId         = companyId,
            BranchId          = branchId,
            InvoiceType       = invoiceType,
            PaymentId         = paymentId,
            Series            = series.ToUpperInvariant(),
            Sequence          = sequence,
            EInvoiceNo        = $"{series.ToUpperInvariant()}{year}{sequence:D9}",
            ReceiverType      = receiverType,
            ReceiverName      = receiverName,
            ReceiverTc        = receiverTc,
            ReceiverVkn       = receiverVkn,
            ReceiverTaxOffice = receiverTaxOffice,
            ReceiverAddress   = receiverAddress,
            ReceiverEmail     = receiverEmail,
            Subtotal          = subtotal,
            DiscountAmount    = discountAmount,
            TaxableAmount     = taxableAmount,
            TaxRate           = taxRate,
            TaxAmount         = taxAmount,
            Total             = total,
            Currency          = currency,
            LanguageCode      = languageCode,
            InvoiceDate       = invoiceDate ?? DateOnly.FromDateTime(DateTime.Today),
            CreatedBy         = createdBy,
            CreatedAt         = now
        };
    }

    public void MarkSentToGib(string gibUuid, string gibStatus, string? gibResponseJson = null)
    {
        GibUuid      = gibUuid;
        GibStatus    = gibStatus;
        GibResponse  = gibResponseJson;
        SentToGibAt  = DateTime.UtcNow;
    }

    public void UpdateGibStatus(string gibStatus, string? gibResponseJson = null)
    {
        GibStatus   = gibStatus;
        GibResponse = gibResponseJson;
    }

    public void SetPdfPath(string pdfPath) => PdfPath = pdfPath;

    public void MarkSentToReceiver() => SentToReceiverAt = DateTime.UtcNow;

    public void Cancel(string reason)
    {
        if (IsCancelled)
            throw new InvalidOperationException("Bu fatura zaten iptal edilmiş.");

        IsCancelled  = true;
        CancelledAt  = DateTime.UtcNow;
        CancelReason = reason;
    }
}
