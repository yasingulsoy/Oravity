using System.Text.Json;
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

    public string? ChiefComplaint       { get; private set; }
    public string? ExaminationFindings  { get; private set; }
    public string? Diagnosis            { get; private set; }
    public string? TreatmentPlan        { get; private set; }
    public string? Notes                { get; private set; }

    /// <summary>Seçilen ICD tanıları — JSON dizi olarak protokol tablosunda saklanır.</summary>
    public string IcdDiagnosesJson { get; private set; } = "[]";

    public DateTime? StartedAt   { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public long CreatedBy { get; private set; }

    public ICollection<TreatmentPlan>      TreatmentPlans     { get; private set; } = [];
    public ICollection<PatientAnamnesis>   Anamneses          { get; private set; } = [];

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

    public void UpdateDetails(
        string? chiefComplaint,
        string? examinationFindings,
        string? diagnosis,
        string? treatmentPlan,
        string? notes)
    {
        ChiefComplaint      = chiefComplaint;
        ExaminationFindings = examinationFindings;
        Diagnosis           = diagnosis;
        TreatmentPlan       = treatmentPlan;
        Notes               = notes;
        MarkUpdated();
    }

    // ─── ICD tanı yönetimi ────────────────────────────────────────────────

    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public IReadOnlyList<IcdDiagnosisEntry> GetIcdDiagnoses() =>
        JsonSerializer.Deserialize<List<IcdDiagnosisEntry>>(IcdDiagnosesJson, _jsonOpts) ?? [];

    public IcdDiagnosisEntry AddIcdDiagnosis(long icdCodeId, string code, string description, string category, bool isPrimary)
    {
        var list = GetIcdDiagnoses().ToList();
        if (isPrimary)
            list = list.Select(x => x with { IsPrimary = false }).ToList();
        var entry = new IcdDiagnosisEntry(Guid.NewGuid(), icdCodeId, code, description, category, isPrimary);
        list.Add(entry);
        IcdDiagnosesJson = JsonSerializer.Serialize(list, _jsonOpts);
        SyncDiagnosis(list);
        MarkUpdated();
        return entry;
    }

    public void RemoveIcdDiagnosis(Guid entryId)
    {
        var list = GetIcdDiagnoses().Where(x => x.EntryId != entryId).ToList();
        IcdDiagnosesJson = JsonSerializer.Serialize(list, _jsonOpts);
        SyncDiagnosis(list);
        MarkUpdated();
    }

    private void SyncDiagnosis(IList<IcdDiagnosisEntry> list)
    {
        if (list.Count == 0) return;
        // Birincil tanılar önce, sonra ikinciller
        var codes = list.OrderByDescending(x => x.IsPrimary).Select(x => x.Code);
        Diagnosis = string.Join(", ", codes);
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

    /// <summary>
    /// Odandan ayrılma için zorunlu alanların tümü doldurulmuşsa true döner.
    /// Zorunlu: en az 1 ICD tanısı, şikayet, muayene bulguları, tedavi planı.
    /// </summary>
    public bool IsReadyToClose() =>
        GetIcdDiagnoses().Count > 0
        && !string.IsNullOrWhiteSpace(ChiefComplaint)
        && !string.IsNullOrWhiteSpace(ExaminationFindings)
        && !string.IsNullOrWhiteSpace(TreatmentPlan);
}

/// <summary>Protokol JSON alanında saklanan tek ICD tanı girişi.</summary>
public record IcdDiagnosisEntry(
    Guid   EntryId,
    long   IcdCodeId,
    string Code,
    string Description,
    string Category,
    bool   IsPrimary
);
