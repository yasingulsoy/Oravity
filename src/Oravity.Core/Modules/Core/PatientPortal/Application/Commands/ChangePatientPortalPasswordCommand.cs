using BCrypt.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientPortal.Application.Commands;

public record ChangePatientPortalPasswordCommand(
    string CurrentPassword,
    string NewPassword
) : IRequest;

public class ChangePatientPortalPasswordCommandHandler
    : IRequestHandler<ChangePatientPortalPasswordCommand>
{
    private readonly AppDbContext _db;
    private readonly ICurrentPortalUser _portalUser;

    public ChangePatientPortalPasswordCommandHandler(
        AppDbContext db, ICurrentPortalUser portalUser)
    {
        _db          = db;
        _portalUser  = portalUser;
    }

    public async Task Handle(
        ChangePatientPortalPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _db.PatientPortalAccounts
            .FirstOrDefaultAsync(a => a.Id == _portalUser.AccountId, cancellationToken)
            ?? throw new NotFoundException("Portal hesabı bulunamadı.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, account.PasswordHash))
            throw new UnauthorizedAccessException("Mevcut şifre hatalı.");

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        account.ChangePassword(newHash);

        // Tüm aktif oturumları sonlandır
        var sessions = await _db.PatientPortalSessions
            .Where(s => s.AccountId == account.Id && !s.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
            session.Revoke();

        await _db.SaveChangesAsync(cancellationToken);
    }
}
