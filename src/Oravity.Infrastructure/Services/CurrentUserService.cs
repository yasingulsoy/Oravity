using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Infrastructure.Services;

/// <summary>
/// JWT claim'lerinden ve TenantContext'ten kullanıcı bilgilerini okur.
/// Permission kontrolü için RequirePermissionFilter kullanın (async DB sorgusu içerir).
/// </summary>
public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantContext _tenantContext;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ITenantContext tenantContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        _tenantContext.IsAuthenticated && Principal?.Identity?.IsAuthenticated == true;

    public long UserId => _tenantContext.UserId;

    /// <summary>BranchId varsa branch, yoksa CompanyId döner.</summary>
    public long TenantId => _tenantContext.BranchId ?? _tenantContext.CompanyId ?? _tenantContext.UserId;

    public long? BranchId => _tenantContext.BranchId;

    public string Email =>
        Principal?.FindFirstValue(ClaimTypes.Email)
        ?? Principal?.FindFirstValue("email")
        ?? string.Empty;

    public string FullName => Principal?.FindFirstValue("full_name") ?? string.Empty;

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        ?? (IReadOnlyList<string>)Array.Empty<string>();

    public bool HasRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Senkron claim kontrolü — platform admin her şeye erişebilir.
    /// Detaylı DB bazlı yetki için RequirePermissionFilter kullanın.
    /// </summary>
    public bool HasPermission(string permission) =>
        _tenantContext.IsPlatformAdmin;
}
