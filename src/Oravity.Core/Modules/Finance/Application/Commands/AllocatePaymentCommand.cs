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
    IReadOnlyList<AllocationItem> Allocations,
    AllocationMethod Method = AllocationMethod.Automatic,
    string? Notes = null
) : IRequest<IReadOnlyList<PaymentAllocationResponse>>;

public class AllocatePaymentCommandHandler
    : IRequestHandler<AllocatePaymentCommand, IReadOnlyList<PaymentAllocationResponse>>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public AllocatePaymentCommandHandler(AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
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

        // Kalan (dağıtılmamış) tutarı kontrol et
        var alreadyAllocated = await _db.PaymentAllocations
            .Where(a => a.PaymentId == payment.Id && !a.IsRefunded)
            .SumAsync(a => (decimal?)a.AllocatedAmount, cancellationToken) ?? 0m;
        var remaining = payment.Amount - alreadyAllocated;

        var totalRequest = request.Allocations.Sum(a => a.Amount);
        if (totalRequest > remaining)
            throw new InvalidOperationException(
                $"Dağıtılacak toplam ({totalRequest:N2}) kalan dağıtılabilir tutarı ({remaining:N2}) aşıyor.");

        var userId = _user.IsAuthenticated ? _user.UserId : 0;

        var created = new List<PaymentAllocation>();
        foreach (var alloc in request.Allocations)
        {
            var item = PaymentAllocation.CreateFromPatient(
                paymentId:           payment.Id,
                treatmentPlanItemId: alloc.TreatmentPlanItemId,
                branchId:            payment.BranchId,
                allocatedAmount:     alloc.Amount,
                allocatedByUserId:   userId,
                method:              request.Method,
                approvalId:          null,
                notes:               request.Notes);
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
