namespace Oravity.SharedKernel.Interfaces;

public interface ICurrentUser
{
    long UserId { get; }
    long TenantId { get; }
    long? BranchId { get; }
    string Email { get; }
    string FullName { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool HasRole(string role);
    bool HasPermission(string permission);
}
