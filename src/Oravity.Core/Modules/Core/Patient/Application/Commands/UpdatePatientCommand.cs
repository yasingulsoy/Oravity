using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Patient.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Patient.Application.Commands;

public record UpdatePatientCommand(
    Guid PublicId,
    string FirstName,
    string LastName,
    string? Phone,
    string? Email,
    DateOnly? BirthDate,
    string? Gender,
    string? Address,
    string? BloodType,
    string? PreferredLanguageCode,
    string? MotherName = null,
    string? FatherName = null,
    string? MaritalStatus = null,
    string? Nationality = null,
    string? Occupation = null,
    string? SmokingType = null,
    int? PregnancyStatus = null,
    string? HomePhone = null,
    string? WorkPhone = null,
    string? Country = null,
    string? City = null,
    string? District = null,
    long? ReferralSourceId = null,
    string? ReferralPerson = null,
    long? AgreementInstitutionId = null,
    long? InsuranceInstitutionId = null,
    long? CitizenshipTypeId = null,
    string? Notes = null,
    bool? SmsOptIn = null,
    bool? CampaignOptIn = null,
    string? TcNumber = null,
    string? PassportNo = null
) : IRequest<PatientResponse>;

public class UpdatePatientCommandHandler : IRequestHandler<UpdatePatientCommand, PatientResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IEncryptionService _encryption;

    public UpdatePatientCommandHandler(
        AppDbContext db,
        ITenantContext tenantContext,
        IEncryptionService encryption)
    {
        _db = db;
        _tenantContext = tenantContext;
        _encryption = encryption;
    }

    public async Task<PatientResponse> Handle(
        UpdatePatientCommand request,
        CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.PublicId == request.PublicId, cancellationToken)
            ?? throw new NotFoundException($"Hasta bulunamadı: {request.PublicId}");

        EnsureTenantAccess(patient);

        patient.Update(
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Email,
            request.BirthDate,
            request.Gender,
            request.Address,
            request.BloodType,
            request.PreferredLanguageCode,
            request.MotherName,
            request.FatherName,
            request.MaritalStatus,
            request.Nationality,
            request.Occupation,
            request.SmokingType,
            request.PregnancyStatus,
            request.HomePhone,
            request.WorkPhone,
            request.Country,
            request.City,
            request.District,
            null, // neighborhood kaldırıldı
            request.ReferralSourceId,
            request.ReferralPerson,
            request.AgreementInstitutionId,
            request.InsuranceInstitutionId,
            request.CitizenshipTypeId,
            request.Notes,
            request.SmsOptIn,
            request.CampaignOptIn);

        // TC Kimlik No güncelle (girilmişse)
        if (!string.IsNullOrWhiteSpace(request.TcNumber))
        {
            var tc = request.TcNumber.Trim();
            var hash = _encryption.HashSha256(tc);

            // Başka bir hastada aynı TC hash var mı?
            var duplicate = await _db.Patients
                .AnyAsync(p => p.TcNumberHash == hash && p.PublicId != request.PublicId,
                    cancellationToken);
            if (duplicate)
                throw new ConflictException("Bu TC Kimlik No başka bir hastaya ait.");

            patient.UpdateTcNumber(_encryption.Encrypt(tc), hash);
        }

        // Pasaport No güncelle (girilmişse)
        if (!string.IsNullOrWhiteSpace(request.PassportNo))
            patient.UpdatePassport(_encryption.Encrypt(request.PassportNo.Trim()));

        await _db.SaveChangesAsync(cancellationToken);
        return PatientMappings.ToResponse(patient);
    }

    private void EnsureTenantAccess(SharedKernel.Entities.Patient patient)
    {
        if (_tenantContext.IsPlatformAdmin) return;

        if (_tenantContext.IsBranchLevel && patient.BranchId != _tenantContext.BranchId)
            throw new ForbiddenException("Bu hastaya erişim yetkiniz bulunmuyor.");
    }
}
