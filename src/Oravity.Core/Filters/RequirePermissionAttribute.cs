using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Filters;

/// <summary>
/// İzin tabanlı yetkilendirme filter'ı.
/// <para>Kullanım: [RequirePermission("patient:view")]</para>
/// <para>İzin kodu formatı: "kaynak:eylem" — DB'de "kaynak.eylem" olarak saklanır.</para>
/// Platform admin her izni bypass eder.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string permission)
        : base(typeof(PermissionAuthorizationFilter))
    {
        Arguments = new object[] { permission };
    }
}

/// <summary>
/// Async izin kontrolü filter'ı — DB üzerinden UserRoleAssignment → Permission zincirini sorgular.
/// </summary>
public class PermissionAuthorizationFilter : IAsyncActionFilter
{
    private readonly ITenantContext _tenantContext;
    private readonly AppDbContext _db;
    private readonly string _permission;

    public PermissionAuthorizationFilter(
        ITenantContext tenantContext,
        AppDbContext db,
        string permission)
    {
        _tenantContext = tenantContext;
        _db = db;
        _permission = permission;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (!_tenantContext.IsAuthenticated)
        {
            context.Result = Problem(401, "Yetkisiz Erişim", "Giriş yapmanız gerekiyor.");
            return;
        }

        // Platform admin her yetkiye sahiptir
        if (_tenantContext.IsPlatformAdmin)
        {
            await next();
            return;
        }

        // "patient:view" → "patient.view"
        var permCode = _permission.Replace(':', '.');

        var hasPermission = await _db.UserRoleAssignments
            .Where(a => a.UserId == _tenantContext.UserId
                        && a.IsActive
                        && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
            .SelectMany(a => a.RoleTemplate.RoleTemplatePermissions)
            .AnyAsync(rtp => rtp.Permission.Code == permCode);

        if (!hasPermission)
        {
            hasPermission = await _db.UserPermissionOverrides
                .AnyAsync(o => o.UserId == _tenantContext.UserId
                               && o.Permission.Code == permCode
                               && o.IsGranted);
        }

        if (!hasPermission)
        {
            context.Result = Problem(
                403,
                "Erişim Engellendi",
                $"Bu işlem için '{_permission}' yetkisi gereklidir.");
            return;
        }

        await next();
    }

    private static ObjectResult Problem(int status, string title, string detail)
        => new(new ProblemDetails { Status = status, Title = title, Detail = detail })
        { StatusCode = status };
}
