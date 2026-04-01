using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum ComplaintSource
{
    Phone       = 1,  // Telefon
    FaceToFace  = 2,  // Yüz Yüze
    Email       = 3,  // Email
    Web         = 4,  // Web
    Survey      = 5,  // Anket (otomatik)
    SocialMedia = 6   // Sosyal Medya
}

public enum ComplaintStatus
{
    New        = 1,  // Yeni
    InProgress = 2,  // İnceleniyor
    Resolved   = 3,  // Çözüldü
    Closed     = 4,  // Kapatıldı
    Escalated  = 5   // Eskalasyonda
}

public enum ComplaintPriority
{
    Low    = 1,
    Normal = 2,
    High   = 3,
    Urgent = 4
}

/// <summary>
/// Hasta şikayeti (SPEC §ŞİKAYET YÖNETİMİ §3.2).
/// complaint_no otomatik üretilir: SC-{yıl}-{sequence}.
/// SLA: priority'e göre hesaplanır.
/// Düşük puan anketi → otomatik oluşur (Source=Survey).
/// </summary>
public class Complaint : BaseEntity
{
    public long CompanyId { get; private set; }
    public Company Company { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public long? PatientId { get; private set; }
    public Patient? Patient { get; private set; }

    public ComplaintSource Source { get; private set; }

    public string Subject { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    public ComplaintStatus Status { get; private set; } = ComplaintStatus.New;
    public ComplaintPriority Priority { get; private set; } = ComplaintPriority.Normal;

    /// <summary>Atanan personel ID'si.</summary>
    public long? AssignedTo { get; private set; }
    public User? AssignedUser { get; private set; }

    public string? Resolution { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    /// <summary>SLA son tarihi (priority'e göre otomatik hesaplanır).</summary>
    public DateTime? SlaDueAt { get; private set; }

    /// <summary>İlgili anket yanıtı (Source=Survey ise dolu).</summary>
    public long? SurveyResponseId { get; private set; }

    public long CreatedBy { get; private set; }
    public User Creator { get; private set; } = default!;

    public ICollection<ComplaintNote> Notes { get; private set; } = [];

    private Complaint() { }

    /// <summary>SLA süresi: Low=72s, Normal=48s, High=24s, Urgent=4s</summary>
    public static int SlaHoursFor(ComplaintPriority priority) => priority switch
    {
        ComplaintPriority.Low    => 72,
        ComplaintPriority.Normal => 48,
        ComplaintPriority.High   => 24,
        ComplaintPriority.Urgent => 4,
        _                        => 48
    };

    public static Complaint Create(
        long companyId, long branchId, long createdBy,
        ComplaintSource source, string subject, string description,
        ComplaintPriority priority = ComplaintPriority.Normal,
        long? patientId = null, long? surveyResponseId = null)
    {
        return new Complaint
        {
            CompanyId        = companyId,
            BranchId         = branchId,
            CreatedBy        = createdBy,
            Source           = source,
            Subject          = subject,
            Description      = description,
            Priority         = priority,
            PatientId        = patientId,
            SurveyResponseId = surveyResponseId,
            Status           = ComplaintStatus.New,
            SlaDueAt         = DateTime.UtcNow.AddHours(SlaHoursFor(priority))
        };
    }

    public void Assign(long userId)
    {
        AssignedTo = userId;
        if (Status == ComplaintStatus.New)
            Status = ComplaintStatus.InProgress;
        MarkUpdated();
    }

    public void Resolve(string resolution, long resolvedBy)
    {
        Status     = ComplaintStatus.Resolved;
        Resolution = resolution;
        ResolvedAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Close()
    {
        Status = ComplaintStatus.Closed;
        MarkUpdated();
    }

    public void UpdateStatus(ComplaintStatus newStatus)
    {
        Status = newStatus;
        if (newStatus == ComplaintStatus.Resolved && ResolvedAt is null)
            ResolvedAt = DateTime.UtcNow;
        MarkUpdated();
    }
}
