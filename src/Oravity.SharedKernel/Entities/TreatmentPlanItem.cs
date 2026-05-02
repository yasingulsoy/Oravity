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
///
/// Fiyat modeli (KDV DAHİL):
///   UnitPrice    = girilen KDV dahil fiyat
///   TotalAmount  = UnitPrice * (1 − DiscountRate / 100)  → hastanın ödeyeceği KDV dahil tutar
///   KdvAmount    = TotalAmount * KdvRate / (100 + KdvRate)  → KDV içinden çıkarılır
///   FinalPrice   = TotalAmount − KdvAmount                 → muhasebe için KDV hariç net
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
    /// <summary>Referans fiyat listesindeki ham fiyat (kampanya/kural öncesi). Pazarlama amaçlı "liste fiyatı".</summary>
    public decimal? ListPrice { get; private set; }
    public decimal UnitPrice { get; private set; }
    /// <summary>0–100 arası indirim yüzdesi.</summary>
    public decimal DiscountRate { get; private set; }
    /// <summary>KDV hariç net fiyat (muhasebe): TotalAmount − KdvAmount</summary>
    public decimal FinalPrice { get; private set; }
    /// <summary>Kalem oluşturulurken Treatment.KdvRate'den snapshot alınır.</summary>
    public decimal KdvRate { get; private set; }
    /// <summary>KDV tutarı (TotalAmount içinden çıkarılır): TotalAmount × KdvRate / (100 + KdvRate)</summary>
    public decimal KdvAmount { get; private set; }
    /// <summary>Hastanın ödeyeceği KDV dahil tutar: UnitPrice × (1 − DiscountRate / 100)</summary>
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

    // ── Kurum Katkısı (Provizyon) ──────────────────────────────────────────
    /// <summary>
    /// Bu kalemin ait olduğu provizyon kurumu — kalem eklenirken hastanın o anki AgreementInstitutionId'si snapshot alınır.
    /// Plan'ın InstitutionId'sinden bağımsız; hastanın kurumu değişip eski plana kalem eklendiğinde yeni kurum atanır.
    /// </summary>
    public long? InstitutionId { get; private set; }
    public Institution? Institution { get; private set; }

    /// <summary>
    /// Provizyon kurumu bu kalem için ne kadar öder (TZH'den alınan onay tutarı).
    /// Null → kurum katkısı yok (Discount modeli veya henüz girilmedi).
    /// </summary>
    public decimal? InstitutionContributionAmount { get; private set; }
    /// <summary>Hastanın ödemesi gereken tutar = FinalPrice - InstitutionContributionAmount.</summary>
    public decimal PatientAmount { get; private set; }

    // ── Hekim ─────────────────────────────────────────────────────────────
    /// <summary>Bu kalemi yapan hekim (null ise plan'ın DoctorId'si kullanılır).</summary>
    public long? DoctorId { get; private set; }
    public User? Doctor { get; private set; }

    // ── Onaylama ──────────────────────────────────────────────────────────
    /// <summary>Kalemi "Onaylandı" statüsüne geçiren kullanıcı.</summary>
    public long? ApprovedByUserId { get; private set; }
    public User? ApprovedBy { get; private set; }
    /// <summary>Kalemin onaylandığı zaman.</summary>
    public DateTime? ApprovedAt { get; private set; }

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
        decimal priceExchangeRate = 1m,
        decimal? listPrice = null,
        long? institutionId = null)
    {
        if (discountRate < 0 || discountRate > 100)
            throw new ArgumentOutOfRangeException(nameof(discountRate), "İndirim oranı 0–100 arasında olmalıdır.");
        if (kdvRate < 0 || kdvRate > 100)
            throw new ArgumentOutOfRangeException(nameof(kdvRate), "KDV oranı 0–100 arasında olmalıdır.");
        if (priceExchangeRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(priceExchangeRate), "Döviz kuru sıfırdan büyük olmalıdır.");

        // KDV DAHİL model: UnitPrice KDV dahil, KDV içinden çıkarılır
        var totalAmount  = ComputeFinalPrice(unitPrice, discountRate);           // hasta öder
        var kdvAmount    = Math.Round(totalAmount * kdvRate / (100 + kdvRate), 2);
        var finalPrice   = totalAmount - kdvAmount;                              // muhasebe
        var priceBaseAmt = priceCurrency == "TRY"
            ? totalAmount
            : Math.Round(totalAmount * priceExchangeRate, 4);

        return new TreatmentPlanItem
        {
            PlanId                        = planId,
            TreatmentId                   = treatmentId,
            InstitutionId                 = institutionId,
            ToothNumber                   = toothNumber,
            ToothSurfaces                 = toothSurfaces,
            BodyRegionCode                = bodyRegionCode,
            Status                        = TreatmentItemStatus.Planned,
            ListPrice                     = listPrice > 0 ? listPrice : null,
            UnitPrice                     = unitPrice,
            DiscountRate                  = discountRate,
            FinalPrice                    = finalPrice,
            KdvRate                       = kdvRate,
            KdvAmount                     = kdvAmount,
            TotalAmount                   = totalAmount,
            InstitutionContributionAmount = null,
            PatientAmount                 = priceBaseAmt,
            DoctorId                      = doctorId,
            Notes                         = notes,
            PriceCurrency                 = priceCurrency,
            PriceExchangeRate             = priceCurrency == "TRY" ? 1m : priceExchangeRate,
            PriceBaseAmount               = priceBaseAmt,
            RateLockType                  = 1
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

    /// <summary>
    /// Hekim tarafından tamamlandı işareti.
    /// Dövizli kalemlerde yapıldı anındaki kur kilitlenir → PriceBaseAmount güncellenir.
    /// </summary>
    public void Complete(DateTime? completedAt = null, decimal? completionRate = null)
    {
        if (Status == TreatmentItemStatus.Completed)
            throw new InvalidOperationException("Bu kalem zaten tamamlanmış.");
        if (Status == TreatmentItemStatus.Cancelled)
            throw new InvalidOperationException("İptal edilmiş kalem tamamlanamaz.");

        Status      = TreatmentItemStatus.Completed;
        CompletedAt = completedAt?.ToUniversalTime() ?? DateTime.UtcNow;

        // Dövizli kalem: yapıldı anındaki kuru kilitle, PriceBaseAmount'u güncelle
        if (PriceCurrency != "TRY" && completionRate.HasValue && completionRate.Value > 0)
        {
            RateLockType    = 2; // Yapıldı anı kilidi
            RateLockedAt    = CompletedAt;
            RateLockedValue = completionRate.Value;
            PriceBaseAmount = Math.Round(TotalAmount * completionRate.Value, 2);
            PatientAmount   = PriceBaseAmount; // Katkı henüz yok
        }

        MarkUpdated();
    }

    /// <summary>
    /// Ödeme anında kur farkı varsa günceller.
    /// Ödeme ekranında kullanıcı onayından sonra çağrılır.
    /// </summary>
    public void UpdatePaymentRate(decimal newRate)
    {
        if (PriceCurrency == "TRY")
            throw new InvalidOperationException("TRY cinsinden kalemlerde kur güncellemesi yapılamaz.");
        if (newRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(newRate), "Kur sıfırdan büyük olmalıdır.");

        RateLockType    = 2;
        RateLockedAt    = DateTime.UtcNow;
        RateLockedValue = newRate;
        PriceBaseAmount = Math.Round(TotalAmount * newRate, 2);
        PatientAmount   = PriceBaseAmount - (InstitutionContributionAmount ?? 0m);
        MarkUpdated();
    }

    /// <summary>
    /// Tamamlanmış kalemi 'Onaylandı' durumuna geri alır.
    /// Kurallar:
    ///   - Yalnızca 'Tamamlandı' durumundaki kalemler geri alınabilir.
    ///   - Ödeme tahsisi yapılmışsa bu metod dışarıdan çağrılmadan önce kontrol edilmeli.
    /// </summary>
    public void RevertToApproved()
    {
        if (Status != TreatmentItemStatus.Completed)
            throw new InvalidOperationException("Yalnızca 'Tamamlandı' durumundaki kalemler geri alınabilir.");

        Status      = TreatmentItemStatus.Approved;
        CompletedAt = null;

        // Yapıldı anı kur kilidini geri al
        if (RateLockType == 2)
        {
            RateLockType    = 1;
            RateLockedAt    = null;
            RateLockedValue = null;
            // PriceBaseAmount'u orijinal plan kuruna geri döndür
            PriceBaseAmount = PriceCurrency == "TRY"
                ? TotalAmount
                : Math.Round(TotalAmount * PriceExchangeRate, 4);
            PatientAmount   = PriceBaseAmount;
        }

        MarkUpdated();
    }

    /// <summary>Plan onaylandığında item'lar da onaylanır.</summary>
    internal void SetApproved(long? approvedByUserId = null)
    {
        Status             = TreatmentItemStatus.Approved;
        ApprovedByUserId   = approvedByUserId;
        ApprovedAt         = DateTime.UtcNow;
        MarkUpdated();
    }

    /// <summary>
    /// Onaylanmış kalemi tekrar 'Planlandı' durumuna geri alır.
    /// Kural: İmzalı onam formu varsa geri alınamaz.
    /// </summary>
    public void RevertToPlanned()
    {
        if (Status != TreatmentItemStatus.Approved)
            throw new InvalidOperationException("Yalnızca 'Onaylandı' durumundaki kalemler plana geri alınabilir.");

        Status = TreatmentItemStatus.Planned;
        MarkUpdated();
    }

    /// <summary>Kalemi tekil olarak Onaylandı yapar (seçili onaylama için).</summary>
    public void Approve(long? approvedByUserId = null)
    {
        if (Status != TreatmentItemStatus.Planned)
            throw new InvalidOperationException("Yalnızca 'Planlandı' durumundaki kalemler onaylanabilir.");

        Status           = TreatmentItemStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAt       = DateTime.UtcNow;
        MarkUpdated();
    }

    public void UpdateToothNumber(string? toothNumber)
    {
        ToothNumber = string.IsNullOrWhiteSpace(toothNumber) ? null : toothNumber.Trim();
        MarkUpdated();
    }

    public void UpdatePrice(decimal unitPrice, decimal discountRate)
    {
        if (Status != TreatmentItemStatus.Planned)
            throw new InvalidOperationException("Fiyat yalnızca planlanmış kalemlerde değiştirilebilir.");

        if (discountRate < 0 || discountRate > 100)
            throw new ArgumentOutOfRangeException(nameof(discountRate));

        UnitPrice    = unitPrice;
        DiscountRate = discountRate;
        TotalAmount  = ComputeFinalPrice(unitPrice, discountRate);
        KdvAmount    = Math.Round(TotalAmount * KdvRate / (100 + KdvRate), 2);
        FinalPrice   = TotalAmount - KdvAmount;
        PriceBaseAmount = PriceCurrency == "TRY" ? TotalAmount : Math.Round(TotalAmount * PriceExchangeRate, 4);
        var tryBase  = PriceCurrency == "TRY" ? TotalAmount : PriceBaseAmount;
        PatientAmount = tryBase - (InstitutionContributionAmount ?? 0m);
        MarkUpdated();
    }

    /// <summary>
    /// Resepsiyonun TZH'den aldığı onaya göre kurum katkı tutarını girer.
    /// PatientAmount otomatik hesaplanır: FinalPrice - contributionAmount.
    /// </summary>
    public void SetInstitutionContribution(decimal? contributionAmount, long? institutionId = null)
    {
        // Kurum katkısı her zaman TRY cinsindendir.
        // Dövizli kalemlerde PriceBaseAmount (plan oluşturma anındaki TRY karşılığı) referans alınır.
        // Not: Ceiling doğrulaması yapılmaz — kurum provizyon formundaki TRY tutarı
        // kur farkı nedeniyle PriceBaseAmount'u aşabilir. Doğru kur için kur modülü gereklidir.
        if (contributionAmount.HasValue && contributionAmount.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(contributionAmount), "Katkı tutarı negatif olamaz.");

        var tryBase = PriceCurrency == "TRY" ? TotalAmount : PriceBaseAmount;

        InstitutionContributionAmount = contributionAmount;
        PatientAmount = tryBase - (contributionAmount ?? 0m);

        // Kalem kurumla henüz eşleştirilmemişse ve katkı giriliyorsa kurumu da set et
        if (InstitutionId == null && institutionId.HasValue)
            InstitutionId = institutionId.Value;

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
