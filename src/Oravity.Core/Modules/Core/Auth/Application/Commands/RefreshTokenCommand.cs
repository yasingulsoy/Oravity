using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Auth.Application.Commands;

public record RefreshTokenCommand(string RefreshToken, string? IpAddress) : IRequest<LoginResponse>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;

    public RefreshTokenCommandHandler(AppDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtService.HashToken(request.RefreshToken);

        var tokenEntity = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (tokenEntity == null || !tokenEntity.IsValid)
            throw new UnauthorizedException("Geçersiz veya süresi dolmuş refresh token.");

        if (tokenEntity.User is null || !tokenEntity.User.IsActive)
            throw new UnauthorizedException("Hesap aktif değil.");

        // Eski token'ı iptal et
        tokenEntity.Revoke();

        // Yeni token çifti üret
        var newAccessToken = _jwtService.GenerateAccessToken(tokenEntity.User);
        var newRefreshTokenStr = _jwtService.GenerateRefreshToken();
        var newTokenHash = _jwtService.HashToken(newRefreshTokenStr);

        _db.RefreshTokens.Add(
            RefreshToken.Create(tokenEntity.UserId, newTokenHash, DateTime.UtcNow.AddDays(7), request.IpAddress));

        await _db.SaveChangesAsync(cancellationToken);

        return new LoginResponse(newAccessToken, newRefreshTokenStr, 15 * 60);
    }
}
