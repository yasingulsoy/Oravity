using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum PatientFileType
{
    XRay        = 1,  // Röntgen
    Photo       = 2,  // Fotoğraf
    Orthodontic = 3,  // Ortodonti
    MedicalReport = 4,// Medikal Rapor
    Prescription = 5, // Reçete
    Consent     = 6,  // ONAM Formu
    Document    = 7   // Genel Doküman
}

/// <summary>
/// Hasta dosya kaydı — fiziksel dosya ayrıca upload edilir, burada meta tutulur (SPEC §DOSYALAR SEKMESİ).
/// deleted_at ile soft-delete edilir — is_deleted yerine zaman damgası kullanılır.
/// </summary>
public class PatientFile : BaseEntity
{
    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public PatientFileType FileType { get; private set; }
    public string? Category { get; private set; }
    public string? Title { get; private set; }

    /// <summary>Depolama yolu veya URL (fiziksel upload ayrı endpoint).</summary>
    public string FilePath { get; private set; } = default!;

    /// <summary>Bayt cinsinden.</summary>
    public int? FileSize { get; private set; }
    public string? FileExt { get; private set; }
    public string? Note { get; private set; }

    /// <summary>Çekim / oluşturma tarihi (opsiyonel).</summary>
    public DateTime? TakenAt { get; private set; }

    public long UploadedBy { get; private set; }
    public User UploadedByUser { get; private set; } = default!;
    public DateTime UploadedAt { get; private set; }

    /// <summary>Soft-delete tarihi. Null ise aktif.</summary>
    public DateTime? DeletedAt { get; private set; }

    private PatientFile() { }

    public static PatientFile Create(
        long patientId,
        long branchId,
        PatientFileType fileType,
        string filePath,
        long uploadedBy,
        string? category = null,
        string? title = null,
        int? fileSize = null,
        string? fileExt = null,
        string? note = null,
        DateTime? takenAt = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Dosya yolu boş olamaz.", nameof(filePath));

        return new PatientFile
        {
            PatientId  = patientId,
            BranchId   = branchId,
            FileType   = fileType,
            FilePath   = filePath,
            Category   = category,
            Title      = title,
            FileSize   = fileSize,
            FileExt    = fileExt,
            Note       = note,
            TakenAt    = takenAt,
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.UtcNow
        };
    }

    public new void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
        MarkUpdated();
    }
}
