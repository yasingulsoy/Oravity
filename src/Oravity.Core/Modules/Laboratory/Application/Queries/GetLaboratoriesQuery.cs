using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Laboratory.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Queries;

public record GetLaboratoriesQuery(
    bool   ActiveOnly = false,
    Guid?  BranchPublicId = null   // doluysa → sadece bu şubeye atanmışlar
) : IRequest<IReadOnlyList<LaboratoryResponse>>;

public class GetLaboratoriesQueryHandler
    : IRequestHandler<GetLaboratoriesQuery, IReadOnlyList<LaboratoryResponse>>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public GetLaboratoriesQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<LaboratoryResponse>> Handle(
        GetLaboratoriesQuery request,
        CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct);
        if (companyId == null) return [];

        var q = _db.Laboratories.AsNoTracking()
            .Where(l => l.CompanyId == companyId.Value);

        if (request.ActiveOnly) q = q.Where(l => l.IsActive);

        if (request.BranchPublicId is { } bpid)
        {
            var branchId = await _db.Branches.AsNoTracking()
                .Where(b => b.PublicId == bpid && b.CompanyId == companyId.Value)
                .Select(b => (long?)b.Id)
                .FirstOrDefaultAsync(ct);
            if (branchId == null) return [];

            q = q.Where(l => _db.LaboratoryBranchAssignments
                .Any(a => a.LaboratoryId == l.Id
                           && a.BranchId == branchId.Value
                           && a.IsActive));
        }

        var labs = await q
            .OrderByDescending(l => l.IsActive)
            .ThenBy(l => l.Name)
            .ToListAsync(ct);

        var labIds = labs.Select(l => l.Id).ToArray();

        var branchAssignments = await _db.LaboratoryBranchAssignments.AsNoTracking()
            .Where(a => labIds.Contains(a.LaboratoryId) && a.IsActive)
            .Select(a => new { a.LaboratoryId, BranchName = a.Branch.Name })
            .ToListAsync(ct);

        var branchNamesByLab = branchAssignments
            .GroupBy(a => a.LaboratoryId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(a => a.BranchName).OrderBy(n => n).ToList());

        var activeWorkCounts = await _db.LaboratoryWorks.AsNoTracking()
            .Where(w => labIds.Contains(w.LaboratoryId)
                         && w.Status != LaboratoryWorkStatus.Approved
                         && w.Status != LaboratoryWorkStatus.Cancelled
                         && w.Status != LaboratoryWorkStatus.Rejected)
            .GroupBy(w => w.LaboratoryId)
            .Select(g => new { LabId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LabId, x => x.Count, ct);

        return labs.Select(l =>
        {
            var branches = branchNamesByLab.GetValueOrDefault(l.Id, []);
            return new LaboratoryResponse(
                l.PublicId, l.Name, l.Code, l.Phone, l.Email, l.Website,
                l.Country, l.City, l.District, l.Address,
                l.ContactPerson, l.ContactPhone,
                l.WorkingDays, l.WorkingHours, l.PaymentTerms, l.PaymentDays,
                l.Notes, l.IsActive,
                branches.Count,
                activeWorkCounts.GetValueOrDefault(l.Id, 0),
                l.CreatedAt,
                branches);
        }).ToList();
    }
}
