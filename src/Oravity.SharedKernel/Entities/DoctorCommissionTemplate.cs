using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>Şablonun çalışma şekli.</summary>
public enum CommissionWorkingStyle
{
    /// <summary>Tedavi yapılınca hakediş hesaplanır (ödeme beklenmez).</summary>
    Accrual    = 1,
    /// <summary>Ödeme alınınca hakediş hesaplanır.</summary>
    Collection = 2
}

/// <summary>Ödeme tipi — hakediş hesaplama yöntemi.</summary>
public enum CommissionPaymentType
{
    Fix                       = 1, // Sadece sabit ücret
    Prim                      = 2, // Sadece yüzdesel prim
    FixPlusPrim               = 3, // Sabit ücret + prim
    PerJob                    = 4, // Her tedavi başına ayrı hesap
    PerJobSelectedPlusFixPrim = 5, // Seçili tedaviler iş başı, geri kalan fix+prim
    PriceRange                = 6  // Fiyat bandına göre
}

/// <summary>İş başı hesaplama kaynağı.</summary>
public enum JobStartCalculation
{
    /// <summary>Sistemdeki fiyat listesinden okunur.</summary>
    FromPriceList = 1,
    /// <summary>Şablonda tedavi bazında özel tanımlanır.</summary>
    CustomPrices  = 2
}

/// <summary>İş başı özel fiyat tipi.</summary>
public enum JobStartPriceType
{
    FixedAmount = 1, // Sabit ücret
    Percentage  = 2  // Yüzde (%)
}

/// <summary>
/// Hekim hakediş şablonu. Birden fazla hekime atanabilir.
/// Şablon adı örnekleri: "%30", "Cerrah", "Kayseri %35", "Başhekim Tedavi %30".
/// </summary>
public class DoctorCommissionTemplate : AuditableEntity
{
    public long CompanyId { get; private set; }

    /// <summary>Şablon adı (şirket içinde benzersiz).</summary>
    public string Name { get; private set; } = default!;

    public CommissionWorkingStyle WorkingStyle { get; private set; } = CommissionWorkingStyle.Accrual;
    public CommissionPaymentType PaymentType { get; private set; } = CommissionPaymentType.Prim;
    public JobStartCalculation? JobStartCalculation { get; private set; }

    // Sabit ücret & prim
    public decimal FixedFee { get; private set; }
    public decimal PrimRate { get; private set; }

    // Hedef sistemi — tutarlar DoctorTargets / BranchTargets tablosunda
    public bool ClinicTargetEnabled { get; private set; }
    public decimal? ClinicTargetBonusRate { get; private set; }
    public bool DoctorTargetEnabled { get; private set; }
    public decimal? DoctorTargetBonusRate { get; private set; }

    /// <summary>Kurum ödemesi için hakediş zamanlaması.
    /// true = fatura kesilince hekim hakediş alır (riskli).
    /// false = kurum ödeyince hekim hakediş alır (güvenli).</summary>
    public bool InstitutionPayOnInvoice { get; private set; }

    // Kesintiler (bool toggle)
    public bool DeductTreatmentPlanCommission { get; private set; }
    public bool DeductLabCost { get; private set; }
    public bool DeductTreatmentCost { get; private set; }

    /// <summary>
    /// true (varsayılan): Tedaviye bağlı lab işi varsa <em>onaylanmış</em> olmalı.
    /// false: Lab onayı beklenmeden hakediş hesaplanabilir.
    /// </summary>
    public bool RequireLabApproval { get; private set; } = true;
    /// <summary>Her zaman açık (kararlaştırıldı).</summary>
    public bool DeductCreditCardCommission { get; private set; } = true;

    // KDV
    public bool KdvEnabled { get; private set; }
    public decimal? KdvRate { get; private set; }
    /// <summary>JSON array — KDV uygulanan ödeme tipi ID'leri. Örn: [1,2,3].</summary>
    public string? KdvAppliedPaymentTypes { get; private set; }

    // Ekstra gider
    public bool ExtraExpenseEnabled { get; private set; }
    public decimal? ExtraExpenseRate { get; private set; }

    // Stopaj
    public bool WithholdingTaxEnabled { get; private set; }
    public decimal? WithholdingTaxRate { get; private set; }

    public bool IsActive { get; private set; } = true;

    public ICollection<TemplateJobStartPrice> JobStartPrices { get; private set; } = [];
    public ICollection<TemplatePriceRange> PriceRanges { get; private set; } = [];

    private DoctorCommissionTemplate() { }

    public static DoctorCommissionTemplate Create(
        long companyId,
        string name,
        CommissionWorkingStyle workingStyle,
        CommissionPaymentType paymentType,
        decimal fixedFee,
        decimal primRate,
        bool institutionPayOnInvoice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Şablon adı boş olamaz.", nameof(name));
        if (fixedFee < 0) throw new ArgumentOutOfRangeException(nameof(fixedFee));
        if (primRate < 0 || primRate > 100) throw new ArgumentOutOfRangeException(nameof(primRate));

        return new DoctorCommissionTemplate
        {
            CompanyId               = companyId,
            Name                    = name.Trim(),
            WorkingStyle            = workingStyle,
            PaymentType             = paymentType,
            FixedFee                = fixedFee,
            PrimRate                = primRate,
            InstitutionPayOnInvoice = institutionPayOnInvoice,
            IsActive                = true,
        };
    }

    public void UpdateBasics(
        string name,
        CommissionWorkingStyle workingStyle,
        CommissionPaymentType paymentType,
        decimal fixedFee,
        decimal primRate,
        bool institutionPayOnInvoice,
        JobStartCalculation? jobStartCalculation)
    {
        Name                     = name.Trim();
        WorkingStyle             = workingStyle;
        PaymentType              = paymentType;
        FixedFee                 = fixedFee;
        PrimRate                 = primRate;
        InstitutionPayOnInvoice  = institutionPayOnInvoice;
        JobStartCalculation      = jobStartCalculation;
        MarkUpdated();
    }

    public void UpdateTargets(
        bool clinicEnabled, decimal? clinicBonus,
        bool doctorEnabled, decimal? doctorBonus)
    {
        ClinicTargetEnabled   = clinicEnabled;
        ClinicTargetBonusRate = clinicEnabled ? clinicBonus : null;
        DoctorTargetEnabled   = doctorEnabled;
        DoctorTargetBonusRate = doctorEnabled ? doctorBonus : null;
        MarkUpdated();
    }

    public void UpdateDeductions(
        bool planCommission,
        bool labCost,
        bool treatmentCost,
        bool requireLabApproval,
        bool kdvEnabled, decimal? kdvRate, string? kdvPaymentTypes,
        bool extraExpense, decimal? extraRate,
        bool withholding, decimal? withholdingRate)
    {
        DeductTreatmentPlanCommission = planCommission;
        DeductLabCost                 = labCost;
        DeductTreatmentCost           = treatmentCost;
        RequireLabApproval            = requireLabApproval;

        KdvEnabled             = kdvEnabled;
        KdvRate                = kdvEnabled ? kdvRate : null;
        KdvAppliedPaymentTypes = kdvEnabled ? kdvPaymentTypes : null;

        ExtraExpenseEnabled = extraExpense;
        ExtraExpenseRate    = extraExpense ? extraRate : null;

        WithholdingTaxEnabled = withholding;
        WithholdingTaxRate    = withholding ? withholdingRate : null;
        MarkUpdated();
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        MarkUpdated();
    }
}
