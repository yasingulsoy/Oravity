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
    string? PreferredLanguageCode
) : IRequest<PatientResponse>;

public class UpdatePatientCommandHandler : IRequestHandler<UpdatePatientCommand, PatientResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public UpdatePatientCommandHandler(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
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
            request.PreferredLanguageCode);

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
