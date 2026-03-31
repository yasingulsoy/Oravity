using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Treatment.Application.Commands;

/// <summary>
/// Tedavi kalemi tamamlandı olarak işaretlenir.
/// İzin: treatment_plan:complete
/// </summary>
public record CompleteTreatmentPlanItemCommand(Guid ItemPublicId) : IRequest<TreatmentPlanItemResponse>;

public class CompleteTreatmentPlanItemCommandHandler
    : IRequestHandler<CompleteTreatmentPlanItemCommand, TreatmentPlanItemResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public CompleteTreatmentPlanItemCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<TreatmentPlanItemResponse> Handle(
        CompleteTreatmentPlanItemCommand request,
        CancellationToken cancellationToken)
    {
        var item = await _db.TreatmentPlanItems
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(i => i.PublicId == request.ItemPublicId, cancellationToken)
            ?? throw new NotFoundException($"Tedavi kalemi bulunamadı: {request.ItemPublicId}");

        EnsureTenantAccess(item.Plan);

        item.Complete();
        await _db.SaveChangesAsync(cancellationToken);

        // Outbox: TreatmentItemCompleted event
        var payload = JsonSerializer.Serialize(new
        {
            item.PublicId,
            item.PlanId,
            item.TreatmentId,
            item.DoctorId,
            item.CompletedAt,
            item.FinalPrice
        });
        _db.OutboxMessages.Add(OutboxMessage.Create("TreatmentItemCompleted", payload));
        await _db.SaveChangesAsync(cancellationToken);

        return TreatmentPlanMappings.ToResponse(item);
    }

    private void EnsureTenantAccess(TreatmentPlan plan)
    {
        if (_tenant.IsPlatformAdmin) return;
        if (_tenant.IsBranchLevel && plan.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu tedavi kalemine erişim yetkiniz bulunmuyor.");
    }
}
