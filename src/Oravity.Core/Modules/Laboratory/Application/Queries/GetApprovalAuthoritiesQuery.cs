using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Laboratory.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Queries;

public record GetApprovalAuthoritiesQuery
    : IRequest<IReadOnlyList<ApprovalAuthorityResponse>>;

public class GetApprovalAuthoritiesQueryHandler
    : IRequestHandler<GetApprovalAuthoritiesQuery, IReadOnlyList<ApprovalAuthorityResponse>>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public GetApprovalAuthoritiesQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<ApprovalAuthorityResponse>> Handle(
        GetApprovalAuthoritiesQuery request,
        CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct);
        if (companyId == null) return [];

        // Şirket kapsamındaki kullanıcı kimlikleri (UserRoleAssignment üzerinden)
        var companyUserIds = await _db.UserRoleAssignments.AsNoTracking()
            .Where(ura => ura.CompanyId == companyId.Value
                           || (ura.BranchId != null
                               && _db.Branches.Any(b => b.Id == ura.BranchId && b.CompanyId == companyId.Value)))
            .Select(ura => ura.UserId)
            .Distinct()
            .ToListAsync(ct);

        return await _db.LaboratoryApprovalAuthorities.AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Branch)
            .Where(a => companyUserIds.Contains(a.UserId))
            .OrderBy(a => a.User.FullName)
            .Select(a => new ApprovalAuthorityResponse(
                a.PublicId, a.User.PublicId, a.User.FullName,
                a.Branch != null ? a.Branch.PublicId : (Guid?)null,
                a.Branch != null ? a.Branch.Name : null,
                a.CanApprove, a.CanReject, a.NotificationEnabled))
            .ToListAsync(ct);
    }
}
