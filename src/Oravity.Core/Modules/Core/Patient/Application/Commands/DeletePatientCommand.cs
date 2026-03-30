using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Patient.Application.Commands;

public record DeletePatientCommand(Guid PublicId) : IRequest;

public class DeletePatientCommandHandler : IRequestHandler<DeletePatientCommand>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public DeletePatientCommandHandler(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task Handle(DeletePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.PublicId == request.PublicId, cancellationToken)
            ?? throw new NotFoundException($"Hasta bulunamadı: {request.PublicId}");

        EnsureTenantAccess(patient);

        patient.SoftDelete();
        await _db.SaveChangesAsync(cancellationToken);
    }

    private void EnsureTenantAccess(SharedKernel.Entities.Patient patient)
    {
        if (_tenantContext.IsPlatformAdmin) return;

        if (_tenantContext.IsBranchLevel && patient.BranchId != _tenantContext.BranchId)
            throw new ForbiddenException("Bu hastayı silme yetkiniz bulunmuyor.");
    }
}
