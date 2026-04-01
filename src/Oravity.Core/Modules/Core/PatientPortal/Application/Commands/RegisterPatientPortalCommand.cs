using BCrypt.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.PatientPortal.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Core.PatientPortal.Application.Commands;

public record RegisterPatientPortalCommand(
    string Email,
    string Phone,
    string Password,
    long? PatientId = null
) : IRequest<PortalAccountResponse>;

public class RegisterPatientPortalCommandHandler
    : IRequestHandler<RegisterPatientPortalCommand, PortalAccountResponse>
{
    private readonly AppDbContext _db;

    public RegisterPatientPortalCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PortalAccountResponse> Handle(
        RegisterPatientPortalCommand request,
        CancellationToken cancellationToken)
    {
        var emailNorm = request.Email.ToLowerInvariant().Trim();

        // Email benzersizlik kontrolü
        if (await _db.PatientPortalAccounts
            .AnyAsync(a => a.Email == emailNorm, cancellationToken))
            throw new ConflictException($"Bu e-posta adresiyle zaten bir hesap mevcut: {emailNorm}");

        // Hasta ID verilmişse zaten portal hesabı var mı?
        if (request.PatientId.HasValue &&
            await _db.PatientPortalAccounts
                .AnyAsync(a => a.PatientId == request.PatientId, cancellationToken))
            throw new ConflictException("Bu hastaya ait bir portal hesabı zaten mevcut.");

        var hash    = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var account = PatientPortalAccount.Create(emailNorm, request.Phone, hash, request.PatientId);

        _db.PatientPortalAccounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);

        // Gerçek uygulamada email verification link'i gönderilir.
        // Burada token response'a eklenmez — güvenlik gereği sadece e-posta ile iletilir.

        return PatientPortalMappings.ToResponse(account);
    }
}
