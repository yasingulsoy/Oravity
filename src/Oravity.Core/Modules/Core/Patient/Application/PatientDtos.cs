using PatientEntity = Oravity.SharedKernel.Entities.Patient;

namespace Oravity.Core.Modules.Core.Patient.Application;

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
    long? LastInstitutionId,
    string? LastInstitutionName,
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
        p.LastInstitutionId, p.LastInstitution?.Name,
        p.Notes, p.PreferredLanguageCode,
        p.SmsOptIn, p.CampaignOptIn, p.IsActive, p.CreatedAt);
}
