namespace Oravity.SharedKernel.Entities;

/// <summary>
/// KVKK onay/ret logu — SPEC §KVKK YÖNETİMİ §kvkk_consents.
/// Her consent değişikliği (verme / geri alma) yeni satır olarak kaydedilir.
/// Güncel durum en son kayda bakılarak belirlenir.
/// </summary>
public class KvkkConsentLog
{
    public long Id { get; private set; }

    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    /// <summary>
    /// Onay türü:
    ///   data_processing  → Tedavi amaçlı veri işleme (zorunlu)
    ///   marketing        → Pazarlama iletişimi
    ///   third_party_sharing → Lab / sigorta ile paylaşım
    ///   health_data      → Özel nitelikli sağlık verisi işleme
    /// </summary>
    public string ConsentType { get; private set; } = default!;

    /// <summary>true = onay verildi, false = geri alındı.</summary>
    public bool IsGiven { get; private set; }

    public DateTime GivenAt { get; private set; }

    public string? IpAddress { get; private set; }

    /// <summary>Onay geri alındıysa dolu.</summary>
    public DateTime? RevokedAt { get; private set; }

    private KvkkConsentLog() { }

    public static KvkkConsentLog Record(
        long patientId,
        string consentType,
        bool isGiven,
        string? ipAddress = null)
    {
        return new KvkkConsentLog
        {
            PatientId   = patientId,
            ConsentType = consentType,
            IsGiven     = isGiven,
            GivenAt     = DateTime.UtcNow,
            IpAddress   = ipAddress,
            RevokedAt   = isGiven ? null : DateTime.UtcNow
        };
    }
}
