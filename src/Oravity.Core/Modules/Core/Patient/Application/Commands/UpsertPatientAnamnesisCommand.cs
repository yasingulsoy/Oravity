using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Patient.Application;
using Oravity.Core.Modules.Core.Patient.Application.Queries;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Patient.Application.Commands;

public record UpsertPatientAnamnesisCommand(
    Guid PatientPublicId,
    PatientAnamnesisData Data
) : IRequest<PatientAnamnesisResponse>;

public class UpsertPatientAnamnesisCommandHandler
    : IRequestHandler<UpsertPatientAnamnesisCommand, PatientAnamnesisResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public UpsertPatientAnamnesisCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<PatientAnamnesisResponse> Handle(UpsertPatientAnamnesisCommand request, CancellationToken ct)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.PublicId == request.PatientPublicId && !p.IsDeleted, ct)
            ?? throw new NotFoundException($"Hasta bulunamadı: {request.PatientPublicId}");

        var anamnesis = await _db.PatientAnamneses
            .Where(a => !a.IsDeleted && a.PatientId == patient.Id)
            .OrderByDescending(a => a.FilledAt)
            .FirstOrDefaultAsync(ct);

        if (anamnesis == null)
        {
            anamnesis = PatientAnamnesis.Create(patient.Id, patient.BranchId, _tenant.UserId);
            _db.PatientAnamneses.Add(anamnesis);
        }

        anamnesis.Update(request.Data, _tenant.UserId);
        await _db.SaveChangesAsync(ct);

        return GetPatientAnamnesisQueryHandler.Map(anamnesis);
    }
}
