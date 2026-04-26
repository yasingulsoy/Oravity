using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Finance.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Commands;

public record CreatePaymentCommand(
    long PatientId,
    decimal Amount,
    PaymentMethod Method,
    DateOnly PaymentDate,
    string Currency = "TRY",
    decimal ExchangeRate = 1m,
    string? Notes = null
) : IRequest<PaymentResponse>;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, PaymentResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public CreatePaymentCommandHandler(
        AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    public async Task<PaymentResponse> Handle(
        CreatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        if (!_tenant.BranchId.HasValue && !_tenant.IsCompanyAdmin && !_tenant.IsPlatformAdmin)
            throw new ForbiddenException("Ödeme kaydı için şube bağlamı gereklidir.");

        var branchId = _tenant.BranchId
            ?? await _db.TreatmentPlans.AsNoTracking()
                .Where(p => p.PatientId == request.PatientId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => (long?)p.BranchId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Hastanın şube bilgisi belirlenemedi.");

        // ── Geçmiş tarih kontrolü ─────────────────────────────────────────
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.PaymentDate < today && !_user.HasPermission("payment.backdate"))
            throw new ForbiddenException("Geçmiş tarihli ödeme girmek için yetkiniz yok.");

        var payment = Payment.Create(
            patientId:    request.PatientId,
            branchId:     branchId,
            amount:       request.Amount,
            method:       request.Method,
            paymentDate:  request.PaymentDate,
            currency:     request.Currency,
            exchangeRate: request.Currency == "TRY" ? 1m : request.ExchangeRate,
            notes:        request.Notes);

        if (_user.IsAuthenticated)
            payment.SetCreatedBy(_user.UserId, _user.TenantId);

        _db.Payments.Add(payment);

        // Outbox: kritik event — ödeme alındı
        var payload = JsonSerializer.Serialize(new
        {
            payment.PublicId,
            payment.PatientId,
            payment.BranchId,
            payment.Amount,
            payment.Currency,
            Method = payment.Method.ToString(),
            payment.PaymentDate
        });
        _db.OutboxMessages.Add(OutboxMessage.Create("PaymentReceived", payload));

        await _db.SaveChangesAsync(cancellationToken);
        return FinanceMappings.ToResponse(payment);
    }
}
