using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientPortal.Infrastructure.Services;

/// <summary>
/// "PatientPortal" authentication scheme JWT claim'lerinden portal kimliğini çözümler.
/// </summary>
public class CurrentPortalUserService : ICurrentPortalUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentPortalUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true &&
        long.TryParse(User.FindFirst("portal_account_id")?.Value, out _);

    public long AccountId
    {
        get
        {
            var val = User?.FindFirst("portal_account_id")?.Value;
            return long.TryParse(val, out var id) ? id
                : throw new UnauthorizedAccessException("Portal hesabı bulunamadı.");
        }
    }

    public long PatientId
    {
        get
        {
            var val = User?.FindFirst("patient_id")?.Value;
            return long.TryParse(val, out var id) && id > 0 ? id
                : throw new UnauthorizedAccessException("Hasta kaydı eşleştirilmemiş.");
        }
    }

    public long BranchId => 0;   // Portal JWT'de şube scope'u yoktur; context'ten alınır.
    public long CompanyId => 0;  // Portal JWT'de şirket scope'u yoktur; context'ten alınır.
}
