using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Patient.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Patient.Application.Queries;

public record SearchPatientsQuery(
    string? FirstName,
    string? LastName,
    string? Phone,
    string? TcHash,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<PatientResponse>>;

public class SearchPatientsQueryHandler
    : IRequestHandler<SearchPatientsQuery, PagedResult<PatientResponse>>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public SearchPatientsQueryHandler(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<PagedResult<PatientResponse>> Handle(
        SearchPatientsQuery request,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var page     = Math.Max(request.Page, 1);

        IQueryable<SharedKernel.Entities.Patient> q = _db.Patients
            .AsNoTracking()
            .Include(p => p.CitizenshipType)
            .Include(p => p.ReferralSource);

        q = ApplyTenantFilter(q);

        if (!string.IsNullOrWhiteSpace(request.FirstName))
            q = q.Where(p => EF.Functions.ILike(p.FirstName, $"%{request.FirstName.Trim()}%"));

        if (!string.IsNullOrWhiteSpace(request.LastName))
            q = q.Where(p => EF.Functions.ILike(p.LastName, $"%{request.LastName.Trim()}%"));

        if (!string.IsNullOrWhiteSpace(request.Phone))
            q = q.Where(p => p.Phone != null &&
                EF.Functions.ILike(p.Phone, $"%{request.Phone.Trim()}%"));

        if (!string.IsNullOrWhiteSpace(request.TcHash))
            q = q.Where(p => p.TcNumberHash == request.TcHash.ToLowerInvariant());

        var totalCount = await q.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await q
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => PatientMappings.ToResponse(p))
            .ToListAsync(cancellationToken);

        return new PagedResult<PatientResponse>(items, page, pageSize, totalCount, totalPages);
    }

    private IQueryable<SharedKernel.Entities.Patient> ApplyTenantFilter(
        IQueryable<SharedKernel.Entities.Patient> query)
    {
        if (_tenantContext.IsPlatformAdmin) return query;

        if (_tenantContext.IsBranchLevel && _tenantContext.BranchId.HasValue)
            return query.Where(p => p.BranchId == _tenantContext.BranchId.Value);

        if (_tenantContext.IsCompanyAdmin && _tenantContext.CompanyId.HasValue)
            return query.Where(p => p.Branch.CompanyId == _tenantContext.CompanyId.Value);

        return query.Where(_ => false);
    }
}
