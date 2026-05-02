using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Treatment.Application.Commands;

/// <summary>
/// Taslak planı onaylar → plan.Status=Approved, tüm Planned item'lar Approved olur.
/// </summary>
public record ApproveTreatmentPlanCommand(Guid PlanPublicId) : IRequest<TreatmentPlanResponse>;

public class ApproveTreatmentPlanCommandHandler
    : IRequestHandler<ApproveTreatmentPlanCommand, TreatmentPlanResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public ApproveTreatmentPlanCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<TreatmentPlanResponse> Handle(
        ApproveTreatmentPlanCommand request,
        CancellationToken cancellationToken)
    {
        var plan = await _db.TreatmentPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.PublicId == request.PlanPublicId, cancellationToken)
            ?? throw new NotFoundException($"Tedavi planı bulunamadı: {request.PlanPublicId}");

        EnsureTenantAccess(plan);

        // Domain içinde durum geçişi ve item güncellemeleri yapılır
        plan.Approve(_tenant.UserId);

        await _db.SaveChangesAsync(cancellationToken);

        return TreatmentPlanMappings.ToResponse(plan);
    }

    private void EnsureTenantAccess(TreatmentPlan plan)
    {
        if (_tenant.IsPlatformAdmin) return;
        if (_tenant.IsBranchLevel && plan.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu tedavi planına erişim yetkiniz bulunmuyor.");
    }
}
