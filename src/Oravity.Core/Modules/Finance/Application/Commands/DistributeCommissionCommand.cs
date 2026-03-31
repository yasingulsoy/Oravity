using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Finance.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Commands;

/// <summary>
/// Tamamlanmış tedavi için hekim hakediş hesaplar ve dağıtır.
/// Varsa mevcut kaydı dağıtır, yoksa commission_rate ile yeni oluşturur.
/// İzin: commission:distribute
/// </summary>
public record DistributeCommissionCommand(
    long TreatmentPlanItemId,
    /// <summary>0–100 arası hakediş oranı. Yeni oluşturmada kullanılır.</summary>
    decimal CommissionRate
) : IRequest<DoctorCommissionResponse>;

public class DistributeCommissionCommandHandler
    : IRequestHandler<DistributeCommissionCommand, DoctorCommissionResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public DistributeCommissionCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<DoctorCommissionResponse> Handle(
        DistributeCommissionCommand request,
        CancellationToken cancellationToken)
    {
        var item = await _db.TreatmentPlanItems
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(i => i.Id == request.TreatmentPlanItemId, cancellationToken)
            ?? throw new NotFoundException($"Tedavi kalemi bulunamadı: {request.TreatmentPlanItemId}");

        if (item.Status != TreatmentItemStatus.Completed)
            throw new InvalidOperationException("Hakediş yalnızca tamamlanmış tedaviler için hesaplanabilir.");

        if (_tenant.IsBranchLevel && item.Plan.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu tedaviye erişim yetkiniz bulunmuyor.");

        var doctorId = item.DoctorId ?? item.Plan.DoctorId;

        // Mevcut Pending kaydı var mı?
        var commission = await _db.DoctorCommissions
            .FirstOrDefaultAsync(c =>
                c.TreatmentPlanItemId == request.TreatmentPlanItemId &&
                c.Status == CommissionStatus.Pending,
                cancellationToken);

        if (commission is null)
        {
            // Yeni hakediş kaydı oluştur
            commission = DoctorCommission.Create(
                doctorId:            doctorId,
                treatmentPlanItemId: request.TreatmentPlanItemId,
                branchId:            item.Plan.BranchId,
                grossAmount:         item.FinalPrice,
                commissionRate:      request.CommissionRate);
            _db.DoctorCommissions.Add(commission);
        }

        commission.Distribute();
        await _db.SaveChangesAsync(cancellationToken);

        return FinanceMappings.ToResponse(commission);
    }
}
