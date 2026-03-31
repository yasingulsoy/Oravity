using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Finance.Application;
using Oravity.Infrastructure.Database;
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

        payment.Refund();
        if (!string.IsNullOrWhiteSpace(request.Reason))
            payment.UpdateNotes(request.Reason);

        // İlgili allocation'ları iade olarak işaretle
        foreach (var alloc in payment.Allocations.Where(a => !a.IsRefunded))
            alloc.MarkRefunded();

        await _db.SaveChangesAsync(cancellationToken);
        return FinanceMappings.ToResponse(payment);
    }
}
