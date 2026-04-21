using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Pricing.Application;
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
    bool     IsActive,
    decimal? CostPrice = null,
    string?  ChartSymbolCode = null
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
        var isPlatformAdmin = _tenant.IsPlatformAdmin;

        // Platform admin global şablonları düzenleyebilir (CompanyId = null).
        // Diğer kullanıcılar yalnızca kendi şirketlerine ait tedavileri güncelleyebilir.
        SharedKernel.Entities.Treatment treatment;

        if (isPlatformAdmin)
        {
            treatment = await _db.Treatments
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.PublicId == request.PublicId, cancellationToken)
                ?? throw new NotFoundException("Tedavi bulunamadı.");
        }
        else
        {
            var companyId = await TenantCompanyResolver.ResolveCompanyIdAsync(_tenant, _db, cancellationToken)
                ?? throw new ForbiddenException("Tedavi güncellemek için şirket bağlamı gereklidir.");

            treatment = await _db.Treatments
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
        }

        long? categoryId = null;
        if (request.CategoryPublicId.HasValue)
        {
            var companyId = isPlatformAdmin ? null : await TenantCompanyResolver.ResolveCompanyIdAsync(_tenant, _db, cancellationToken);
            var cat = await _db.TreatmentCategories
                .FirstOrDefaultAsync(c => c.PublicId == request.CategoryPublicId.Value
                                       && (c.CompanyId == null || c.CompanyId == companyId), cancellationToken)
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
            request.Tags,
            costPrice: request.CostPrice,
            chartSymbolCode: request.ChartSymbolCode);

        treatment.SetActive(request.IsActive);

        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(treatment).Reference(t => t.Category).LoadAsync(cancellationToken);
        return TreatmentCatalogMappings.ToResponse(treatment);
    }
}
