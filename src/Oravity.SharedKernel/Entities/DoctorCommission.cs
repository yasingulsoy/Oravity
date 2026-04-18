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
/// Kesinti zinciri: Gross → deductions → NetBase × PrimRate → GrossCommission → KDV/Stopaj → NetCommission
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
    public string Currency { get; private set; } = "TRY";
    public decimal ExchangeRate { get; private set; } = 1m;
    /// <summary>TRY bazında brüt tutar = GrossAmount × ExchangeRate.</summary>
    public decimal BaseAmount { get; private set; }

    // ── Kesintiler ────────────────────────────────────────────────────────
    public decimal PosCommissionRate { get; private set; }
    public decimal PosCommissionAmount { get; private set; }
    public decimal LabCostDeducted { get; private set; }
    public decimal TreatmentCostDeducted { get; private set; }
    public decimal TreatmentPlanCommissionDeducted { get; private set; }
    public decimal ExtraExpenseRate { get; private set; }
    public decimal ExtraExpenseAmount { get; private set; }
    /// <summary>Tüm kesintilerden sonra prim hesaplanacak net baz tutar.</summary>
    public decimal NetBaseAmount { get; private set; }

    // ── Prim ──────────────────────────────────────────────────────────────
    /// <summary>Hakediş oranı (0–100). Hedef bonusu uygulanmışsa bonus oran.</summary>
    public decimal CommissionRate { get; private set; }
    public bool BonusApplied { get; private set; }
    public decimal FixedFee { get; private set; }
    /// <summary>Prim tutarı = FixedFee + NetBaseAmount × CommissionRate/100</summary>
    public decimal CommissionAmount { get; private set; }

    // ── Vergi ─────────────────────────────────────────────────────────────
    public decimal KdvRate { get; private set; }
    public decimal KdvAmount { get; private set; }
    public decimal WithholdingTaxRate { get; private set; }
    public decimal WithholdingTaxAmount { get; private set; }

    /// <summary>Ödenecek net hakediş = CommissionAmount + KDV − Stopaj</summary>
    public decimal NetCommissionAmount { get; private set; }

    /// <summary>Hesaplamada kullanılan şablon (history).</summary>
    public long? TemplateId { get; private set; }

    // ── Period / Group ────────────────────────────────────────────────────
    /// <summary>Hakediş periyodu yılı (ör. 2026).</summary>
    public int PeriodYear { get; private set; }
    /// <summary>Hakediş periyodu ayı (1–12).</summary>
    public int PeriodMonth { get; private set; }

    public CommissionStatus Status { get; private set; } = CommissionStatus.Pending;
    public DateTime? DistributedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private DoctorCommission() { }

    /// <summary>Geriye uyumluluk için basit factory — eski kod yolu (AppointmentCompletedEventHandler).
    /// NetCommissionAmount = CommissionAmount, kesinti yok.</summary>
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
            throw new ArgumentOutOfRangeException(nameof(commissionRate));
        if (exchangeRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(exchangeRate));

        var baseAmount       = currency == "TRY" ? grossAmount : Math.Round(grossAmount * exchangeRate, 4);
        var commissionAmount = Math.Round(grossAmount * commissionRate / 100, 2);
        var now              = DateTime.UtcNow;

        return new DoctorCommission
        {
            DoctorId              = doctorId,
            TreatmentPlanItemId   = treatmentPlanItemId,
            BranchId              = branchId,
            GrossAmount           = grossAmount,
            Currency              = currency,
            ExchangeRate          = currency == "TRY" ? 1m : exchangeRate,
            BaseAmount            = baseAmount,
            NetBaseAmount         = grossAmount,
            CommissionRate        = commissionRate,
            CommissionAmount      = commissionAmount,
            NetCommissionAmount   = commissionAmount,
            Status                = CommissionStatus.Pending,
            PeriodYear            = now.Year,
            PeriodMonth           = now.Month,
            CreatedAt             = now
        };
    }

    /// <summary>Hakediş hesaplama sonucundan oluşturma.</summary>
    public static DoctorCommission CreateCalculated(CommissionCalculation calc)
    {
        var baseAmount = calc.Currency == "TRY" ? calc.GrossAmount : Math.Round(calc.GrossAmount * calc.ExchangeRate, 4);

        return new DoctorCommission
        {
            DoctorId                         = calc.DoctorId,
            TreatmentPlanItemId              = calc.TreatmentPlanItemId,
            BranchId                         = calc.BranchId,
            GrossAmount                      = calc.GrossAmount,
            Currency                         = calc.Currency,
            ExchangeRate                     = calc.ExchangeRate,
            BaseAmount                       = baseAmount,

            PosCommissionRate                = calc.PosCommissionRate,
            PosCommissionAmount              = calc.PosCommissionAmount,
            LabCostDeducted                  = calc.LabCostDeducted,
            TreatmentCostDeducted            = calc.TreatmentCostDeducted,
            TreatmentPlanCommissionDeducted  = calc.TreatmentPlanCommissionDeducted,
            ExtraExpenseRate                 = calc.ExtraExpenseRate,
            ExtraExpenseAmount               = calc.ExtraExpenseAmount,
            NetBaseAmount                    = calc.NetBaseAmount,

            CommissionRate                   = calc.AppliedPrimRate,
            BonusApplied                     = calc.BonusApplied,
            FixedFee                         = calc.FixedFee,
            CommissionAmount                 = calc.GrossCommission,

            KdvRate                          = calc.KdvRate,
            KdvAmount                        = calc.KdvAmount,
            WithholdingTaxRate               = calc.WithholdingTaxRate,
            WithholdingTaxAmount             = calc.WithholdingTaxAmount,

            NetCommissionAmount              = calc.NetCommissionAmount,
            TemplateId                       = calc.TemplateId,

            PeriodYear                       = calc.PeriodYear,
            PeriodMonth                      = calc.PeriodMonth,
            Status                           = CommissionStatus.Pending,
            CreatedAt                        = DateTime.UtcNow,
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

/// <summary>Hakediş hesaplama sonucu — servis tarafından doldurulur.</summary>
public sealed class CommissionCalculation
{
    public long DoctorId { get; init; }
    public long TreatmentPlanItemId { get; init; }
    public long BranchId { get; init; }
    public long? TemplateId { get; init; }
    public int PeriodYear { get; init; }
    public int PeriodMonth { get; init; }

    public decimal GrossAmount { get; init; }
    public string Currency { get; init; } = "TRY";
    public decimal ExchangeRate { get; init; } = 1m;

    public decimal PosCommissionRate { get; init; }
    public decimal PosCommissionAmount { get; init; }
    public decimal LabCostDeducted { get; init; }
    public decimal TreatmentCostDeducted { get; init; }
    public decimal TreatmentPlanCommissionDeducted { get; init; }
    public decimal ExtraExpenseRate { get; init; }
    public decimal ExtraExpenseAmount { get; init; }
    public decimal NetBaseAmount { get; init; }

    public decimal AppliedPrimRate { get; init; }
    public bool BonusApplied { get; init; }
    public decimal FixedFee { get; init; }
    public decimal GrossCommission { get; init; }

    public decimal KdvRate { get; init; }
    public decimal KdvAmount { get; init; }
    public decimal WithholdingTaxRate { get; init; }
    public decimal WithholdingTaxAmount { get; init; }

    public decimal NetCommissionAmount { get; init; }
}
