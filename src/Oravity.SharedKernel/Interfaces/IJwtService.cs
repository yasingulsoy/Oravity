using Oravity.SharedKernel.Entities;

namespace Oravity.SharedKernel.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashToken(string token);
}
