using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Auth.Application.Commands;

public record LogoutCommand(string RefreshToken) : IRequest;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;

    public LogoutCommandHandler(AppDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return;

        var tokenHash = _jwtService.HashToken(request.RefreshToken);

        var tokenEntity = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (tokenEntity is { IsRevoked: false })
        {
            tokenEntity.Revoke();
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
