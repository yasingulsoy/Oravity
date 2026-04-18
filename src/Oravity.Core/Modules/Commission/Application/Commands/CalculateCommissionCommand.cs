using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Commission.Infrastructure;
using Oravity.Core.Modules.Finance.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Commission.Application.Commands;

/// <summary>
/// Tamamlanan tedavi için hekim hakediş kaydı oluşturur.
/// Atanmış şablon varsa kesinti zinciri ile hesaplanır; yoksa basit %30 default.
/// Aynı kalem için Pending kayıt varsa yeniden hesaplanıp güncellenir.
/// </summary>
public record CalculateCommissionCommand(long TreatmentPlanItemId)
    : IRequest<DoctorCommissionResponse>;

public class CalculateCommissionCommandHandler
    : IRequestHandler<CalculateCommissionCommand, DoctorCommissionResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICommissionCalculator _calculator;

    public CalculateCommissionCommandHandler(
        AppDbContext db, ITenantContext tenant, ICommissionCalculator calculator)
    {
        _db = db;
        _tenant = tenant;
        _calculator = calculator;
    }

    public async Task<DoctorCommissionResponse> Handle(
        CalculateCommissionCommand r, CancellationToken ct)
    {
        var item = await _db.TreatmentPlanItems.AsNoTracking()
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(i => i.Id == r.TreatmentPlanItemId, ct)
            ?? throw new NotFoundException($"Tedavi kalemi bulunamadı: {r.TreatmentPlanItemId}");

        if (item.Status != TreatmentItemStatus.Completed)
            throw new InvalidOperationException("Hakediş yalnızca tamamlanmış tedaviler için hesaplanabilir.");

        if (_tenant.IsBranchLevel && item.Plan.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu tedaviye erişim yetkiniz bulunmuyor.");

        var calc = await _calculator.CalculateAsync(r.TreatmentPlanItemId, ct);

        var existing = await _db.DoctorCommissions
            .FirstOrDefaultAsync(c =>
                c.TreatmentPlanItemId == r.TreatmentPlanItemId &&
                c.Status == CommissionStatus.Pending, ct);

        if (existing != null)
            _db.DoctorCommissions.Remove(existing);

        var commission = DoctorCommission.CreateCalculated(calc);
        _db.DoctorCommissions.Add(commission);

        await _db.SaveChangesAsync(ct);
        return FinanceMappings.ToResponse(commission);
    }
}
