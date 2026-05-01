using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.InstitutionInvoice.Application.Commands;

public record RegisterInstitutionPaymentCommand(
    Guid InvoicePublicId,
    decimal Amount,
    DateOnly PaymentDate,
    InstitutionPaymentMethod Method,
    string? ReferenceNo,
    string? Notes,
    string Currency = "TRY",
    string? BankAccountPublicId = null
) : IRequest<InstitutionPaymentResponse>;

public class RegisterInstitutionPaymentCommandHandler
    : IRequestHandler<RegisterInstitutionPaymentCommand, InstitutionPaymentResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public RegisterInstitutionPaymentCommandHandler(
        AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    public async Task<InstitutionPaymentResponse> Handle(
        RegisterInstitutionPaymentCommand r, CancellationToken ct)
    {
        var invoice = await _db.InstitutionInvoices
            .FirstOrDefaultAsync(i => i.PublicId == r.InvoicePublicId, ct)
            ?? throw new NotFoundException($"Fatura bulunamadı: {r.InvoicePublicId}");

        if (_tenant.IsBranchLevel && invoice.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu faturaya erişim yetkiniz bulunmuyor.");

        if (invoice.Status is InstitutionInvoiceStatus.Paid)
            throw new InvalidOperationException("Fatura zaten tam ödendi.");
        if (invoice.Status is InstitutionInvoiceStatus.Rejected)
            throw new InvalidOperationException("Reddedilmiş faturaya ödeme alınamaz.");

        var remaining = invoice.NetPayableAmount - invoice.PaidAmount;
        if (r.Amount > remaining + 0.01m)
            throw new InvalidOperationException(
                $"Ödeme tutarı ({r.Amount:N2}) kalan tutarı ({remaining:N2}) aşıyor.");

        var payment = InstitutionPayment.Create(
            invoice.Id, invoice.PatientId, invoice.InstitutionId,
            r.Amount, r.Currency, r.PaymentDate, r.Method, r.ReferenceNo, r.Notes, r.BankAccountPublicId);

        if (_user.IsAuthenticated)
            payment.SetCreatedBy(_user.UserId, _user.TenantId);

        _db.InstitutionPayments.Add(payment);

        invoice.RegisterPayment(r.Amount, r.PaymentDate, r.ReferenceNo);
        if (_user.IsAuthenticated)
            invoice.SetUpdatedBy(_user.UserId);

        await _db.SaveChangesAsync(ct);
        return InstitutionInvoiceMappings.ToResponse(payment);
    }
}
