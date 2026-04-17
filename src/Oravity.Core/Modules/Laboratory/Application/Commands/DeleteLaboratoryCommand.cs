using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Commands;

public record DeleteLaboratoryCommand(Guid PublicId) : IRequest<Unit>;

public class DeleteLaboratoryCommandHandler
    : IRequestHandler<DeleteLaboratoryCommand, Unit>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public DeleteLaboratoryCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<Unit> Handle(DeleteLaboratoryCommand request, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct)
            ?? throw new ForbiddenException("Şirket bağlamı gerekli.");

        var lab = await _db.Laboratories
            .FirstOrDefaultAsync(l => l.PublicId == request.PublicId
                                       && l.CompanyId == companyId, ct)
            ?? throw new NotFoundException("Laboratuvar bulunamadı.");

        var activeWorkExists = await _db.LaboratoryWorks.AsNoTracking()
            .AnyAsync(w => w.LaboratoryId == lab.Id
                            && w.Status != LaboratoryWorkStatus.Approved
                            && w.Status != LaboratoryWorkStatus.Cancelled
                            && w.Status != LaboratoryWorkStatus.Rejected, ct);
        if (activeWorkExists)
            throw new ConflictException("Aktif iş emri olan laboratuvar silinemez.");

        lab.SoftDelete();
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
