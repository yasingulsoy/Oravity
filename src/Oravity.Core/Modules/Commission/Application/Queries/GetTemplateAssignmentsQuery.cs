using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Commission.Application.Queries;

public record GetTemplateAssignmentsQuery(long? DoctorId = null, bool ActiveOnly = true)
    : IRequest<IReadOnlyList<TemplateAssignmentResponse>>;

public class GetTemplateAssignmentsQueryHandler
    : IRequestHandler<GetTemplateAssignmentsQuery, IReadOnlyList<TemplateAssignmentResponse>>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetTemplateAssignmentsQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<TemplateAssignmentResponse>> Handle(
        GetTemplateAssignmentsQuery r, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct);
        if (companyId == null) return [];

        var q = from a in _db.DoctorTemplateAssignments.AsNoTracking()
                join t in _db.DoctorCommissionTemplates.AsNoTracking() on a.TemplateId equals t.Id
                join u in _db.Users.AsNoTracking() on a.DoctorId equals u.Id
                where t.CompanyId == companyId.Value
                select new { a, t, u };

        if (r.DoctorId.HasValue) q = q.Where(x => x.a.DoctorId == r.DoctorId.Value);
        if (r.ActiveOnly) q = q.Where(x => x.a.IsActive);

        var list = await q.OrderByDescending(x => x.a.CreatedAt).ToListAsync(ct);
        return list.Select(x => CommissionMappings.ToResponse(x.a, x.u.FullName, x.t.Name)).ToList();
    }
}
