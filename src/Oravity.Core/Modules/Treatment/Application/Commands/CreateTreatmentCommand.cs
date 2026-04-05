using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using TreatmentEntity = Oravity.SharedKernel.Entities.Treatment;

namespace Oravity.Core.Modules.Treatment.Application.Commands;

public record CreateTreatmentCommand(
    string   Code,
    string   Name,
    Guid?    CategoryPublicId,
    decimal  KdvRate,
    bool     RequiresSurfaceSelection,
    bool     RequiresLaboratory,
    int[]?   AllowedScopes,
    string?  Tags
) : IRequest<TreatmentResponse>;

public class CreateTreatmentCommandHandler
    : IRequestHandler<CreateTreatmentCommand, TreatmentResponse>
{
    private readonly AppDbContext    _db;
    private readonly ITenantContext  _tenant;

    public CreateTreatmentCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<TreatmentResponse> Handle(
        CreateTreatmentCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenant.CompanyId
            ?? throw new ForbiddenException("Tedavi oluşturmak için şirket bağlamı gereklidir.");

        // Aynı şirkette aynı kod var mı?
        var codeUpper = request.Code.Trim().ToUpperInvariant();
        var exists = await _db.Treatments
            .AnyAsync(t => t.CompanyId == companyId && t.Code == codeUpper, cancellationToken);
        if (exists)
            throw new ConflictException($"'{codeUpper}' kodu bu şirkette zaten kullanılıyor.");

        long? categoryId = null;
        if (request.CategoryPublicId.HasValue)
        {
            var cat = await _db.TreatmentCategories
                .FirstOrDefaultAsync(c => c.PublicId == request.CategoryPublicId.Value
                                       && c.CompanyId == companyId, cancellationToken)
                ?? throw new NotFoundException("Kategori bulunamadı.");
            categoryId = cat.Id;
        }

        var treatment = TreatmentEntity.Create(
            companyId,
            request.Code,
            request.Name,
            categoryId,
            request.KdvRate,
            request.RequiresSurfaceSelection,
            request.RequiresLaboratory,
            request.AllowedScopes,
            request.Tags);

        _db.Treatments.Add(treatment);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(treatment).Reference(t => t.Category).LoadAsync(cancellationToken);
        return TreatmentCatalogMappings.ToResponse(treatment);
    }
}
