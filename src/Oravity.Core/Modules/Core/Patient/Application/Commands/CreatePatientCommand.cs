using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Patient.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using PatientEntity = Oravity.SharedKernel.Entities.Patient;

namespace Oravity.Core.Modules.Core.Patient.Application.Commands;

public record CreatePatientCommand(
    string FirstName,
    string LastName,
    string? Phone,
    string? Email,
    DateOnly? BirthDate,
    string? Gender,
    string? TcNumber,
    string? Address,
    string? BloodType,
    string? PreferredLanguageCode
) : IRequest<PatientResponse>;

public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, PatientResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IEncryptionService _encryption;

    public CreatePatientCommandHandler(
        AppDbContext db,
        ITenantContext tenantContext,
        IEncryptionService encryption)
    {
        _db = db;
        _tenantContext = tenantContext;
        _encryption = encryption;
    }

    public async Task<PatientResponse> Handle(
        CreatePatientCommand request,
        CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsAuthenticated)
            throw new UnauthorizedException();

        var branchId = _tenantContext.BranchId;

        if (branchId is null && _tenantContext.IsCompanyAdmin && _tenantContext.CompanyId.HasValue)
        {
            branchId = await _db.Branches
                .Where(b => b.CompanyId == _tenantContext.CompanyId.Value && b.IsActive)
                .OrderBy(b => b.Id)
                .Select(b => (long?)b.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (branchId is null)
            throw new ForbiddenException("Hasta kaydı için şube bağlamı gereklidir.");

        // TC Kimlik No işle
        string? tcEncrypted = null;
        string? tcHash = null;

        if (!string.IsNullOrWhiteSpace(request.TcNumber))
        {
            var tc = request.TcNumber.Trim();
            tcHash = _encryption.HashSha256(tc);
            tcEncrypted = _encryption.Encrypt(tc);

            // Aynı şubede TC hash çakışması kontrolü
            var exists = await _db.Patients
                .AnyAsync(p => p.BranchId == branchId && p.TcNumberHash == tcHash,
                    cancellationToken);
            if (exists)
                throw new ConflictException("Bu TC Kimlik No ile kayıtlı hasta zaten mevcut.");
        }

        var patient = PatientEntity.Create(
            branchId: branchId.Value,
            firstName: request.FirstName,
            lastName: request.LastName,
            phone: request.Phone,
            email: request.Email,
            birthDate: request.BirthDate,
            gender: request.Gender,
            tcNumberEncrypted: tcEncrypted,
            tcNumberHash: tcHash,
            address: request.Address,
            bloodType: request.BloodType,
            preferredLanguageCode: request.PreferredLanguageCode);

        _db.Patients.Add(patient);

        // Outbox: PatientCreated event
        var payload = JsonSerializer.Serialize(new
        {
            patient.PublicId,
            patient.BranchId,
            FullName = $"{patient.FirstName} {patient.LastName}",
            patient.CreatedAt
        });
        _db.OutboxMessages.Add(OutboxMessage.Create("PatientCreated", payload));

        await _db.SaveChangesAsync(cancellationToken);

        return PatientMappings.ToResponse(patient);
    }
}
