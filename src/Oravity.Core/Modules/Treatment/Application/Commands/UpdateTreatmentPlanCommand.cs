using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Treatment.Application.Commands;

// ── Plan güncelle ─────────────────────────────────────────────────────────────

public record UpdateTreatmentPlanCommand(
    Guid    PlanPublicId,
    string  Name,
    string? Notes,
    long?   InstitutionId = null
) : IRequest<TreatmentPlanResponse>;

public class UpdateTreatmentPlanCommandHandler
    : IRequestHandler<UpdateTreatmentPlanCommand, TreatmentPlanResponse>
{
    private readonly AppDbContext  _db;
    private readonly ITenantContext _tenant;

    public UpdateTreatmentPlanCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<TreatmentPlanResponse> Handle(
        UpdateTreatmentPlanCommand request,
        CancellationToken cancellationToken)
    {
        var plan = await _db.TreatmentPlans
            .Include(p => p.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Treatment)
            .Include(p => p.Doctor)
            .Include(p => p.Branch)
            .Include(p => p.Institution)
            .FirstOrDefaultAsync(p => p.PublicId == request.PlanPublicId, cancellationToken)
            ?? throw new NotFoundException("Tedavi planı bulunamadı.");

        plan.Update(request.Name, request.Notes);
        plan.SetInstitution(request.InstitutionId);
        await _db.SaveChangesAsync(cancellationToken);

        return TreatmentPlanMappings.ToResponse(plan);
    }
}

// ── Plan sil (soft-delete veya Taslak'ı iptal et) ────────────────────────────

public record DeleteTreatmentPlanCommand(Guid PlanPublicId) : IRequest;

public class DeleteTreatmentPlanCommandHandler
    : IRequestHandler<DeleteTreatmentPlanCommand>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public DeleteTreatmentPlanCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task Handle(
        DeleteTreatmentPlanCommand request,
        CancellationToken cancellationToken)
    {
        var plan = await _db.TreatmentPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.PublicId == request.PlanPublicId, cancellationToken)
            ?? throw new NotFoundException("Tedavi planı bulunamadı.");

        if (plan.Status == SharedKernel.Entities.TreatmentPlanStatus.Completed)
            throw new InvalidOperationException("Tamamlanmış plan silinemez.");

        // Taslak → soft-delete; diğerleri → iptal
        if (plan.Status == SharedKernel.Entities.TreatmentPlanStatus.Draft)
            plan.SoftDelete();
        else
            plan.Cancel();

        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ── Kalem fiyatını güncelle ───────────────────────────────────────────────────

public record UpdateTreatmentPlanItemCommand(
    Guid    ItemPublicId,
    decimal UnitPrice,
    decimal DiscountRate,
    string? ToothNumber
) : IRequest<TreatmentPlanItemResponse>;

public class UpdateTreatmentPlanItemCommandHandler
    : IRequestHandler<UpdateTreatmentPlanItemCommand, TreatmentPlanItemResponse>
{
    private readonly AppDbContext _db;

    public UpdateTreatmentPlanItemCommandHandler(AppDbContext db) => _db = db;

    public async Task<TreatmentPlanItemResponse> Handle(
        UpdateTreatmentPlanItemCommand request,
        CancellationToken cancellationToken)
    {
        var item = await _db.TreatmentPlanItems
            .Include(i => i.Treatment)
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(i => i.PublicId == request.ItemPublicId && !i.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tedavi kalemi bulunamadı.");

        if (item.UnitPrice != request.UnitPrice || item.DiscountRate != request.DiscountRate)
        {
            await TreatmentItemFinancialGuard.AssertPriceCanBeChangedAsync(item.Id, _db, cancellationToken);
            item.UpdatePrice(request.UnitPrice, request.DiscountRate);
        }

        item.UpdateToothNumber(request.ToothNumber);
        await _db.SaveChangesAsync(cancellationToken);

        return TreatmentPlanMappings.ToResponse(item);
    }
}
