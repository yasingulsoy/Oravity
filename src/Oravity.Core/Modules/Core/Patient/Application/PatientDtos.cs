using PatientEntity = Oravity.SharedKernel.Entities.Patient;

namespace Oravity.Core.Modules.Core.Patient.Application;

public record PatientResponse(
    Guid PublicId,
    long BranchId,
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
    string? Neighborhood,
    string? Address,
    // Tıbbi
    string? BloodType,
    // Geliş / Kurum
    long? ReferralSourceId,
    string? ReferralSourceName,
    string? ReferralPerson,
    long? LastInstitutionId,
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
        p.PublicId, p.BranchId,
        p.FirstName, p.LastName, p.MotherName, p.FatherName,
        p.Gender, p.MaritalStatus, p.Nationality,
        p.CitizenshipTypeId, p.CitizenshipType?.Name,
        p.Occupation, p.SmokingType, p.PregnancyStatus,
        p.BirthDate,
        p.Phone, p.HomePhone, p.WorkPhone, p.Email,
        p.Country, p.City, p.District, p.Neighborhood, p.Address,
        p.BloodType,
        p.ReferralSourceId, p.ReferralSource?.Name, p.ReferralPerson,
        p.LastInstitutionId,
        p.Notes, p.PreferredLanguageCode,
        p.SmsOptIn, p.CampaignOptIn, p.IsActive, p.CreatedAt);
}
