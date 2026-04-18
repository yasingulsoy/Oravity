using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Commands;

public record AllocationApprovalResponse(
    Guid PublicId,
    long Id,
    long PatientId,
    long BranchId,
    long TreatmentPlanItemId,
    AllocationSource Source,
    long? PaymentId,
    long? InstitutionPaymentId,
    decimal RequestedAmount,
    AllocationApprovalStatus Status,
    string StatusLabel,
    long RequestedByUserId,
    string? RequestNotes,
    long? ApprovedByUserId,
    DateTime? ApprovalDate,
    string? ApprovalNotes,
    string? RejectionReason,
    long? PaymentAllocationId,
    DateTime CreatedAt
);

public static class AllocationApprovalMappings
{
    public static AllocationApprovalResponse ToResponse(AllocationApproval a) => new(
        a.PublicId, a.Id, a.PatientId, a.BranchId, a.TreatmentPlanItemId,
        a.Source, a.PaymentId, a.InstitutionPaymentId,
        a.RequestedAmount, a.Status, StatusLabel(a.Status),
        a.RequestedByUserId, a.RequestNotes,
        a.ApprovedByUserId, a.ApprovalDate, a.ApprovalNotes, a.RejectionReason,
        a.PaymentAllocationId, a.CreatedAt);

    public static string StatusLabel(AllocationApprovalStatus s) => s switch
    {
        AllocationApprovalStatus.Pending   => "Bekliyor",
        AllocationApprovalStatus.Approved  => "Onaylandı",
        AllocationApprovalStatus.Rejected  => "Reddedildi",
        AllocationApprovalStatus.Cancelled => "İptal",
        _ => s.ToString()
    };
}

public record RequestManualAllocationCommand(
    Guid PaymentPublicId,
    long TreatmentPlanItemId,
    decimal Amount,
    string? Notes,
    AllocationSource Source = AllocationSource.Patient
) : IRequest<AllocationApprovalResponse>;

public class RequestManualAllocationCommandHandler
    : IRequestHandler<RequestManualAllocationCommand, AllocationApprovalResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public RequestManualAllocationCommandHandler(
        AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    public async Task<AllocationApprovalResponse> Handle(
        RequestManualAllocationCommand r, CancellationToken ct)
    {
        if (r.Amount <= 0)
            throw new InvalidOperationException("Tutar sıfırdan büyük olmalıdır.");
        if (!_user.IsAuthenticated)
            throw new ForbiddenException("Talep için giriş yapmanız gerekir.");

        long patientId;
        long branchId;
        long? paymentId = null;
        long? institutionPaymentId = null;

        if (r.Source == AllocationSource.Patient)
        {
            var payment = await _db.Payments.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PublicId == r.PaymentPublicId, ct)
                ?? throw new NotFoundException($"Ödeme bulunamadı: {r.PaymentPublicId}");
            if (payment.IsRefunded)
                throw new InvalidOperationException("İade edilmiş ödeme dağıtılamaz.");
            if (_tenant.IsBranchLevel && payment.BranchId != _tenant.BranchId)
                throw new ForbiddenException("Bu ödemeye erişim yetkiniz yok.");
            patientId = payment.PatientId;
            branchId = payment.BranchId;
            paymentId = payment.Id;
        }
        else
        {
            var ip = await _db.InstitutionPayments.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PublicId == r.PaymentPublicId, ct)
                ?? throw new NotFoundException($"Kurum ödemesi bulunamadı: {r.PaymentPublicId}");
            var invoice = await _db.InstitutionInvoices.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == ip.InvoiceId, ct)
                ?? throw new NotFoundException("İlgili fatura bulunamadı.");
            if (_tenant.IsBranchLevel && invoice.BranchId != _tenant.BranchId)
                throw new ForbiddenException("Bu ödemeye erişim yetkiniz yok.");
            patientId = ip.PatientId;
            branchId = invoice.BranchId;
            institutionPaymentId = ip.Id;
        }

        var itemExists = await _db.TreatmentPlanItems.AsNoTracking()
            .AnyAsync(i => i.Id == r.TreatmentPlanItemId && i.Plan.PatientId == patientId, ct);
        if (!itemExists)
            throw new NotFoundException($"Tedavi kalemi bulunamadı: {r.TreatmentPlanItemId}");

        var approval = AllocationApproval.Create(
            patientId, branchId, r.TreatmentPlanItemId, r.Source,
            paymentId, institutionPaymentId, r.Amount,
            _user.UserId, r.Notes);
        approval.SetCreatedBy(_user.UserId, _user.TenantId);

        _db.AllocationApprovals.Add(approval);
        await _db.SaveChangesAsync(ct);

        return AllocationApprovalMappings.ToResponse(approval);
    }
}
