using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Commission.Application.Queries;

public record GetDoctorTargetsQuery(
    long? DoctorId = null,
    long? BranchId = null,
    int? Year = null,
    int? Month = null
) : IRequest<IReadOnlyList<DoctorTargetResponse>>;

public class GetDoctorTargetsQueryHandler
    : IRequestHandler<GetDoctorTargetsQuery, IReadOnlyList<DoctorTargetResponse>>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetDoctorTargetsQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<DoctorTargetResponse>> Handle(
        GetDoctorTargetsQuery r, CancellationToken ct)
    {
        var q = from t in _db.DoctorTargets.AsNoTracking()
                join u in _db.Users.AsNoTracking() on t.DoctorId equals u.Id
                select new { t, u };

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            q = q.Where(x => x.t.BranchId == _tenant.BranchId.Value);
        else if (r.BranchId.HasValue)
            q = q.Where(x => x.t.BranchId == r.BranchId.Value);

        if (r.DoctorId.HasValue) q = q.Where(x => x.t.DoctorId == r.DoctorId.Value);
        if (r.Year.HasValue)     q = q.Where(x => x.t.Year == r.Year.Value);
        if (r.Month.HasValue)    q = q.Where(x => x.t.Month == r.Month.Value);

        var list = await q
            .OrderByDescending(x => x.t.Year).ThenByDescending(x => x.t.Month)
            .ThenBy(x => x.u.FullName)
            .ToListAsync(ct);

        return list.Select(x => CommissionMappings.ToResponse(x.t, x.u.FullName)).ToList();
    }
}

public record GetBranchTargetsQuery(
    long? BranchId = null,
    int? Year = null,
    int? Month = null
) : IRequest<IReadOnlyList<BranchTargetResponse>>;

public class GetBranchTargetsQueryHandler
    : IRequestHandler<GetBranchTargetsQuery, IReadOnlyList<BranchTargetResponse>>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetBranchTargetsQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<BranchTargetResponse>> Handle(
        GetBranchTargetsQuery r, CancellationToken ct)
    {
        var q = _db.BranchTargets.AsNoTracking().AsQueryable();

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            q = q.Where(t => t.BranchId == _tenant.BranchId.Value);
        else if (r.BranchId.HasValue)
            q = q.Where(t => t.BranchId == r.BranchId.Value);

        if (r.Year.HasValue)  q = q.Where(t => t.Year == r.Year.Value);
        if (r.Month.HasValue) q = q.Where(t => t.Month == r.Month.Value);

        var list = await q
            .OrderByDescending(t => t.Year).ThenByDescending(t => t.Month)
            .ToListAsync(ct);

        return list.Select(CommissionMappings.ToResponse).ToList();
    }
}
