using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientPortal.Application.Commands;

public record PatientPortalLogoutCommand(string RefreshToken) : IRequest;

public class PatientPortalLogoutCommandHandler
    : IRequestHandler<PatientPortalLogoutCommand>
{
    private readonly AppDbContext _db;
    private readonly IPatientPortalJwtService _jwt;

    public PatientPortalLogoutCommandHandler(AppDbContext db, IPatientPortalJwtService jwt)
    {
        _db  = db;
        _jwt = jwt;
    }

    public async Task Handle(
        PatientPortalLogoutCommand request,
        CancellationToken cancellationToken)
    {
        var tokenHash = _jwt.HashToken(request.RefreshToken);

        var session = await _db.PatientPortalSessions
            .FirstOrDefaultAsync(s => s.TokenHash == tokenHash, cancellationToken);

        if (session is not null && !session.IsRevoked)
        {
            session.Revoke();
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
