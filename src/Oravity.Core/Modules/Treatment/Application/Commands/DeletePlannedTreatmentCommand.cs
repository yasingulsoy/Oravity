using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Treatment.Application.Commands;

/// <summary>
/// Planlanmış (status=1) tedavi kalemini siler.
/// Tamamlanmış (status=3) kalem silinemez.
/// İzin: treatment_plan:delete_planned
/// </summary>
public record DeletePlannedTreatmentCommand(Guid ItemPublicId) : IRequest;

public class DeletePlannedTreatmentCommandHandler : IRequestHandler<DeletePlannedTreatmentCommand>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public DeletePlannedTreatmentCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task Handle(DeletePlannedTreatmentCommand request, CancellationToken cancellationToken)
    {
        var item = await _db.TreatmentPlanItems
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(i => i.PublicId == request.ItemPublicId, cancellationToken)
            ?? throw new NotFoundException($"Tedavi kalemi bulunamadı: {request.ItemPublicId}");

        EnsureTenantAccess(item.Plan);

        if (item.Status == TreatmentItemStatus.Completed)
            throw new ForbiddenException("Tamamlanmış tedavi kalemi silinemez.");

        if (item.Status != TreatmentItemStatus.Planned)
            throw new InvalidOperationException(
                "Yalnızca planlanmış (status=1) tedavi kalemleri silinebilir.");

        await TreatmentItemFinancialGuard.AssertCanBeDeletedAsync(item.Id, _db, cancellationToken);

        item.SoftDelete();
        await _db.SaveChangesAsync(cancellationToken);
    }

    private void EnsureTenantAccess(TreatmentPlan plan)
    {
        if (_tenant.IsPlatformAdmin) return;
        if (_tenant.IsBranchLevel && plan.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu tedavi kalemine erişim yetkiniz bulunmuyor.");
    }
}
