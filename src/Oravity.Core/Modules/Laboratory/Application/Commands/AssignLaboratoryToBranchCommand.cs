using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Laboratory.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Commands;

public record AssignLaboratoryToBranchCommand(
    Guid LaboratoryPublicId,
    Guid BranchPublicId,
    int  Priority,
    bool IsActive
) : IRequest<BranchAssignmentResponse>;

public class AssignLaboratoryToBranchCommandHandler
    : IRequestHandler<AssignLaboratoryToBranchCommand, BranchAssignmentResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public AssignLaboratoryToBranchCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<BranchAssignmentResponse> Handle(
        AssignLaboratoryToBranchCommand request,
        CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct)
            ?? throw new ForbiddenException("Şirket bağlamı gerekli.");

        var lab = await _db.Laboratories
            .FirstOrDefaultAsync(l => l.PublicId == request.LaboratoryPublicId
                                       && l.CompanyId == companyId, ct)
            ?? throw new NotFoundException("Laboratuvar bulunamadı.");

        var branch = await _db.Branches
            .FirstOrDefaultAsync(b => b.PublicId == request.BranchPublicId
                                       && b.CompanyId == companyId, ct)
            ?? throw new NotFoundException("Şube bulunamadı.");

        var existing = await _db.LaboratoryBranchAssignments
            .FirstOrDefaultAsync(a => a.LaboratoryId == lab.Id
                                       && a.BranchId == branch.Id, ct);

        if (existing != null)
        {
            existing.Update(request.Priority, request.IsActive);
        }
        else
        {
            existing = LaboratoryBranchAssignment.Create(lab.Id, branch.Id, request.Priority);
            if (!request.IsActive) existing.Update(request.Priority, false);
            _db.LaboratoryBranchAssignments.Add(existing);
        }

        await _db.SaveChangesAsync(ct);

        return new BranchAssignmentResponse(
            existing.PublicId, branch.PublicId, branch.Name,
            existing.Priority, existing.IsActive);
    }
}
