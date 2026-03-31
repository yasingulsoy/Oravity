using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Finance.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Commands;

public record AllocationItem(long TreatmentPlanItemId, decimal Amount);

public record AllocatePaymentCommand(
    Guid PaymentPublicId,
    IReadOnlyList<AllocationItem> Allocations
) : IRequest<IReadOnlyList<PaymentAllocationResponse>>;

public class AllocatePaymentCommandHandler
    : IRequestHandler<AllocatePaymentCommand, IReadOnlyList<PaymentAllocationResponse>>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public AllocatePaymentCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<PaymentAllocationResponse>> Handle(
        AllocatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.PublicId == request.PaymentPublicId, cancellationToken)
            ?? throw new NotFoundException($"Ödeme bulunamadı: {request.PaymentPublicId}");

        EnsureTenantAccess(payment);

        if (payment.IsRefunded)
            throw new InvalidOperationException("İade edilmiş ödeme dağıtılamaz.");

        // Toplam dağıtım tutarı ödemeden fazla olamaz
        var totalRequest = request.Allocations.Sum(a => a.Amount);
        if (totalRequest > payment.Amount)
            throw new InvalidOperationException(
                $"Dağıtılacak toplam ({totalRequest:N2}) ödeme tutarını ({payment.Amount:N2}) aşıyor.");

        var created = new List<PaymentAllocation>();
        foreach (var alloc in request.Allocations)
        {
            var item = PaymentAllocation.Create(
                paymentId:           payment.Id,
                treatmentPlanItemId: alloc.TreatmentPlanItemId,
                allocatedAmount:     alloc.Amount);
            _db.PaymentAllocations.Add(item);
            created.Add(item);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return created.Select(FinanceMappings.ToResponse).ToList();
    }

    private void EnsureTenantAccess(Payment payment)
    {
        if (_tenant.IsPlatformAdmin) return;
        if (_tenant.IsBranchLevel && payment.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu ödemeye erişim yetkiniz bulunmuyor.");
    }
}
