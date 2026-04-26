using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Finance.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Commands;

/// <summary>
/// Ödeme iadesi.
/// Orijinal ödeme IsRefunded=true olur, tüm allocations MarkRefunded olur.
/// İzin: payment:refund
/// </summary>
public record RefundPaymentCommand(Guid PaymentPublicId, string? Reason = null) : IRequest<PaymentResponse>;

public class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, PaymentResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public RefundPaymentCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaymentResponse> Handle(
        RefundPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await _db.Payments
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.PublicId == request.PaymentPublicId, cancellationToken)
            ?? throw new NotFoundException($"Ödeme bulunamadı: {request.PaymentPublicId}");

        if (_tenant.IsBranchLevel && payment.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu ödemeye erişim yetkiniz bulunmuyor.");

        if (payment.IsRefunded)
            throw new InvalidOperationException("Bu ödeme zaten iade edilmiş.");

        // ── Komisyon kontrolü ──────────────────────────────────────────────
        // Bu ödemenin dağıtıldığı tedavi kalemlerini bul
        var itemIds = payment.Allocations
            .Where(a => !a.IsRefunded)
            .Select(a => a.TreatmentPlanItemId)
            .Distinct()
            .ToList();

        if (itemIds.Count > 0)
        {
            var commissions = await _db.DoctorCommissions
                .Where(c => itemIds.Contains(c.TreatmentPlanItemId))
                .ToListAsync(cancellationToken);

            // Dağıtılmış komisyon varsa iade bloklanır
            var distributed = commissions
                .Where(c => c.Status == CommissionStatus.Distributed)
                .ToList();

            if (distributed.Count > 0)
            {
                var doctorIds = distributed.Select(c => c.DoctorId).Distinct();
                throw new InvalidOperationException(
                    $"Bu ödemeye bağlı {distributed.Count} adet dağıtılmış hekim hakediş kaydı var. " +
                    $"İade yapılabilmesi için önce ilgili hekimlerin hakediş kayıtları iptal edilmelidir. " +
                    $"Etkilenen kalem sayısı: {itemIds.Count}");
            }

            // Bekleyen komisyonları iptal et
            foreach (var c in commissions.Where(c => c.Status == CommissionStatus.Pending))
                c.Cancel();
        }

        // ── Ödeme ve allocation'ları iade et ──────────────────────────────
        payment.Refund();
        if (!string.IsNullOrWhiteSpace(request.Reason))
            payment.UpdateNotes(request.Reason);

        foreach (var alloc in payment.Allocations.Where(a => !a.IsRefunded))
            alloc.MarkRefunded();

        await _db.SaveChangesAsync(cancellationToken);
        return FinanceMappings.ToResponse(payment);
    }
}
