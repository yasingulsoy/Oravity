using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Laboratory.Application;
using Oravity.Core.Modules.Laboratory.Application.Queries;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Commands;

/// <summary>
/// Tek komut ile lab iş emri durum geçişi yapar.
/// Action: send | in_progress | ready | receive | fit | complete | approve | reject | cancel
/// </summary>
public record TransitionLaboratoryWorkCommand(
    Guid    WorkPublicId,
    string  Action,
    string? Notes
) : IRequest<LaboratoryWorkDetailResponse>;

public class TransitionLaboratoryWorkCommandHandler
    : IRequestHandler<TransitionLaboratoryWorkCommand, LaboratoryWorkDetailResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public TransitionLaboratoryWorkCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<LaboratoryWorkDetailResponse> Handle(
        TransitionLaboratoryWorkCommand request,
        CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct)
            ?? throw new ForbiddenException("Şirket bağlamı gerekli.");

        var work = await _db.LaboratoryWorks
            .Include(w => w.Items).ThenInclude(i => i.LabPriceItem)
            .FirstOrDefaultAsync(w => w.PublicId == request.WorkPublicId
                                       && w.CompanyId == companyId, ct)
            ?? throw new NotFoundException("Laboratuvar iş emri bulunamadı.");

        var userId = _tenant.UserId > 0 ? _tenant.UserId : 0;
        var action = (request.Action ?? "").Trim().ToLowerInvariant();

        switch (action)
        {
            case "send":
                var estDays = work.Items
                    .Where(i => i.LabPriceItem != null)
                    .Select(i => i.LabPriceItem!.EstimatedDeliveryDays ?? 0)
                    .DefaultIfEmpty(0)
                    .Max();
                if (estDays <= 0) estDays = 7;
                work.SendToLab(estDays, userId, request.Notes);
                break;

            case "in_progress":
                work.MarkInProgress(userId, request.Notes);
                break;

            case "ready":
                work.MarkReady(userId, request.Notes);
                break;

            case "receive":
                work.Receive(userId, request.Notes);
                break;

            case "fit":
                work.Fit(userId, request.Notes);
                break;

            case "complete":
                work.Complete(userId, request.Notes);
                break;

            case "approve":
                await EnsureApprovalAuthorityAsync(userId, work.BranchId, canApprove: true, ct);
                work.Approve(userId, request.Notes);
                break;

            case "reject":
                await EnsureApprovalAuthorityAsync(userId, work.BranchId, canApprove: false, ct);
                work.Reject(userId, request.Notes ?? throw new InvalidOperationException("Red nedeni zorunludur."));
                break;

            case "cancel":
                work.Cancel(userId, request.Notes);
                break;

            default:
                throw new InvalidOperationException($"Geçersiz durum geçişi: '{request.Action}'.");
        }

        await _db.SaveChangesAsync(ct);

        return await GetLaboratoryWorkDetailQueryHandler.BuildDetailAsync(_db, work.Id, ct);
    }

    private async Task EnsureApprovalAuthorityAsync(long userId, long branchId, bool canApprove, CancellationToken ct)
    {
        if (_tenant.IsPlatformAdmin) return;

        var auth = await _db.LaboratoryApprovalAuthorities.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId
                                       && (a.BranchId == null || a.BranchId == branchId), ct);

        if (auth == null) throw new ForbiddenException("Lab işi onay/red yetkiniz yok.");
        if (canApprove && !auth.CanApprove) throw new ForbiddenException("Lab işi onay yetkiniz yok.");
        if (!canApprove && !auth.CanReject) throw new ForbiddenException("Lab işi red yetkiniz yok.");
    }
}
