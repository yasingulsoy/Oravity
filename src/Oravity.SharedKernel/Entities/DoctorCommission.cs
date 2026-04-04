namespace Oravity.SharedKernel.Entities;

public enum CommissionStatus
{
    Pending     = 1,  // Bekliyor
    Distributed = 2,  // Dağıtıldı
    Cancelled   = 3   // İptal
}

/// <summary>
/// Hekim hakediş kaydı.
/// Tamamlanan her TreatmentPlanItem için oluşturulur.
/// commission_amount = gross_amount × (commission_rate / 100)
/// </summary>
public class DoctorCommission
{
    public long Id { get; private set; }

    public long DoctorId { get; private set; }
    public User Doctor { get; private set; } = default!;

    public long TreatmentPlanItemId { get; private set; }
    public TreatmentPlanItem TreatmentPlanItem { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    /// <summary>Brüt tedavi tutarı (kesintiler öncesi).</summary>
    public decimal GrossAmount { get; private set; }
    /// <summary>Hakediş para birimi (tedavi plan kalemiyle aynı).</summary>
    public string Currency { get; private set; } = "TRY";
    /// <summary>Hesaplama anındaki döviz kuru. TRY hakedişte 1.</summary>
    public decimal ExchangeRate { get; private set; } = 1m;
    /// <summary>TRY bazında brüt tutar = GrossAmount × ExchangeRate.</summary>
    public decimal BaseAmount { get; private set; }
    /// <summary>Hakediş oranı (0–100).</summary>
    public decimal CommissionRate { get; private set; }
    /// <summary>Hesaplanan hakediş = GrossAmount × (CommissionRate / 100)</summary>
    public decimal CommissionAmount { get; private set; }

    public CommissionStatus Status { get; private set; } = CommissionStatus.Pending;
    public DateTime? DistributedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private DoctorCommission() { }

    public static DoctorCommission Create(
        long doctorId,
        long treatmentPlanItemId,
        long branchId,
        decimal grossAmount,
        decimal commissionRate,
        string currency = "TRY",
        decimal exchangeRate = 1m)
    {
        if (commissionRate < 0 || commissionRate > 100)
            throw new ArgumentOutOfRangeException(nameof(commissionRate), "Oran 0–100 arasında olmalıdır.");
        if (exchangeRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(exchangeRate), "Döviz kuru sıfırdan büyük olmalıdır.");

        var baseAmount       = currency == "TRY" ? grossAmount : Math.Round(grossAmount * exchangeRate, 4);
        var commissionAmount = Math.Round(grossAmount * commissionRate / 100, 2);

        return new DoctorCommission
        {
            DoctorId            = doctorId,
            TreatmentPlanItemId = treatmentPlanItemId,
            BranchId            = branchId,
            GrossAmount         = grossAmount,
            Currency            = currency,
            ExchangeRate        = currency == "TRY" ? 1m : exchangeRate,
            BaseAmount          = baseAmount,
            CommissionRate      = commissionRate,
            CommissionAmount    = commissionAmount,
            Status              = CommissionStatus.Pending,
            CreatedAt           = DateTime.UtcNow
        };
    }

    public void Distribute()
    {
        if (Status == CommissionStatus.Distributed)
            throw new InvalidOperationException("Bu hakediş zaten dağıtılmış.");
        if (Status == CommissionStatus.Cancelled)
            throw new InvalidOperationException("İptal edilmiş hakediş dağıtılamaz.");

        Status        = CommissionStatus.Distributed;
        DistributedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == CommissionStatus.Distributed)
            throw new InvalidOperationException("Dağıtılmış hakediş iptal edilemez.");

        Status = CommissionStatus.Cancelled;
    }
}
