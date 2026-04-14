using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum TreatmentItemStatus
{
    Planned   = 1,  // Planlandı
    Approved  = 2,  // Onaylandı
    Completed = 3,  // Tamamlandı
    Cancelled = 4   // İptal
}

/// <summary>
/// Tedavi planı kalemi — tek bir tedavi işleminin plan içindeki satırı.
/// FDI diş numarası veya vücut bölgesi ile eşleşir.
/// Fiyat hesabı: final_price = unit_price * (1 − discount_rate / 100)
/// </summary>
public class TreatmentPlanItem : BaseEntity
{
    public long PlanId { get; private set; }
    public TreatmentPlan Plan { get; private set; } = default!;

    public long TreatmentId { get; private set; }
    public Treatment? Treatment { get; private set; }

    // ── Tedavi kapsamı ────────────────────────────────────────────────────
    /// <summary>FDI diş numarası (örn. "16", "21"). Diş dışı işlemlerde null.</summary>
    public string? ToothNumber { get; private set; }
    /// <summary>Yüzey kodu (örn. "MOD", "MO"). Null = tüm yüzeyler veya geçersiz.</summary>
    public string? ToothSurfaces { get; private set; }
    /// <summary>Vücut bölge kodu — diş dışı tedaviler için (örn. "SCALP", "LEFT_FOOT").</summary>
    public string? BodyRegionCode { get; private set; }

    // ── Durum ─────────────────────────────────────────────────────────────
    public TreatmentItemStatus Status { get; private set; } = TreatmentItemStatus.Planned;

    // ── Fiyat ─────────────────────────────────────────────────────────────
    public decimal UnitPrice { get; private set; }
    /// <summary>0–100 arası indirim yüzdesi.</summary>
    public decimal DiscountRate { get; private set; }
    /// <summary>Hesaplanan net fiyat (KDV hariç): UnitPrice * (1 − DiscountRate / 100)</summary>
    public decimal FinalPrice { get; private set; }
    /// <summary>Kalem oluşturulurken Treatment.KdvRate'den snapshot alınır.</summary>
    public decimal KdvRate { get; private set; }
    /// <summary>KDV tutarı: FinalPrice * KdvRate / 100</summary>
    public decimal KdvAmount { get; private set; }
    /// <summary>KDV dahil toplam: FinalPrice + KdvAmount</summary>
    public decimal TotalAmount { get; private set; }

    // ── Döviz ─────────────────────────────────────────────────────────────
    /// <summary>Fiyat para birimi (örn. "EUR", "USD", "TRY").</summary>
    public string PriceCurrency { get; private set; } = "TRY";
    /// <summary>Fiyat oluşturulurken kullanılan döviz kuru.</summary>
    public decimal PriceExchangeRate { get; private set; } = 1m;
    /// <summary>TRY bazında birim fiyat = UnitPrice × PriceExchangeRate.</summary>
    public decimal PriceBaseAmount { get; private set; }
    /// <summary>Kur kilitleme tipi: 1=Güncel Kur, 2=Onay Anı Kilidi, 3=Manuel Kilit.</summary>
    public int RateLockType { get; private set; } = 1;
    /// <summary>Kur kilitlendiği an (RateLockType=2/3 ise dolu).</summary>
    public DateTime? RateLockedAt { get; private set; }
    /// <summary>Kilitlenmiş döviz kuru değeri.</summary>
    public decimal? RateLockedValue { get; private set; }

    // ── Hekim ─────────────────────────────────────────────────────────────
    /// <summary>Bu kalemi yapan hekim (null ise plan'ın DoctorId'si kullanılır).</summary>
    public long? DoctorId { get; private set; }
    public User? Doctor { get; private set; }

    public string? Notes { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private TreatmentPlanItem() { }

    public static TreatmentPlanItem Create(
        long planId,
        long treatmentId,
        decimal unitPrice,
        decimal kdvRate = 0,
        decimal discountRate = 0,
        string? toothNumber = null,
        string? toothSurfaces = null,
        string? bodyRegionCode = null,
        long? doctorId = null,
        string? notes = null,
        string priceCurrency = "TRY",
        decimal priceExchangeRate = 1m)
    {
        if (discountRate < 0 || discountRate > 100)
            throw new ArgumentOutOfRangeException(nameof(discountRate), "İndirim oranı 0–100 arasında olmalıdır.");
        if (kdvRate < 0 || kdvRate > 100)
            throw new ArgumentOutOfRangeException(nameof(kdvRate), "KDV oranı 0–100 arasında olmalıdır.");
        if (priceExchangeRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(priceExchangeRate), "Döviz kuru sıfırdan büyük olmalıdır.");

        var finalPrice   = ComputeFinalPrice(unitPrice, discountRate);
        var kdvAmount    = Math.Round(finalPrice * kdvRate / 100, 2);
        var totalAmount  = finalPrice + kdvAmount;
        var priceBaseAmt = priceCurrency == "TRY"
            ? finalPrice
            : Math.Round(finalPrice * priceExchangeRate, 4);

        return new TreatmentPlanItem
        {
            PlanId            = planId,
            TreatmentId       = treatmentId,
            ToothNumber       = toothNumber,
            ToothSurfaces     = toothSurfaces,
            BodyRegionCode    = bodyRegionCode,
            Status            = TreatmentItemStatus.Planned,
            UnitPrice         = unitPrice,
            DiscountRate      = discountRate,
            FinalPrice        = finalPrice,
            KdvRate           = kdvRate,
            KdvAmount         = kdvAmount,
            TotalAmount       = totalAmount,
            DoctorId          = doctorId,
            Notes             = notes,
            PriceCurrency     = priceCurrency,
            PriceExchangeRate = priceCurrency == "TRY" ? 1m : priceExchangeRate,
            PriceBaseAmount   = priceBaseAmt,
            RateLockType      = 1
        };
    }

    /// <summary>Kuru kilitle (plan onayı veya manuel kilit).</summary>
    public void LockRate(int lockType, decimal lockedValue)
    {
        if (lockType < 1 || lockType > 3)
            throw new ArgumentOutOfRangeException(nameof(lockType), "Kilit tipi 1-3 arasında olmalıdır.");

        RateLockType   = lockType;
        RateLockedAt   = DateTime.UtcNow;
        RateLockedValue = lockedValue;
        MarkUpdated();
    }

    /// <summary>Hekim tarafından tamamlandı işareti.</summary>
    public void Complete(DateTime? completedAt = null)
    {
        if (Status == TreatmentItemStatus.Completed)
            throw new InvalidOperationException("Bu kalem zaten tamamlanmış.");
        if (Status == TreatmentItemStatus.Cancelled)
            throw new InvalidOperationException("İptal edilmiş kalem tamamlanamaz.");

        Status      = TreatmentItemStatus.Completed;
        CompletedAt = completedAt?.ToUniversalTime() ?? DateTime.UtcNow;
        MarkUpdated();
    }

    /// <summary>Plan onaylandığında item'lar da onaylanır.</summary>
    internal void SetApproved()
    {
        Status = TreatmentItemStatus.Approved;
        MarkUpdated();
    }

    public void UpdatePrice(decimal unitPrice, decimal discountRate)
    {
        if (Status != TreatmentItemStatus.Planned)
            throw new InvalidOperationException("Fiyat yalnızca planlanmış kalemlerde değiştirilebilir.");

        if (discountRate < 0 || discountRate > 100)
            throw new ArgumentOutOfRangeException(nameof(discountRate));

        UnitPrice   = unitPrice;
        DiscountRate = discountRate;
        FinalPrice  = ComputeFinalPrice(unitPrice, discountRate);
        KdvAmount   = Math.Round(FinalPrice * KdvRate / 100, 2);
        TotalAmount = FinalPrice + KdvAmount;
        MarkUpdated();
    }

    public void AddNote(string? note)
    {
        Notes = note;
        MarkUpdated();
    }

    private static decimal ComputeFinalPrice(decimal unitPrice, decimal discountRate)
        => Math.Round(unitPrice * (1 - discountRate / 100), 2);
}
