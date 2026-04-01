using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.PatientPortal.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Core.PatientPortal.Application.Commands;

public record VerifyPatientPortalEmailCommand(
    string Token
) : IRequest<PortalAccountResponse>;

public class VerifyPatientPortalEmailCommandHandler
    : IRequestHandler<VerifyPatientPortalEmailCommand, PortalAccountResponse>
{
    private readonly AppDbContext _db;

    public VerifyPatientPortalEmailCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PortalAccountResponse> Handle(
        VerifyPatientPortalEmailCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _db.PatientPortalAccounts
            .FirstOrDefaultAsync(
                a => a.EmailVerificationToken == request.Token,
                cancellationToken)
            ?? throw new NotFoundException("Geçersiz veya süresi dolmuş doğrulama tokeni.");

        if (!account.VerifyEmail(request.Token))
            throw new InvalidOperationException("Doğrulama tokeni süresi dolmuş. Yeni token talep edin.");

        await _db.SaveChangesAsync(cancellationToken);

        return PatientPortalMappings.ToResponse(account);
    }
}
