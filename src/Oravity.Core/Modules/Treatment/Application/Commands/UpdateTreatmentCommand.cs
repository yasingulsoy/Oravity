using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Treatment.Application.Commands;

public record UpdateTreatmentCommand(
    Guid     PublicId,
    string   Code,
    string   Name,
    Guid?    CategoryPublicId,
    decimal  KdvRate,
    bool     RequiresSurfaceSelection,
    bool     RequiresLaboratory,
    int[]?   AllowedScopes,
    string?  Tags,
    bool     IsActive
) : IRequest<TreatmentResponse>;

public class UpdateTreatmentCommandHandler
    : IRequestHandler<UpdateTreatmentCommand, TreatmentResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public UpdateTreatmentCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<TreatmentResponse> Handle(
        UpdateTreatmentCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenant.CompanyId
            ?? throw new ForbiddenException("Tedavi güncellemek için şirket bağlamı gereklidir.");

        var treatment = await _db.Treatments
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.PublicId == request.PublicId
                                   && t.CompanyId == companyId, cancellationToken)
            ?? throw new NotFoundException("Tedavi bulunamadı.");

        var codeUpper = request.Code.Trim().ToUpperInvariant();
        var codeTaken = await _db.Treatments.AnyAsync(
            t => t.CompanyId == companyId
              && t.Code == codeUpper
              && t.Id != treatment.Id, cancellationToken);
        if (codeTaken)
            throw new ConflictException($"'{codeUpper}' kodu başka bir tedavide kullanılıyor.");

        long? categoryId = null;
        if (request.CategoryPublicId.HasValue)
        {
            var cat = await _db.TreatmentCategories
                .FirstOrDefaultAsync(c => c.PublicId == request.CategoryPublicId.Value
                                       && c.CompanyId == companyId, cancellationToken)
                ?? throw new NotFoundException("Kategori bulunamadı.");
            categoryId = cat.Id;
        }

        treatment.Update(
            request.Code,
            request.Name,
            categoryId,
            request.KdvRate,
            request.RequiresSurfaceSelection,
            request.RequiresLaboratory,
            request.AllowedScopes,
            request.Tags);

        treatment.SetActive(request.IsActive);

        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(treatment).Reference(t => t.Category).LoadAsync(cancellationToken);
        return TreatmentCatalogMappings.ToResponse(treatment);
    }
}
