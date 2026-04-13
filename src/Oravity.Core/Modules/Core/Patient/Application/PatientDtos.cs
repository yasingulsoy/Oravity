using PatientEntity = Oravity.SharedKernel.Entities.Patient;

namespace Oravity.Core.Modules.Core.Patient.Application;

public record PatientAnamnesisResponse(
    Guid    PublicId,
    // Genel
    string? BloodType,
    bool    IsPregnant,
    bool    IsBreastfeeding,
    // Sistemik
    bool    HasDiabetes,
    bool    HasHypertension,
    bool    HasHeartDisease,
    bool    HasPacemaker,
    bool    HasAsthma,
    bool    HasEpilepsy,
    bool    HasKidneyDisease,
    bool    HasLiverDisease,
    bool    HasHiv,
    bool    HasHepatitisB,
    bool    HasHepatitisC,
    string? OtherSystemicDiseases,
    // Diş hekimliği spesifik
    bool    LocalAnesthesiaAllergy,
    string? LocalAnesthesiaAllergyNote,
    bool    BleedingTendency,
    bool    OnAnticoagulant,
    string? AnticoagulantDrug,
    bool    BisphosphonateUse,
    // Alerjiler
    bool    HasPenicillinAllergy,
    bool    HasAspirinAllergy,
    bool    HasLatexAllergy,
    string? OtherAllergies,
    // Diğer
    string? PreviousSurgeries,
    int?    BrushingFrequency,
    bool    UsesFloss,
    int?    SmokingStatus,
    string? SmokingAmount,
    int?    AlcoholUse,
    string? AdditionalNotes,
    bool    HasCriticalAlert,
    DateTime FilledAt,
    string  FilledByName
);

public record AnamnesisHistoryItem(
    Guid     PublicId,
    DateTime FilledAt,
    string   FilledByName,
    bool     HasCriticalAlert,
    string?  BloodType,
    int?     SmokingStatus,
    int?     AlcoholUse
);

public record PatientResponse(
    long Id,
    Guid PublicId,
    long BranchId,
    // Kimlik (şifreli alanlar: sadece var/yok bilgisi döner)
    bool HasTcNumber,
    bool HasPassportNo,
    // Kişisel
    string FirstName,
    string LastName,
    string? MotherName,
    string? FatherName,
    string? Gender,
    string? MaritalStatus,
    string? Nationality,
    long? CitizenshipTypeId,
    string? CitizenshipTypeName,
    string? Occupation,
    string? SmokingType,
    int? PregnancyStatus,
    DateOnly? BirthDate,
    // İletişim
    string? Phone,
    string? HomePhone,
    string? WorkPhone,
    string? Email,
    // Adres
    string? Country,
    string? City,
    string? District,
    string? Address,
    // Tıbbi
    string? BloodType,
    // Geliş / Kurum
    long? ReferralSourceId,
    string? ReferralSourceName,
    string? ReferralPerson,
    long? AgreementInstitutionId,
    string? AgreementInstitutionName,
    long? InsuranceInstitutionId,
    string? InsuranceInstitutionName,
    // Sistem
    string? Notes,
    string PreferredLanguageCode,
    bool SmsOptIn,
    bool CampaignOptIn,
    bool IsActive,
    DateTime CreatedAt
);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);

public static class PatientMappings
{
    public static PatientResponse ToResponse(PatientEntity p) => new(
        p.Id, p.PublicId, p.BranchId,
        p.TcNumberEncrypted != null, p.PassportNoEncrypted != null,
        p.FirstName, p.LastName, p.MotherName, p.FatherName,
        p.Gender, p.MaritalStatus, p.Nationality,
        p.CitizenshipTypeId, p.CitizenshipType?.Name,
        p.Occupation, p.SmokingType, p.PregnancyStatus,
        p.BirthDate,
        p.Phone, p.HomePhone, p.WorkPhone, p.Email,
        p.Country, p.City, p.District, p.Address,
        p.BloodType,
        p.ReferralSourceId, p.ReferralSource?.Name, p.ReferralPerson,
        p.AgreementInstitutionId, p.AgreementInstitution?.Name,
        p.InsuranceInstitutionId, p.InsuranceInstitution?.Name,
        p.Notes, p.PreferredLanguageCode,
        p.SmsOptIn, p.CampaignOptIn, p.IsActive, p.CreatedAt);
}
