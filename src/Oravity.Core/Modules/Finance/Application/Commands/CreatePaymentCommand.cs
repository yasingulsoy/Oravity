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
    string Currency      = "TRY",
    decimal ExchangeRate = 1m,
    string? Notes        = null,
    Guid? PosTerminalId  = null,
    Guid? BankAccountId  = null
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

        long? posTerminalId = null;
        if (request.PosTerminalId.HasValue)
            posTerminalId = await _db.PosTerminals.AsNoTracking()
                .Where(p => p.PublicId == request.PosTerminalId.Value && !p.IsDeleted)
                .Select(p => (long?)p.Id).FirstOrDefaultAsync(cancellationToken);

        long? bankAccountId = null;
        if (request.BankAccountId.HasValue)
            bankAccountId = await _db.BankAccounts.AsNoTracking()
                .Where(b => b.PublicId == request.BankAccountId.Value && !b.IsDeleted)
                .Select(b => (long?)b.Id).FirstOrDefaultAsync(cancellationToken);

        var payment = Payment.Create(
            patientId:    request.PatientId,
            branchId:     branchId,
            amount:       request.Amount,
            method:       request.Method,
            paymentDate:  request.PaymentDate,
            currency:     request.Currency,
            exchangeRate: request.Currency == "TRY" ? 1m : request.ExchangeRate,
            notes:        request.Notes,
            posTerminalId: posTerminalId,
            bankAccountId: bankAccountId);

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
