using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Security.Application.Commands;

public record Disable2FACommand : IRequest<bool>;

public class Disable2FACommandHandler : IRequestHandler<Disable2FACommand, bool>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public Disable2FACommandHandler(AppDbContext db, ICurrentUser user)
    {
        _db   = db;
        _user = user;
    }

    public async Task<bool> Handle(Disable2FACommand request, CancellationToken cancellationToken)
    {
        if (!_user.IsAuthenticated)
            throw new UnauthorizedException("Giriş yapmanız gerekiyor.");

        var settings = await _db.User2FASettings
            .FirstOrDefaultAsync(s => s.UserId == _user.UserId, cancellationToken);

        if (settings is null || !settings.TotpEnabled)
            return true; // Zaten devre dışı

        settings.DisableTotp();
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
