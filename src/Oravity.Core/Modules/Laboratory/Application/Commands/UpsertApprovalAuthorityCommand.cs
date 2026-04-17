using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Laboratory.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Commands;

public record UpsertApprovalAuthorityCommand(
    Guid  UserPublicId,
    Guid? BranchPublicId,      // null → tüm şubeler
    bool  CanApprove,
    bool  CanReject,
    bool  NotificationEnabled
) : IRequest<ApprovalAuthorityResponse>;

public class UpsertApprovalAuthorityCommandHandler
    : IRequestHandler<UpsertApprovalAuthorityCommand, ApprovalAuthorityResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public UpsertApprovalAuthorityCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<ApprovalAuthorityResponse> Handle(
        UpsertApprovalAuthorityCommand request,
        CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct)
            ?? throw new ForbiddenException("Şirket bağlamı gerekli.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.PublicId == request.UserPublicId, ct)
            ?? throw new NotFoundException("Kullanıcı bulunamadı.");

        long? branchId = null;
        string? branchName = null;
        Guid? branchPid = null;
        if (request.BranchPublicId is { } bpid)
        {
            var branch = await _db.Branches
                .FirstOrDefaultAsync(b => b.PublicId == bpid && b.CompanyId == companyId, ct)
                ?? throw new NotFoundException("Şube bulunamadı.");
            branchId   = branch.Id;
            branchName = branch.Name;
            branchPid  = branch.PublicId;
        }

        var existing = await _db.LaboratoryApprovalAuthorities
            .FirstOrDefaultAsync(a => a.UserId == user.Id
                                       && a.BranchId == branchId, ct);

        if (existing != null)
        {
            existing.Update(request.CanApprove, request.CanReject, request.NotificationEnabled);
        }
        else
        {
            existing = LaboratoryApprovalAuthority.Create(
                user.Id, branchId, request.CanApprove, request.CanReject, request.NotificationEnabled);
            _db.LaboratoryApprovalAuthorities.Add(existing);
        }

        await _db.SaveChangesAsync(ct);

        return new ApprovalAuthorityResponse(
            existing.PublicId, user.PublicId, user.FullName,
            branchPid, branchName,
            existing.CanApprove, existing.CanReject, existing.NotificationEnabled);
    }
}
