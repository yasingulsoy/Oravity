using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum ProtocolStatus
{
    Open      = 1,  // Açık
    Completed = 2,  // Tamamlandı
    Cancelled = 3   // İptal
}

public enum ProtocolType
{
    Examination  = 1,  // Muayene
    Treatment    = 2,  // Tedavi
    Consultation = 3,  // Konsültasyon
    FollowUp     = 4,  // Kontrol
    Emergency    = 5   // Acil
}

/// <summary>
/// Vizite içinde hekim bazlı protokol kaydı (SPEC §VİZİTE & PROTOKOL MİMARİSİ).
/// Protocol No: branch_id + year bazlı sequence → ör. "2026/1452".
/// </summary>
public class Protocol : BaseEntity
{
    public long  VisitId  { get; private set; }
    public Visit Visit    { get; private set; } = default!;

    public long   BranchId  { get; private set; }
    public Branch Branch    { get; private set; } = default!;
    public long   CompanyId { get; private set; }

    public long    PatientId { get; private set; }
    public Patient Patient   { get; private set; } = default!;

    public long DoctorId { get; private set; }
    public User Doctor   { get; private set; } = default!;

    // ─── Protokol numarası ────────────────────────────────────────────────
    public int    ProtocolYear { get; private set; }
    public int    ProtocolSeq  { get; private set; }
    public string ProtocolNo   { get; private set; } = default!;  // "2026/1452"

    public ProtocolType   ProtocolType { get; private set; }
    public ProtocolStatus Status       { get; private set; } = ProtocolStatus.Open;

    public string? ChiefComplaint { get; private set; }
    public string? Diagnosis      { get; private set; }
    public string? Notes          { get; private set; }

    public DateTime? StartedAt   { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public long CreatedBy { get; private set; }

    public ICollection<TreatmentPlan>    TreatmentPlans { get; private set; } = [];
    public ICollection<PatientAnamnesis> Anamneses      { get; private set; } = [];

    private Protocol() { }

    public static Protocol Create(
        long visitId,
        long branchId,
        long companyId,
        long patientId,
        long doctorId,
        ProtocolType type,
        int year,
        int seq,
        long createdBy) => new()
    {
        VisitId      = visitId,
        BranchId     = branchId,
        CompanyId    = companyId,
        PatientId    = patientId,
        DoctorId     = doctorId,
        ProtocolType = type,
        ProtocolYear = year,
        ProtocolSeq  = seq,
        ProtocolNo   = $"{year}/{seq}",
        Status       = ProtocolStatus.Open,
        CreatedBy    = createdBy,
    };

    /// <summary>Hekim hastayı odaya çağırdığında çağrılır (odaya alındı).</summary>
    public void Start()
    {
        if (Status != ProtocolStatus.Open)
            throw new InvalidOperationException("Sadece açık protokoller başlatılabilir.");
        if (StartedAt.HasValue)
            throw new InvalidOperationException("Protokol zaten başlatılmış.");
        StartedAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public void UpdateDetails(string? chiefComplaint, string? diagnosis, string? notes)
    {
        ChiefComplaint = chiefComplaint;
        Diagnosis      = diagnosis;
        Notes          = notes;
        MarkUpdated();
    }

    public void Complete()
    {
        if (Status != ProtocolStatus.Open)
            throw new InvalidOperationException("Sadece açık protokoller tamamlanabilir.");
        Status      = ProtocolStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Cancel()
    {
        if (Status == ProtocolStatus.Completed)
            throw new InvalidOperationException("Tamamlanmış protokol iptal edilemez.");
        Status = ProtocolStatus.Cancelled;
        MarkUpdated();
    }
}
