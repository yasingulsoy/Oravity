using Oravity.SharedKernel.Entities;
using AnamnesisEntity = Oravity.SharedKernel.Entities.PatientAnamnesis;
using MedicationEntity = Oravity.SharedKernel.Entities.PatientMedication;
using NoteEntity = Oravity.SharedKernel.Entities.PatientNote;
using FileEntity = Oravity.SharedKernel.Entities.PatientFile;

namespace Oravity.Core.Modules.Core.PatientRecord.Application;

// ── Anamnesis ─────────────────────────────────────────────────────────────

public record PatientAnamnesisResponse(
    Guid PublicId,
    long PatientId,
    string? BloodType,
    bool IsPregnant,
    bool IsBreastfeeding,
    bool HasDiabetes,
    bool HasHypertension,
    bool HasHeartDisease,
    bool HasPacemaker,
    bool HasAsthma,
    bool HasEpilepsy,
    bool HasKidneyDisease,
    bool HasLiverDisease,
    bool HasHiv,
    bool HasHepatitisB,
    bool HasHepatitisC,
    string? OtherSystemicDiseases,
    bool LocalAnesthesiaAllergy,
    string? LocalAnesthesiaAllergyNote,
    bool BleedingTendency,
    bool OnAnticoagulant,
    string? AnticoagulantDrug,
    bool BisphosphonateUse,
    bool HasPenicillinAllergy,
    bool HasAspirinAllergy,
    bool HasLatexAllergy,
    string? OtherAllergies,
    string? PreviousSurgeries,
    int? BrushingFrequency,
    bool UsesFloss,
    int? SmokingStatus,
    string? SmokingAmount,
    int? AlcoholUse,
    string? AdditionalNotes,
    bool HasCriticalAlert,
    IReadOnlyList<string> CriticalAlerts,
    long FilledBy,
    DateTime FilledAt,
    DateTime? UpdatedAt
);

// ── Medication ────────────────────────────────────────────────────────────

public record PatientMedicationResponse(
    long Id,
    long PatientId,
    string DrugName,
    string? Dose,
    string? Frequency,
    string? Reason,
    bool IsActive,
    long AddedBy,
    DateTime AddedAt
);

// ── Note ──────────────────────────────────────────────────────────────────

public record PatientNoteResponse(
    Guid PublicId,
    long PatientId,
    NoteType Type,
    string TypeLabel,
    string? Title,
    string Content,
    bool IsPinned,
    bool IsHidden,
    long? AppointmentId,
    long CreatedBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

// ── File ──────────────────────────────────────────────────────────────────

public record PatientFileResponse(
    Guid PublicId,
    long PatientId,
    PatientFileType FileType,
    string FileTypeLabel,
    string? Category,
    string? Title,
    string FilePath,
    int? FileSize,
    string? FileExt,
    string? Note,
    DateTime? TakenAt,
    long UploadedBy,
    DateTime UploadedAt
);

// ── Mappings ──────────────────────────────────────────────────────────────

public static class PatientRecordMappings
{
    public static PatientAnamnesisResponse ToResponse(AnamnesisEntity a)
    {
        var alerts = BuildAlerts(a);
        return new PatientAnamnesisResponse(
            a.PublicId, a.PatientId,
            a.BloodType, a.IsPregnant, a.IsBreastfeeding,
            a.HasDiabetes, a.HasHypertension, a.HasHeartDisease, a.HasPacemaker,
            a.HasAsthma, a.HasEpilepsy, a.HasKidneyDisease, a.HasLiverDisease,
            a.HasHiv, a.HasHepatitisB, a.HasHepatitisC, a.OtherSystemicDiseases,
            a.LocalAnesthesiaAllergy, a.LocalAnesthesiaAllergyNote,
            a.BleedingTendency, a.OnAnticoagulant, a.AnticoagulantDrug,
            a.BisphosphonateUse,
            a.HasPenicillinAllergy, a.HasAspirinAllergy, a.HasLatexAllergy, a.OtherAllergies,
            a.PreviousSurgeries,
            a.BrushingFrequency, a.UsesFloss,
            a.SmokingStatus, a.SmokingAmount, a.AlcoholUse,
            a.AdditionalNotes,
            a.HasCriticalAlert, alerts,
            a.FilledBy, a.FilledAt, a.UpdatedAt);
    }

    public static PatientMedicationResponse ToMedicationResponse(MedicationEntity m) => new(
        m.Id, m.PatientId, m.DrugName, m.Dose, m.Frequency, m.Reason,
        m.IsActive, m.AddedBy, m.AddedAt);

    public static PatientNoteResponse ToNoteResponse(NoteEntity n) => new(
        n.PublicId, n.PatientId, n.Type, NoteTypeLabel(n.Type),
        n.Title, n.Content, n.IsPinned, n.IsHidden,
        n.AppointmentId, n.CreatedBy, n.CreatedAt, n.UpdatedAt);

    public static PatientFileResponse ToFileResponse(FileEntity f) => new(
        f.PublicId, f.PatientId, f.FileType, FileTypeLabel(f.FileType),
        f.Category, f.Title, f.FilePath, f.FileSize, f.FileExt, f.Note,
        f.TakenAt, f.UploadedBy, f.UploadedAt);

    // ── Kritik uyarı listesi ──────────────────────────────────────────────
    private static IReadOnlyList<string> BuildAlerts(AnamnesisEntity a)
    {
        var list = new List<string>();
        if (a.LocalAnesthesiaAllergy) list.Add("Lokal Anestezi Alerjisi" +
            (a.LocalAnesthesiaAllergyNote is not null ? $": {a.LocalAnesthesiaAllergyNote}" : ""));
        if (a.HasPenicillinAllergy)   list.Add("Penisilin Alerjisi");
        if (a.OnAnticoagulant)        list.Add("Kan Sulandırıcı" +
            (a.AnticoagulantDrug is not null ? $": {a.AnticoagulantDrug}" : ""));
        if (a.BleedingTendency)       list.Add("Kanama Eğilimi");
        if (a.HasPacemaker)           list.Add("Kalp Pili Var");
        if (a.BisphosphonateUse)      list.Add("Bifosfonat Kullanıyor");
        if (a.HasHiv)                 list.Add("HIV Pozitif");
        if (a.HasHepatitisB)          list.Add("Hepatit B");
        if (a.HasHepatitisC)          list.Add("Hepatit C");
        return list;
    }

    private static string NoteTypeLabel(NoteType t) => t switch
    {
        NoteType.General     => "Genel Not",
        NoteType.Clinical    => "Klinik Not",
        NoteType.Hidden      => "Gizli Not",
        NoteType.Plan        => "Plan Notu",
        NoteType.Treatment   => "Tedavi Notu",
        NoteType.Orthodontic => "Ortodonti Notu",
        _                    => t.ToString()
    };

    private static string FileTypeLabel(PatientFileType t) => t switch
    {
        PatientFileType.XRay          => "Röntgen",
        PatientFileType.Photo         => "Fotoğraf",
        PatientFileType.Orthodontic   => "Ortodonti",
        PatientFileType.MedicalReport => "Medikal Rapor",
        PatientFileType.Prescription  => "Reçete",
        PatientFileType.Consent       => "ONAM Formu",
        PatientFileType.Document      => "Doküman",
        _                             => t.ToString()
    };
}
