using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Infrastructure.Tenancy;

/// <summary>
/// Her request'te tenant bağlamını çözer:
///   1. JWT claim'lerinden okur (birincil kaynak)
///   2. X-Company-Id / X-Branch-Id header'larından okur (fallback / override)
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

                // JWT role claim → numeric role level
                if (int.TryParse(user.FindFirstValue("role_level"), out var roleLevel))
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
        }

        await _next(context);
    }
}
