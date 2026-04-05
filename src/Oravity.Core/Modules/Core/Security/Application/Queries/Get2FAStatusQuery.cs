using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Security.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Security.Application.Queries;

public record Get2FAStatusQuery : IRequest<TwoFAStatusResponse>;

public class Get2FAStatusQueryHandler : IRequestHandler<Get2FAStatusQuery, TwoFAStatusResponse>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public Get2FAStatusQueryHandler(AppDbContext db, ICurrentUser user)
    {
        _db   = db;
        _user = user;
    }

    public async Task<TwoFAStatusResponse> Handle(Get2FAStatusQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsAuthenticated)
            throw new UnauthorizedException("Giriş yapmanız gerekiyor.");

        var settings = await _db.User2FASettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == _user.UserId, cancellationToken);

        if (settings is null)
            return new TwoFAStatusResponse(false, false, false, null, null);

        return new TwoFAStatusResponse(
            settings.TotpEnabled,
            settings.SmsEnabled,
            settings.EmailEnabled,
            settings.PreferredMethod,
            settings.Last2faAt);
    }
}
