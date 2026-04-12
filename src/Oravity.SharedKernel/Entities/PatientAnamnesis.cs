using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hasta anamnez formu (SPEC §SAĞLIK BİLGİLERİ & ANAMNEZİ).
/// Hasta başına tek kayıt — upsert semantiği. Kritik alanlar hasta kartında kırmızı banner olarak gösterilir.
/// Kritik tetikleyiciler: local_anesthesia_allergy, has_penicillin_allergy, on_anticoagulant,
/// bleeding_tendency, has_pacemaker, bisphosphonate_use, has_hiv, has_hepatitis_b, has_hepatitis_c.
/// </summary>
public class PatientAnamnesis : BaseEntity
{
    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    /// <summary>Her protokolde yeni anamnez yazılır. Null = eski kayıt (protokol öncesi).</summary>
    public long?     ProtocolId { get; private set; }
    public Protocol? Protocol   { get; private set; }

    // ── Genel ──────────────────────────────────────────────────────────────
    public string? BloodType { get; private set; }
    public bool IsPregnant { get; private set; }
    public bool IsBreastfeeding { get; private set; }

    // ── Sistemik hastalıklar ───────────────────────────────────────────────
    public bool HasDiabetes { get; private set; }
    public bool HasHypertension { get; private set; }
    public bool HasHeartDisease { get; private set; }
    public bool HasPacemaker { get; private set; }
    public bool HasAsthma { get; private set; }
    public bool HasEpilepsy { get; private set; }
    public bool HasKidneyDisease { get; private set; }
    public bool HasLiverDisease { get; private set; }
    public bool HasHiv { get; private set; }
    public bool HasHepatitisB { get; private set; }
    public bool HasHepatitisC { get; private set; }
    public string? OtherSystemicDiseases { get; private set; }

    // ── Diş hekimliği spesifik ────────────────────────────────────────────
    public bool LocalAnesthesiaAllergy { get; private set; }
    public string? LocalAnesthesiaAllergyNote { get; private set; }
    public bool BleedingTendency { get; private set; }
    public bool OnAnticoagulant { get; private set; }
    public string? AnticoagulantDrug { get; private set; }
    public bool BisphosphonateUse { get; private set; }

    // ── Alerjiler ─────────────────────────────────────────────────────────
    public bool HasPenicillinAllergy { get; private set; }
    public bool HasAspirinAllergy { get; private set; }
    public bool HasLatexAllergy { get; private set; }
    public string? OtherAllergies { get; private set; }

    // ── Geçirilen ameliyatlar ─────────────────────────────────────────────
    public string? PreviousSurgeries { get; private set; }

    // ── Ağız bakımı alışkanlıkları ────────────────────────────────────────
    public int? BrushingFrequency { get; private set; }
    public bool UsesFloss { get; private set; }

    // ── Sosyal alışkanlıklar ───────────────────────────────────────────────
    /// <summary>0=Hayır, 1=Evet, 2=Bıraktı</summary>
    public int? SmokingStatus { get; private set; }
    public string? SmokingAmount { get; private set; }
    /// <summary>0=Hayır, 1=Sosyal, 2=Düzenli</summary>
    public int? AlcoholUse { get; private set; }

    public string? AdditionalNotes { get; private set; }

    // ── Meta ──────────────────────────────────────────────────────────────
    public long FilledBy { get; private set; }
    public User FilledByUser { get; private set; } = default!;
    public DateTime FilledAt { get; private set; }

    public long? UpdatedBy { get; private set; }
    public User? UpdatedByUser { get; private set; }
    public DateTime? UpdatedByAt { get; private set; }

    // ── Computed: Kritik uyarı ────────────────────────────────────────────
    public bool HasCriticalAlert =>
        LocalAnesthesiaAllergy || HasPenicillinAllergy || OnAnticoagulant ||
        BleedingTendency || HasPacemaker || BisphosphonateUse ||
        HasHiv || HasHepatitisB || HasHepatitisC;

    private PatientAnamnesis() { }

    public static PatientAnamnesis Create(long patientId, long branchId, long filledBy, long? protocolId = null)
    {
        return new PatientAnamnesis
        {
            PatientId  = patientId,
            BranchId   = branchId,
            FilledBy   = filledBy,
            FilledAt   = DateTime.UtcNow,
            ProtocolId = protocolId,
        };
    }

    /// <summary>Tüm form alanlarını günceller.</summary>
    public void Update(PatientAnamnesisData data, long updatedBy)
    {
        BloodType                   = data.BloodType;
        IsPregnant                  = data.IsPregnant;
        IsBreastfeeding             = data.IsBreastfeeding;
        HasDiabetes                 = data.HasDiabetes;
        HasHypertension             = data.HasHypertension;
        HasHeartDisease             = data.HasHeartDisease;
        HasPacemaker                = data.HasPacemaker;
        HasAsthma                   = data.HasAsthma;
        HasEpilepsy                 = data.HasEpilepsy;
        HasKidneyDisease            = data.HasKidneyDisease;
        HasLiverDisease             = data.HasLiverDisease;
        HasHiv                      = data.HasHiv;
        HasHepatitisB               = data.HasHepatitisB;
        HasHepatitisC               = data.HasHepatitisC;
        OtherSystemicDiseases       = data.OtherSystemicDiseases;
        LocalAnesthesiaAllergy      = data.LocalAnesthesiaAllergy;
        LocalAnesthesiaAllergyNote  = data.LocalAnesthesiaAllergyNote;
        BleedingTendency            = data.BleedingTendency;
        OnAnticoagulant             = data.OnAnticoagulant;
        AnticoagulantDrug           = data.AnticoagulantDrug;
        BisphosphonateUse           = data.BisphosphonateUse;
        HasPenicillinAllergy        = data.HasPenicillinAllergy;
        HasAspirinAllergy           = data.HasAspirinAllergy;
        HasLatexAllergy             = data.HasLatexAllergy;
        OtherAllergies              = data.OtherAllergies;
        PreviousSurgeries           = data.PreviousSurgeries;
        BrushingFrequency           = data.BrushingFrequency;
        UsesFloss                   = data.UsesFloss;
        SmokingStatus               = data.SmokingStatus;
        SmokingAmount               = data.SmokingAmount;
        AlcoholUse                  = data.AlcoholUse;
        AdditionalNotes             = data.AdditionalNotes;
        UpdatedBy                   = updatedBy;
        UpdatedByAt                 = DateTime.UtcNow;
        MarkUpdated();
    }
}

/// <summary>Upsert için taşıyıcı nesne — command'dan entity'ye köprü.</summary>
public record PatientAnamnesisData(
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
    string? AdditionalNotes
);
