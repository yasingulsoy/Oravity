using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Commission.Application.Commands;

public record DeleteCommissionTemplateCommand(Guid PublicId) : IRequest<Unit>;

public class DeleteCommissionTemplateCommandHandler
    : IRequestHandler<DeleteCommissionTemplateCommand, Unit>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public DeleteCommissionTemplateCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Unit> Handle(DeleteCommissionTemplateCommand r, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct)
            ?? throw new ForbiddenException("Şirket bağlamı gereklidir.");

        var template = await _db.DoctorCommissionTemplates
            .FirstOrDefaultAsync(t => t.PublicId == r.PublicId && t.CompanyId == companyId, ct)
            ?? throw new NotFoundException($"Şablon bulunamadı: {r.PublicId}");

        var hasAssignments = await _db.DoctorTemplateAssignments.AsNoTracking()
            .AnyAsync(a => a.TemplateId == template.Id, ct);
        if (hasAssignments)
            throw new InvalidOperationException("Aktif atamaları olan şablon silinemez; pasif yapın.");

        template.SoftDelete();
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
