using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Commands;

public record ApproveAllocationCommand(
    Guid ApprovalPublicId,
    string? Notes
) : IRequest<AllocationApprovalResponse>;

public class ApproveAllocationCommandHandler
    : IRequestHandler<ApproveAllocationCommand, AllocationApprovalResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public ApproveAllocationCommandHandler(
        AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    public async Task<AllocationApprovalResponse> Handle(
        ApproveAllocationCommand r, CancellationToken ct)
    {
        if (!_user.IsAuthenticated)
            throw new ForbiddenException("Onay için giriş yapmanız gerekir.");

        var approval = await _db.AllocationApprovals
            .FirstOrDefaultAsync(a => a.PublicId == r.ApprovalPublicId, ct)
            ?? throw new NotFoundException($"Onay talebi bulunamadı: {r.ApprovalPublicId}");

        if (_tenant.IsBranchLevel && approval.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu talebe erişim yetkiniz yok.");

        // Kaynak ödemenin kalan dağıtılabilir tutarı
        decimal remaining;
        if (approval.Source == AllocationSource.Patient)
        {
            var payment = await _db.Payments.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == approval.PaymentId!.Value, ct)
                ?? throw new NotFoundException("Ödeme bulunamadı.");
            if (payment.IsRefunded)
                throw new InvalidOperationException("İade edilmiş ödeme dağıtılamaz.");

            var already = await _db.PaymentAllocations
                .Where(a => a.PaymentId == payment.Id && !a.IsRefunded)
                .SumAsync(a => (decimal?)a.AllocatedAmount, ct) ?? 0m;
            remaining = payment.Amount - already;
        }
        else
        {
            var ip = await _db.InstitutionPayments.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == approval.InstitutionPaymentId!.Value, ct)
                ?? throw new NotFoundException("Kurum ödemesi bulunamadı.");

            var already = await _db.PaymentAllocations
                .Where(a => a.InstitutionPaymentId == ip.Id && !a.IsRefunded)
                .SumAsync(a => (decimal?)a.AllocatedAmount, ct) ?? 0m;
            remaining = ip.Amount - already;
        }

        if (approval.RequestedAmount > remaining)
            throw new InvalidOperationException(
                $"Talep edilen tutar ({approval.RequestedAmount:N2}) kalan dağıtılabilir tutarı ({remaining:N2}) aşıyor.");

        PaymentAllocation allocation = approval.Source == AllocationSource.Patient
            ? PaymentAllocation.CreateFromPatient(
                paymentId:           approval.PaymentId!.Value,
                treatmentPlanItemId: approval.TreatmentPlanItemId,
                branchId:            approval.BranchId,
                allocatedAmount:     approval.RequestedAmount,
                allocatedByUserId:   _user.UserId,
                method:              AllocationMethod.Manual,
                approvalId:          approval.Id,
                notes:               r.Notes)
            : PaymentAllocation.CreateFromInstitution(
                institutionPaymentId: approval.InstitutionPaymentId!.Value,
                treatmentPlanItemId:  approval.TreatmentPlanItemId,
                branchId:             approval.BranchId,
                allocatedAmount:      approval.RequestedAmount,
                allocatedByUserId:    _user.UserId,
                method:               AllocationMethod.Manual,
                approvalId:           approval.Id,
                notes:                r.Notes);

        _db.PaymentAllocations.Add(allocation);
        await _db.SaveChangesAsync(ct);

        approval.Approve(_user.UserId, r.Notes, allocation.Id);
        approval.SetUpdatedBy(_user.UserId);
        await _db.SaveChangesAsync(ct);

        return AllocationApprovalMappings.ToResponse(approval);
    }
}

public record RejectAllocationCommand(
    Guid ApprovalPublicId,
    string Reason
) : IRequest<AllocationApprovalResponse>;

public class RejectAllocationCommandHandler
    : IRequestHandler<RejectAllocationCommand, AllocationApprovalResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public RejectAllocationCommandHandler(
        AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    public async Task<AllocationApprovalResponse> Handle(
        RejectAllocationCommand r, CancellationToken ct)
    {
        if (!_user.IsAuthenticated)
            throw new ForbiddenException("Red için giriş yapmanız gerekir.");
        if (string.IsNullOrWhiteSpace(r.Reason))
            throw new InvalidOperationException("Red gerekçesi zorunludur.");

        var approval = await _db.AllocationApprovals
            .FirstOrDefaultAsync(a => a.PublicId == r.ApprovalPublicId, ct)
            ?? throw new NotFoundException($"Onay talebi bulunamadı: {r.ApprovalPublicId}");

        if (_tenant.IsBranchLevel && approval.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu talebe erişim yetkiniz yok.");

        approval.Reject(_user.UserId, r.Reason);
        approval.SetUpdatedBy(_user.UserId);
        await _db.SaveChangesAsync(ct);

        return AllocationApprovalMappings.ToResponse(approval);
    }
}
