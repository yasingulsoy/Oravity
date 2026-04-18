using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Finance.Application.Commands;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Queries;

public record GetAllocationApprovalsQuery(
    AllocationApprovalStatus? Status = null,
    long? PatientId = null
) : IRequest<IReadOnlyList<AllocationApprovalResponse>>;

public class GetAllocationApprovalsQueryHandler
    : IRequestHandler<GetAllocationApprovalsQuery, IReadOnlyList<AllocationApprovalResponse>>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetAllocationApprovalsQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<AllocationApprovalResponse>> Handle(
        GetAllocationApprovalsQuery r, CancellationToken ct)
    {
        var q = _db.AllocationApprovals.AsNoTracking().AsQueryable();

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            q = q.Where(a => a.BranchId == _tenant.BranchId.Value);

        if (r.Status.HasValue)     q = q.Where(a => a.Status == r.Status.Value);
        if (r.PatientId.HasValue)  q = q.Where(a => a.PatientId == r.PatientId.Value);

        var list = await q
            .OrderByDescending(a => a.CreatedAt)
            .Take(500)
            .ToListAsync(ct);

        return list.Select(AllocationApprovalMappings.ToResponse).ToList();
    }
}
