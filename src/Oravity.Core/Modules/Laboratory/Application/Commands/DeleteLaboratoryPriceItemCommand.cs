using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Commands;

public record DeleteLaboratoryPriceItemCommand(Guid PublicId) : IRequest<Unit>;

public class DeleteLaboratoryPriceItemCommandHandler
    : IRequestHandler<DeleteLaboratoryPriceItemCommand, Unit>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public DeleteLaboratoryPriceItemCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<Unit> Handle(DeleteLaboratoryPriceItemCommand request, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct)
            ?? throw new ForbiddenException("Şirket bağlamı gerekli.");

        var item = await _db.LaboratoryPriceItems
            .Include(p => p.Laboratory)
            .FirstOrDefaultAsync(p => p.PublicId == request.PublicId
                                       && p.Laboratory.CompanyId == companyId, ct)
            ?? throw new NotFoundException("Fiyat kalemi bulunamadı.");

        item.SoftDelete();
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
