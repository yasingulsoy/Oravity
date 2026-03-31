using System.Text.Json;
using MediatR;
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
        var branchId = _tenant.BranchId
            ?? throw new ForbiddenException("Ödeme kaydı için şube bağlamı gereklidir.");

        var payment = Payment.Create(
            patientId:   request.PatientId,
            branchId:    branchId,
            amount:      request.Amount,
            method:      request.Method,
            paymentDate: request.PaymentDate,
            currency:    request.Currency,
            notes:       request.Notes);

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
