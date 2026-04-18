using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum InstitutionPaymentMethod
{
    BankTransfer = 1, // Havale / EFT
    Check        = 2, // Çek
    Other        = 3
}

/// <summary>
/// Kurumun fatura karşılığında gönderdiği ödeme.
/// Birden fazla taksit/parçalı ödeme olabilir.
/// </summary>
public class InstitutionPayment : AuditableEntity
{
    public long InvoiceId { get; private set; }
    public InstitutionInvoice Invoice { get; private set; } = default!;

    public long PatientId { get; private set; }
    public long InstitutionId { get; private set; }

    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public DateOnly PaymentDate { get; private set; }
    public InstitutionPaymentMethod Method { get; private set; }
    public string? ReferenceNo { get; private set; }

    public string? Notes { get; private set; }
    public bool IsCancelled { get; private set; }

    private InstitutionPayment() { }

    public static InstitutionPayment Create(
        long invoiceId,
        long patientId,
        long institutionId,
        decimal amount,
        string currency,
        DateOnly paymentDate,
        InstitutionPaymentMethod method,
        string? referenceNo,
        string? notes)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

        return new InstitutionPayment
        {
            InvoiceId     = invoiceId,
            PatientId     = patientId,
            InstitutionId = institutionId,
            Amount        = amount,
            Currency      = currency,
            PaymentDate   = paymentDate,
            Method        = method,
            ReferenceNo   = referenceNo,
            Notes         = notes,
            IsCancelled   = false
        };
    }

    public void Cancel()
    {
        IsCancelled = true;
        MarkUpdated();
    }
}
