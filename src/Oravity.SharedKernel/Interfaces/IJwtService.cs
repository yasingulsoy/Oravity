using Oravity.SharedKernel.Entities;

namespace Oravity.SharedKernel.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user, long? branchId = null, long? companyId = null, int? roleLevel = null);
    string GenerateRefreshToken();
    string HashToken(string token);
}
