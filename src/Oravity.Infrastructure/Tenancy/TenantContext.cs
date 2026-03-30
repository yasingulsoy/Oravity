using Oravity.SharedKernel.Interfaces;

namespace Oravity.Infrastructure.Tenancy;

public class TenantContext : ITenantContext
{
    public long UserId { get; set; }
    public int Role { get; set; }
    public long? CompanyId { get; set; }
    public long? BranchId { get; set; }

    /// <summary>Role == 1: tüm şirket ve şubeler</summary>
    public bool IsPlatformAdmin => Role == 1;

    /// <summary>Role == 2: company_id altındaki tüm şubeler</summary>
    public bool IsCompanyAdmin => Role == 2;

    /// <summary>Role >= 3: sadece belirli şube</summary>
    public bool IsBranchLevel => Role >= 3;

    public bool IsAuthenticated => UserId > 0;
}
