using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Infrastructure.Tenancy;

/// <summary>
/// Her request'te tenant bağlamını çözer:
///   1. JWT claim'lerinden okur (birincil kaynak)
///   2. X-Company-Id / X-Branch-Id header'larından okur (fallback / override)
///   3. Hâlâ null ise DB'den otomatik çözer (tek şirket/şube senaryosu)
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantCtx = context.RequestServices.GetService(typeof(ITenantContext)) as ITenantContext;

        if (tenantCtx is not null)
        {
            var user = context.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                // JWT sub claim → UserId
                if (long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub"), out var userId))
                {
                    tenantCtx.UserId = userId;
                }

                // Platform admin claim → role level 1
                if (user.FindFirstValue("is_platform_admin") == "true")
                    tenantCtx.Role = 1;
                // JWT role claim → numeric role level (regular users)
                else if (int.TryParse(user.FindFirstValue("role_level"), out var roleLevel))
                    tenantCtx.Role = roleLevel;

                // JWT company_id claim
                if (long.TryParse(user.FindFirstValue("company_id"), out var jwtCompanyId))
                    tenantCtx.CompanyId = jwtCompanyId;

                // JWT branch_id claim
                if (long.TryParse(user.FindFirstValue("branch_id"), out var jwtBranchId))
                    tenantCtx.BranchId = jwtBranchId;
            }

            // Header override — developer tools veya platform admin için
            if (context.Request.Headers.TryGetValue("X-Company-Id", out var companyHeader)
                && long.TryParse(companyHeader, out var headerCompanyId))
            {
                tenantCtx.CompanyId = headerCompanyId;
            }

            if (context.Request.Headers.TryGetValue("X-Branch-Id", out var branchHeader)
                && long.TryParse(branchHeader, out var headerBranchId))
            {
                tenantCtx.BranchId = headerBranchId;
            }

            // JWT/header'dan çözülemediyse DB'den otomatik çöz
            if (tenantCtx.IsAuthenticated && (tenantCtx.CompanyId == null || tenantCtx.Role == 0))
            {
                var db = context.RequestServices.GetRequiredService<AppDbContext>();
                await ResolveFromDatabase(tenantCtx, db);
            }
        }

        await _next(context);
    }

    private static async Task ResolveFromDatabase(ITenantContext tenant, AppDbContext db)
    {
        // 0. Role henüz set edilmemişse DB'den IsPlatformAdmin kontrol et
        if (tenant.Role == 0 && tenant.UserId > 0)
        {
            var isPlatformAdmin = await db.Users.AsNoTracking()
                .Where(u => u.Id == tenant.UserId)
                .Select(u => u.IsPlatformAdmin)
                .FirstOrDefaultAsync();

            if (isPlatformAdmin)
                tenant.Role = 1;
        }

        // 1. UserRoleAssignment üzerinden çöz
        if (tenant.UserId > 0)
        {
            var assignment = await db.UserRoleAssignments.AsNoTracking()
                .Where(a => a.UserId == tenant.UserId && a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new { a.CompanyId, a.BranchId })
                .FirstOrDefaultAsync();

            if (assignment is not null)
            {
                tenant.CompanyId = assignment.CompanyId;
                tenant.BranchId ??= assignment.BranchId;

                if (tenant.CompanyId == null && assignment.BranchId.HasValue)
                {
                    tenant.CompanyId = await db.Branches.AsNoTracking()
                        .Where(b => b.Id == assignment.BranchId.Value)
                        .Select(b => (long?)b.CompanyId)
                        .FirstOrDefaultAsync();
                }
            }
        }

        // 2. Hâlâ null ise şirket çöz
        if (tenant.CompanyId == null)
        {
            // Platform admin → şirket sayısından bağımsız, ilk şirketi al
            // Normal kullanıcı → sadece tek şirket varsa kullan
            long? resolvedCompanyId = null;

            if (tenant.IsPlatformAdmin)
            {
                resolvedCompanyId = await db.Companies.AsNoTracking()
                    .OrderBy(c => c.Id)
                    .Select(c => (long?)c.Id)
                    .FirstOrDefaultAsync();
            }
            else
            {
                var companyIds = await db.Companies.AsNoTracking()
                    .Select(c => c.Id).Take(2).ToListAsync();
                if (companyIds.Count == 1)
                    resolvedCompanyId = companyIds[0];
            }

            if (resolvedCompanyId.HasValue)
            {
                tenant.CompanyId = resolvedCompanyId.Value;

                if (tenant.BranchId == null)
                {
                    var branchIds = await db.Branches.AsNoTracking()
                        .Where(b => b.CompanyId == resolvedCompanyId.Value)
                        .Select(b => b.Id).Take(2).ToListAsync();

                    if (branchIds.Count == 1)
                        tenant.BranchId = branchIds[0];
                }
            }
        }
    }
}
