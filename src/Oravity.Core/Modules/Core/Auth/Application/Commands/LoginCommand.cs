using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Auth.Application.Commands;

public record LoginCommand(string Email, string Password, string? IpAddress) : IRequest<LoginResponse>;

public record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn, string TokenType = "Bearer");

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(AppDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.ToLowerInvariant();

        // Son 30 dakikada 5+ başarısız deneme → 429
        var recentFails = await _db.LoginAttempts
            .CountAsync(a => a.Identifier == email
                          && !a.Success
                          && a.CreatedAt > DateTime.UtcNow.AddMinutes(-30),
                       cancellationToken);

        if (recentFails >= 5)
            throw new TooManyRequestsException("Çok fazla hatalı giriş denemesi. 30 dakika bekleyin.");

        // Kullanıcıyı bul (soft-delete filtresi zaten uygulanır)
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _db.LoginAttempts.Add(LoginAttempt.Create(email, request.IpAddress, false));
            await _db.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("E-posta veya şifre hatalı.");
        }

        if (!user.IsActive)
            throw new UnauthorizedException("Hesabınız aktif değil.");

        // Başarılı giriş
        _db.LoginAttempts.Add(LoginAttempt.Create(email, request.IpAddress, true));

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var tokenHash = _jwtService.HashToken(refreshToken);

        _db.RefreshTokens.Add(
            RefreshToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddDays(7), request.IpAddress));
        user.SetLastLoginAt();

        await _db.SaveChangesAsync(cancellationToken);

        return new LoginResponse(accessToken, refreshToken, 15 * 60);
    }
}
