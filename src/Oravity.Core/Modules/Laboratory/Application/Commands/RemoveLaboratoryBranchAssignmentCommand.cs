using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Commands;

public record RemoveLaboratoryBranchAssignmentCommand(Guid AssignmentPublicId) : IRequest<Unit>;

public class RemoveLaboratoryBranchAssignmentCommandHandler
    : IRequestHandler<RemoveLaboratoryBranchAssignmentCommand, Unit>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public RemoveLaboratoryBranchAssignmentCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<Unit> Handle(RemoveLaboratoryBranchAssignmentCommand request, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct)
            ?? throw new ForbiddenException("Şirket bağlamı gerekli.");

        var assignment = await _db.LaboratoryBranchAssignments
            .Include(a => a.Laboratory)
            .FirstOrDefaultAsync(a => a.PublicId == request.AssignmentPublicId
                                       && a.Laboratory.CompanyId == companyId, ct)
            ?? throw new NotFoundException("Şube ataması bulunamadı.");

        _db.LaboratoryBranchAssignments.Remove(assignment);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
