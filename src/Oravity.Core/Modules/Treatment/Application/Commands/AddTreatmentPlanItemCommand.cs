using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Treatment.Application.Commands;

public record AddTreatmentPlanItemCommand(
    Guid PlanPublicId,
    long TreatmentId,
    decimal UnitPrice,
    decimal DiscountRate,
    string? ToothNumber,
    string? ToothSurfaces,
    string? BodyRegionCode,
    long? DoctorId,
    string? Notes,
    string PriceCurrency = "TRY",
    decimal PriceExchangeRate = 1m,
    decimal? ListPrice = null
) : IRequest<TreatmentPlanItemResponse>;

public class AddTreatmentPlanItemCommandHandler
    : IRequestHandler<AddTreatmentPlanItemCommand, TreatmentPlanItemResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public AddTreatmentPlanItemCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<TreatmentPlanItemResponse> Handle(
        AddTreatmentPlanItemCommand request,
        CancellationToken cancellationToken)
    {
        var plan = await _db.TreatmentPlans
            .Include(p => p.Patient)
            .FirstOrDefaultAsync(p => p.PublicId == request.PlanPublicId, cancellationToken)
            ?? throw new NotFoundException($"Tedavi planı bulunamadı: {request.PlanPublicId}");

        EnsureTenantAccess(plan);

        // Onaylanmış veya tamamlanmış plana yeni kalem eklenemez
        if (plan.Status is TreatmentPlanStatus.Approved
                         or TreatmentPlanStatus.Completed
                         or TreatmentPlanStatus.Cancelled)
            throw new InvalidOperationException(
                "Onaylanmış, tamamlanmış veya iptal edilmiş plana kalem eklenemez.");

        var treatment = await _db.Treatments.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TreatmentId, cancellationToken)
            ?? throw new NotFoundException("Tedavi bulunamadı.");

        // Kurum: hastanın o anki AgreementInstitutionId — plan oluşturulurken değil,
        // kalem eklenirken snapshot alınır; kurum değişse eski plan doğru kalır.
        var institutionId = plan.Patient?.AgreementInstitutionId;

        var item = TreatmentPlanItem.Create(
            planId:            plan.Id,
            treatmentId:       request.TreatmentId,
            unitPrice:         request.UnitPrice,
            kdvRate:           treatment.KdvRate,
            discountRate:      request.DiscountRate,
            toothNumber:       request.ToothNumber,
            toothSurfaces:     request.ToothSurfaces,
            bodyRegionCode:    request.BodyRegionCode,
            doctorId:          request.DoctorId ?? plan.DoctorId,
            notes:             request.Notes,
            priceCurrency:     request.PriceCurrency,
            priceExchangeRate: request.PriceExchangeRate,
            listPrice:         request.ListPrice,
            institutionId:     institutionId);

        _db.TreatmentPlanItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        // Treatment adını response'a eklemek için reload
        var loaded = await _db.TreatmentPlanItems
            .AsNoTracking()
            .Include(i => i.Treatment)
            .Include(i => i.Doctor)
            .Include(i => i.Plan).ThenInclude(p => p.Doctor)
            .FirstAsync(i => i.Id == item.Id, cancellationToken);

        return TreatmentPlanMappings.ToResponse(loaded);
    }

    private void EnsureTenantAccess(TreatmentPlan plan)
    {
        if (_tenant.IsPlatformAdmin) return;
        if (_tenant.IsBranchLevel && plan.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu tedavi planına erişim yetkiniz bulunmuyor.");
    }
}
