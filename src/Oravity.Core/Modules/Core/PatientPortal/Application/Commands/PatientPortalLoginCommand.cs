using BCrypt.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.PatientPortal.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientPortal.Application.Commands;

public record PatientPortalLoginCommand(
    string Email,
    string Password,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<PortalLoginResponse>;

public class PatientPortalLoginCommandHandler
    : IRequestHandler<PatientPortalLoginCommand, PortalLoginResponse>
{
    private readonly AppDbContext _db;
    private readonly IPatientPortalJwtService _jwt;

    public PatientPortalLoginCommandHandler(AppDbContext db, IPatientPortalJwtService jwt)
    {
        _db  = db;
        _jwt = jwt;
    }

    public async Task<PortalLoginResponse> Handle(
        PatientPortalLoginCommand request,
        CancellationToken cancellationToken)
    {
        var emailNorm = request.Email.ToLowerInvariant().Trim();

        var account = await _db.PatientPortalAccounts
            .FirstOrDefaultAsync(a => a.Email == emailNorm, cancellationToken);

        if (account is null || !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
            throw new UnauthorizedAccessException("E-posta veya şifre hatalı.");

        if (!account.IsActive)
            throw new UnauthorizedAccessException("Hesap devre dışı. Lütfen kliniği arayın.");

        if (!account.IsEmailVerified)
            throw new UnauthorizedAccessException("E-posta adresinizi doğrulamadan giriş yapamazsınız.");

        // Token üret
        var tokenPair    = _jwt.GenerateTokenPair(account);
        var refreshToken = _jwt.GenerateRefreshToken();
        var tokenHash    = _jwt.HashToken(refreshToken);

        var session = PatientPortalSession.Create(
            account.Id, tokenHash,
            request.IpAddress, request.UserAgent);

        _db.PatientPortalSessions.Add(session);

        account.RecordLogin();
        await _db.SaveChangesAsync(cancellationToken);

        return new PortalLoginResponse(
            tokenPair.AccessToken,
            refreshToken,
            tokenPair.ExpiresIn,
            PatientPortalMappings.ToResponse(account));
    }
}
