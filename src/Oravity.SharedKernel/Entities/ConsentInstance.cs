using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum ConsentInstanceStatus
{
    Pending  = 1,  // İmza bekliyor
    Signed   = 2,  // İmzalandı
    Expired  = 3,  // Süresi doldu
    Cancelled = 4  // İptal
}

/// <summary>
/// Belirli bir tedavi planı kalemi grubu için oluşturulan dijital onam kaydı.
/// QR veya SMS token ile hasta imzalar.
/// </summary>
public class ConsentInstance : BaseEntity
{
    public long CompanyId { get; private set; }
    public long PatientId { get; private set; }
    public long TreatmentPlanId { get; private set; }
    public long FormTemplateId { get; private set; }

    /// <summary>İnsan okunabilir kod: CF-2026-00042</summary>
    public string ConsentCode { get; private set; } = default!;

    /// <summary>JSON: hangi tedavi kalemi publicId'leri bu onam kapsamında.</summary>
    public string ItemPublicIdsJson { get; private set; } = "[]";

    /// <summary>qr | sms | both</summary>
    public string DeliveryMethod { get; private set; } = "qr";

    public ConsentInstanceStatus Status { get; private set; } = ConsentInstanceStatus.Pending;

    // ── QR token ─────────────────────────────────────────────────────────
    public string? QrToken { get; private set; }
    public DateTime? QrTokenExpiresAt { get; private set; }

    // ── SMS token ─────────────────────────────────────────────────────────
    public string? SmsToken { get; private set; }
    public DateTime? SmsTokenExpiresAt { get; private set; }

    // ── İmza ─────────────────────────────────────────────────────────────
    public DateTime? SignedAt { get; private set; }
    public string? SignerIp { get; private set; }
    public string? SignerDevice { get; private set; }
    /// <summary>İmzalayan kişi adı (hasta veya vasi).</summary>
    public string? SignerName { get; private set; }
    /// <summary>Canvas imzası — base64 PNG veri URI.</summary>
    public string? SignatureDataBase64 { get; private set; }
    /// <summary>JSON: [{id, checked}] — hastanın işaretlediği checkbox'lar.</summary>
    public string? CheckboxAnswersJson { get; private set; }

    public long? CreatedByUserId { get; private set; }

    // Nav properties
    public Patient? Patient { get; private set; }
    public TreatmentPlan? TreatmentPlan { get; private set; }
    public ConsentFormTemplate? FormTemplate { get; private set; }

    private ConsentInstance() { }

    public static ConsentInstance Create(
        long companyId,
        long patientId,
        long treatmentPlanId,
        long formTemplateId,
        string consentCode,
        string itemPublicIdsJson,
        string deliveryMethod,
        long? createdByUserId)
    {
        var now = DateTime.UtcNow;
        var expiry = now.AddHours(24);

        var instance = new ConsentInstance
        {
            CompanyId        = companyId,
            PatientId        = patientId,
            TreatmentPlanId  = treatmentPlanId,
            FormTemplateId   = formTemplateId,
            ConsentCode      = consentCode,
            ItemPublicIdsJson = itemPublicIdsJson,
            DeliveryMethod   = deliveryMethod,
            Status           = ConsentInstanceStatus.Pending,
            CreatedByUserId  = createdByUserId,
        };

        if (deliveryMethod == "qr" || deliveryMethod == "both")
        {
            instance.QrToken          = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            instance.QrTokenExpiresAt = expiry;
        }

        if (deliveryMethod == "sms" || deliveryMethod == "both")
        {
            instance.SmsToken          = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            instance.SmsTokenExpiresAt = expiry;
        }

        return instance;
    }

    public void Sign(
        string? signerName,
        string? signatureDataBase64,
        string? checkboxAnswersJson,
        string? signerIp,
        string? signerDevice)
    {
        if (Status == ConsentInstanceStatus.Signed)
            throw new InvalidOperationException("Bu form zaten imzalanmış.");
        if (Status == ConsentInstanceStatus.Expired || Status == ConsentInstanceStatus.Cancelled)
            throw new InvalidOperationException("Bu form artık imzalanamaz.");

        Status                = ConsentInstanceStatus.Signed;
        SignedAt              = DateTime.UtcNow;
        SignerName            = signerName;
        SignatureDataBase64   = signatureDataBase64;
        CheckboxAnswersJson   = checkboxAnswersJson;
        SignerIp              = signerIp;
        SignerDevice          = signerDevice;
        MarkUpdated();
    }

    public void Cancel()
    {
        Status = ConsentInstanceStatus.Cancelled;
        MarkUpdated();
    }

    public void MarkExpired()
    {
        Status = ConsentInstanceStatus.Expired;
        MarkUpdated();
    }

    /// <summary>Verilen token (QR veya SMS) geçerli mi?</summary>
    public bool IsTokenValid(string token)
    {
        var now = DateTime.UtcNow;
        if (QrToken == token && QrTokenExpiresAt > now) return true;
        if (SmsToken == token && SmsTokenExpiresAt > now) return true;
        return false;
    }
}
