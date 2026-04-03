namespace Oravity.SharedKernel.Entities;

public enum DataExportStatus
{
    Pending   = 1,  // Bekliyor
    Ready     = 2,  // Hazır (dosya oluşturuldu)
    Delivered = 3   // Teslim Edildi
}

/// <summary>
/// KVKK veri erişim/taşıma talebi — SPEC §KVKK.
/// Hasta, kendi verilerinin bir kopyasını talep eder.
/// Sistem dosyayı hazırlayıp file_path'e yazar, hasta portal üzerinden indirir.
/// Dosya expires_at sonrası otomatik silinir.
/// </summary>
public class DataExportRequest
{
    public long Id { get; private set; }

    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    /// <summary>Talebi başlatan personel veya hasta (portal üzerinden ise null olabilir).</summary>
    public long RequestedBy { get; private set; }
    public User Requester { get; private set; } = default!;

    public DataExportStatus Status { get; private set; } = DataExportStatus.Pending;

    /// <summary>Hazırlanan ZIP/JSON dosyasının sunucu yolu.</summary>
    public string? FilePath { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Dosyanın geçerlilik tarihi — geçtikten sonra otomatik silinir.</summary>
    public DateTime? ExpiresAt { get; private set; }

    private DataExportRequest() { }

    public static DataExportRequest Create(long patientId, long requestedBy)
    {
        return new DataExportRequest
        {
            PatientId   = patientId,
            RequestedBy = requestedBy,
            Status      = DataExportStatus.Pending,
            CreatedAt   = DateTime.UtcNow
        };
    }

    public void MarkReady(string filePath, int expiryHours = 72)
    {
        Status      = DataExportStatus.Ready;
        FilePath    = filePath;
        CompletedAt = DateTime.UtcNow;
        ExpiresAt   = DateTime.UtcNow.AddHours(expiryHours);
    }

    public void MarkDelivered()
    {
        Status = DataExportStatus.Delivered;
    }
}
