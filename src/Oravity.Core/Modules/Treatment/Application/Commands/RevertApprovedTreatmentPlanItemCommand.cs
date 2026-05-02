using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Treatment.Application.Commands;

/// <summary>
/// Onaylanmış tedavi kalemini tekrar 'Planlandı' durumuna geri alır.
///
/// Kurallar:
///   1. Kalem 'Onaylandı' durumunda olmalı.
///   2. İmzalı onam formu varsa geri alınamaz — önce onam iptal edilmeli.
///   3. İzin: treatment_plan.edit
/// </summary>
public record RevertApprovedTreatmentPlanItemCommand(Guid ItemPublicId) : IRequest<TreatmentPlanItemResponse>;

public class RevertApprovedTreatmentPlanItemCommandHandler
    : IRequestHandler<RevertApprovedTreatmentPlanItemCommand, TreatmentPlanItemResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public RevertApprovedTreatmentPlanItemCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<TreatmentPlanItemResponse> Handle(
        RevertApprovedTreatmentPlanItemCommand request,
        CancellationToken cancellationToken)
    {
        var item = await _db.TreatmentPlanItems
            .Include(i => i.Plan)
            .Include(i => i.Treatment)
            .FirstOrDefaultAsync(i => i.PublicId == request.ItemPublicId, cancellationToken)
            ?? throw new NotFoundException($"Tedavi kalemi bulunamadı: {request.ItemPublicId}");

        if (_tenant.IsBranchLevel && item.Plan.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu tedavi kalemine erişim yetkiniz bulunmuyor.");

        // İmzalı onam kontrolü
        // Not: ItemPublicIdsJson jsonb kolonu olduğu için LIKE kullanamayız; in-memory kontrol yapıyoruz.
        var itemIdStr = request.ItemPublicId.ToString();
        var signedJsons = await _db.ConsentInstances
            .AsNoTracking()
            .Where(ci => ci.Status == ConsentInstanceStatus.Signed && ci.TreatmentPlanId == item.PlanId)
            .Select(ci => ci.ItemPublicIdsJson)
            .ToListAsync(cancellationToken);

        if (signedJsons.Any(json => json.Contains(itemIdStr)))
            throw new ConflictException(
                "Bu tedavi için imzalı onam formu mevcut. " +
                "Plana geri göndermeden önce onam formunu iptal edin.");

        item.RevertToPlanned();
        await _db.SaveChangesAsync(cancellationToken);

        return TreatmentPlanMappings.ToResponse(item);
    }
}
