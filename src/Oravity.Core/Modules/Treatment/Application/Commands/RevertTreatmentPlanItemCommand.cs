using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Treatment.Application.Commands;

/// <summary>
/// Tamamlanmış tedavi kalemini 'Onaylandı' durumuna geri alır.
///
/// Kurallar:
///   1. Kalem 'Tamamlandı' durumunda olmalı.
///   2. Geri alma nedeni (reason) zorunludur.
///   3. Ödeme tahsisi yapılmışsa işlem reddedilir
///      — tahsis önce iptal/kaldırılmalıdır.
///   4. İzin: treatment_plan.revert_completed
///      (platform admin her zaman geçer).
///
/// Onam formu hakkında:
///   - İmzalı onam formu varsa geri alma bloklanmaz;
///     ancak onam kaydı geçerliliğini korur.
///     Kliniğin politikasına göre onam ayrıca iptal edilebilir.
/// </summary>
public record RevertTreatmentPlanItemCommand(
    Guid   ItemPublicId,
    string Reason
) : IRequest<TreatmentPlanItemResponse>;

public class RevertTreatmentPlanItemCommandHandler
    : IRequestHandler<RevertTreatmentPlanItemCommand, TreatmentPlanItemResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public RevertTreatmentPlanItemCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<TreatmentPlanItemResponse> Handle(
        RevertTreatmentPlanItemCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Geri alma nedeni boş bırakılamaz.");

        var item = await _db.TreatmentPlanItems
            .Include(i => i.Plan)
            .Include(i => i.Treatment)
            .FirstOrDefaultAsync(i => i.PublicId == request.ItemPublicId, cancellationToken)
            ?? throw new NotFoundException($"Tedavi kalemi bulunamadı: {request.ItemPublicId}");

        EnsureTenantAccess(item.Plan);
        await EnsurePermissionAsync(cancellationToken);
        await EnsureNoPaymentAllocationAsync(item.Id, cancellationToken);

        item.RevertToApproved();

        // Audit kaydı — notes alanına ekliyoruz (OutboxMessage olarak da gönderilecek)
        var auditNote = $"[Geri Alındı {DateTime.UtcNow:dd.MM.yyyy HH:mm}] {request.Reason.Trim()}";
        item.AddNote(string.IsNullOrWhiteSpace(item.Notes)
            ? auditNote
            : $"{item.Notes}\n{auditNote}");

        await _db.SaveChangesAsync(cancellationToken);

        return TreatmentPlanMappings.ToResponse(item);
    }

    private void EnsureTenantAccess(TreatmentPlan plan)
    {
        if (_tenant.IsPlatformAdmin) return;
        if (_tenant.IsBranchLevel && plan.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu tedavi kalemine erişim yetkiniz bulunmuyor.");
    }

    private async Task EnsurePermissionAsync(CancellationToken ct)
    {
        if (_tenant.IsPlatformAdmin) return;

        const string permCode = "treatment_plan.revert_completed";

        var hasOverride = await _db.UserPermissionOverrides
            .AnyAsync(o => o.UserId == _tenant.UserId
                           && o.Permission.Code == permCode
                           && o.IsGranted, ct);
        if (hasOverride) return;

        var hasRole = await _db.UserRoleAssignments
            .Where(a => a.UserId == _tenant.UserId
                        && a.IsActive
                        && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
            .SelectMany(a => a.RoleTemplate.RoleTemplatePermissions)
            .AnyAsync(rtp => rtp.Permission.Code == permCode, ct);

        if (!hasRole)
            throw new ForbiddenException("Tamamlanmış tedaviyi geri alma yetkiniz bulunmuyor.");
    }

    private async Task EnsureNoPaymentAllocationAsync(long itemId, CancellationToken ct)
    {
        var hasAllocation = await _db.PaymentAllocations
            .AnyAsync(a => a.TreatmentPlanItemId == itemId, ct);

        if (hasAllocation)
            throw new ConflictException(
                "Bu tedaviye ödeme tahsisi yapılmış. " +
                "Geri almadan önce ödeme tahsisini kaldırın.");
    }
}
