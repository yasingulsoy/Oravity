using PatientEntity = Oravity.SharedKernel.Entities.Patient;

namespace Oravity.Core.Modules.Core.Patient.Application;

public record PatientResponse(
    Guid PublicId,
    long BranchId,
    string FirstName,
    string LastName,
    string? Phone,
    string? Email,
    DateOnly? BirthDate,
    string? Gender,
    string? Address,
    string? BloodType,
    string PreferredLanguageCode,
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
        p.PublicId, p.BranchId, p.FirstName, p.LastName,
        p.Phone, p.Email, p.BirthDate, p.Gender,
        p.Address, p.BloodType, p.PreferredLanguageCode, p.IsActive, p.CreatedAt);
}
