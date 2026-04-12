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
    PatientAnamnesisData Data,
    Guid? ProtocolPublicId = null
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

        long? protocolId = null;
        if (request.ProtocolPublicId.HasValue)
        {
            protocolId = await _db.Protocols
                .Where(p => p.PublicId == request.ProtocolPublicId.Value && !p.IsDeleted)
                .Select(p => (long?)p.Id)
                .FirstOrDefaultAsync(ct);
        }

        // Her kayıt yeni satır olarak eklenir — geçmiş saklanır.
        var anamnesis = PatientAnamnesis.Create(patient.Id, patient.BranchId, _tenant.UserId, protocolId);
        anamnesis.Update(request.Data, _tenant.UserId);
        _db.PatientAnamneses.Add(anamnesis);
        await _db.SaveChangesAsync(ct);

        // FilledByUser navigation yükle
        await _db.Entry(anamnesis).Reference(x => x.FilledByUser).LoadAsync(ct);

        return GetPatientAnamnesisQueryHandler.Map(anamnesis);
    }
}
