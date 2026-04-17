using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Commands;

public record RemoveApprovalAuthorityCommand(Guid PublicId) : IRequest<Unit>;

public class RemoveApprovalAuthorityCommandHandler
    : IRequestHandler<RemoveApprovalAuthorityCommand, Unit>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public RemoveApprovalAuthorityCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<Unit> Handle(RemoveApprovalAuthorityCommand request, CancellationToken ct)
    {
        await ResolveCompanyIdAsync(_tenant, _db, ct);

        var auth = await _db.LaboratoryApprovalAuthorities
            .FirstOrDefaultAsync(a => a.PublicId == request.PublicId, ct)
            ?? throw new NotFoundException("Onay yetkisi bulunamadı.");

        _db.LaboratoryApprovalAuthorities.Remove(auth);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
