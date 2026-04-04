using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum PaymentMethod
{
    Cash        = 1,  // Nakit
    CreditCard  = 2,  // Kredi Kartı
    BankTransfer = 3, // Havale/EFT
    Installment = 4,  // Taksit
    Check       = 5   // Çek
}

/// <summary>
/// Hasta ödemesi. Ödeme iade edildiğinde IsRefunded=true, IsDeleted=true olur.
/// Dağıtım: PaymentAllocation tablosunda kalem bazında izlenir.
/// </summary>
public class Payment : AuditableEntity
{
    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";

    /// <summary>İşlem anındaki döviz kuru (Currency != TRY ise). TRY ödemede 1.</summary>
    public decimal ExchangeRate { get; private set; } = 1m;
    /// <summary>TRY karşılığı = Amount × ExchangeRate. TRY ödemede Amount ile aynı.</summary>
    public decimal BaseAmount { get; private set; }

    public PaymentMethod Method { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>true = iade edildi; ödeme muhasebe kaydında kalır.</summary>
    public bool IsRefunded { get; private set; }

    public ICollection<PaymentAllocation> Allocations { get; private set; } = [];

    private Payment() { }

    public static Payment Create(
        long patientId,
        long branchId,
        decimal amount,
        PaymentMethod method,
        DateOnly paymentDate,
        string currency = "TRY",
        decimal exchangeRate = 1m,
        string? notes = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Ödeme tutarı sıfırdan büyük olmalıdır.", nameof(amount));
        if (exchangeRate <= 0)
            throw new ArgumentException("Döviz kuru sıfırdan büyük olmalıdır.", nameof(exchangeRate));

        var baseAmount = currency == "TRY" ? amount : Math.Round(amount * exchangeRate, 4);

        return new Payment
        {
            PatientId    = patientId,
            BranchId     = branchId,
            Amount       = amount,
            Currency     = currency,
            ExchangeRate = currency == "TRY" ? 1m : exchangeRate,
            BaseAmount   = baseAmount,
            Method       = method,
            PaymentDate  = paymentDate,
            Notes        = notes,
            IsRefunded   = false
        };
    }

    public void Refund()
    {
        if (IsRefunded)
            throw new InvalidOperationException("Bu ödeme zaten iade edilmiş.");

        IsRefunded = true;
        MarkUpdated();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        MarkUpdated();
    }
}
