using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Pricing.Application;

/// <summary>
/// JWT'de company_id eksik olduğunda (branch-level kullanıcı) DB'den çözer.
/// </summary>
internal static class TenantCompanyResolver
{
    public static async Task<long?> ResolveCompanyIdAsync(
        ITenantContext tenant,
        AppDbContext db,
        CancellationToken ct = default)
    {
        var companyId = tenant.CompanyId;

        if (companyId == null && tenant.BranchId.HasValue)
            companyId = await db.Branches.AsNoTracking()
                .Where(b => b.Id == tenant.BranchId.Value)
                .Select(b => (long?)b.CompanyId)
                .FirstOrDefaultAsync(ct);

        if (companyId == null && tenant.UserId > 0)
        {
            var assignment = await db.UserRoleAssignments.AsNoTracking()
                .Where(a => a.UserId == tenant.UserId && a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (assignment != null)
            {
                companyId = assignment.CompanyId;

                if (companyId == null && assignment.BranchId.HasValue)
                    companyId = await db.Branches.AsNoTracking()
                        .Where(b => b.Id == assignment.BranchId.Value)
                        .Select(b => (long?)b.CompanyId)
                        .FirstOrDefaultAsync(ct);
            }
        }

        // Platform Admin fallback: şirket sayısından bağımsız, ilk şirketi kullan
        if (companyId == null && tenant.IsPlatformAdmin)
        {
            companyId = await db.Companies.AsNoTracking()
                .OrderBy(c => c.Id)
                .Select(c => (long?)c.Id)
                .FirstOrDefaultAsync(ct);
        }

        // Normal kullanıcı fallback: sadece tek şirket varsa kullan
        if (companyId == null && !tenant.IsPlatformAdmin)
        {
            var companies = await db.Companies.AsNoTracking()
                .Select(c => c.Id)
                .Take(2)
                .ToListAsync(ct);

            if (companies.Count == 1)
                companyId = companies[0];
        }

        return companyId;
    }
}
