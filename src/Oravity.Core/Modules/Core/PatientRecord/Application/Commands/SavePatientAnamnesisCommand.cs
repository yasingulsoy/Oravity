using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.PatientRecord.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientRecord.Application.Commands;

/// <summary>
/// Anamnez formu upsert — mevcut varsa günceller, yoksa oluşturur.
/// Kritik alanlar (alerji, antikoagülan vb.) hasta kartında kırmızı banner olarak gösterilir.
/// [RequirePermission("anamnesis:edit")] controller'da uygulanır.
/// </summary>
public record SavePatientAnamnesisCommand(
    long PatientId,
    PatientAnamnesisData Data
) : IRequest<PatientAnamnesisResponse>;

public class SavePatientAnamnesisCommandHandler
    : IRequestHandler<SavePatientAnamnesisCommand, PatientAnamnesisResponse>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly ITenantContext _tenant;

    public SavePatientAnamnesisCommandHandler(
        AppDbContext db, ICurrentUser user, ITenantContext tenant)
    {
        _db = db;
        _user = user;
        _tenant = tenant;
    }

    public async Task<PatientAnamnesisResponse> Handle(
        SavePatientAnamnesisCommand request,
        CancellationToken cancellationToken)
    {
        var branchId = _tenant.BranchId
            ?? throw new ForbiddenException("Bu işlem için şube bağlamı gereklidir.");

        var existing = await _db.PatientAnamneses
            .FirstOrDefaultAsync(a => a.PatientId == request.PatientId, cancellationToken);

        if (existing is null)
        {
            existing = PatientAnamnesis.Create(request.PatientId, branchId, _user.UserId);
            existing.Update(request.Data, _user.UserId);
            // FilledAt create'te set edildiğinden Update tekrar üzerine yazmasın;
            // Update metodu UpdatedBy/UpdatedByAt ayarlıyor, FilledBy/FilledAt dokunulmaz.
            _db.PatientAnamneses.Add(existing);
        }
        else
        {
            existing.Update(request.Data, _user.UserId);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return PatientRecordMappings.ToResponse(existing);
    }
}
