using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Treatment.Application.Commands;

/// <summary>
/// Seçili kalemleri Onaylandı (Approved) yapar.
/// Plan durumunu değiştirmez — plan Taslak kalabilir.
/// </summary>
public record ApproveItemsCommand(
    Guid       PlanPublicId,
    List<Guid> ItemPublicIds
) : IRequest<TreatmentPlanResponse>;

public class ApproveItemsCommandHandler
    : IRequestHandler<ApproveItemsCommand, TreatmentPlanResponse>
{
    private readonly AppDbContext  _db;
    private readonly ITenantContext _tenant;

    public ApproveItemsCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<TreatmentPlanResponse> Handle(
        ApproveItemsCommand request,
        CancellationToken   cancellationToken)
    {
        var plan = await _db.TreatmentPlans
            .Include(p => p.Items)
                .ThenInclude(i => i.Treatment)
            .FirstOrDefaultAsync(p => p.PublicId == request.PlanPublicId, cancellationToken)
            ?? throw new NotFoundException($"Tedavi planı bulunamadı: {request.PlanPublicId}");

        if (_tenant.IsBranchLevel && plan.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu tedavi planına erişim yetkiniz bulunmuyor.");

        var targetIds = request.ItemPublicIds.ToHashSet();
        var targets   = plan.Items.Where(i => targetIds.Contains(i.PublicId)).ToList();

        if (targets.Count == 0)
            throw new NotFoundException("Belirtilen kalemler bulunamadı.");

        foreach (var item in targets)
            item.Approve(_tenant.UserId);

        await _db.SaveChangesAsync(cancellationToken);

        return TreatmentPlanMappings.ToResponse(plan);
    }
}
