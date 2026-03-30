namespace Oravity.SharedKernel.Interfaces;

public interface ITenantContext
{
    long UserId { get; set; }

    /// <summary>
    /// 1 = Platform Admin, 2 = Company Admin, 3 = Branch Manager, 4+ = Branch staff
    /// </summary>
    int Role { get; set; }

    long? CompanyId { get; set; }
    long? BranchId { get; set; }

    bool IsPlatformAdmin { get; }
    bool IsCompanyAdmin { get; }
    bool IsBranchLevel { get; }
    bool IsAuthenticated { get; }
}
