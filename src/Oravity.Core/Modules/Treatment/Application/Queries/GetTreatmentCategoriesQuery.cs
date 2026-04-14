using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Treatment.Application.Queries;

public record GetTreatmentCategoriesQuery : IRequest<IReadOnlyList<TreatmentCategoryResponse>>;

public class GetTreatmentCategoriesQueryHandler
    : IRequestHandler<GetTreatmentCategoriesQuery, IReadOnlyList<TreatmentCategoryResponse>>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public GetTreatmentCategoriesQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<TreatmentCategoryResponse>> Handle(
        GetTreatmentCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await _db.TreatmentCategories
            .AsNoTracking()
            .Where(c => c.IsActive && (c.CompanyId == null || c.CompanyId == _tenant.CompanyId))
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        // Build id → publicId lookup to resolve ParentPublicId
        var idToPublicId = categories.ToDictionary(c => c.Id, c => c.PublicId);

        return categories
            .Select(c => new TreatmentCategoryResponse(
                c.PublicId,
                c.Name,
                c.ParentId.HasValue && idToPublicId.TryGetValue(c.ParentId.Value, out var pId) ? pId : null,
                c.SortOrder,
                c.IsActive))
            .ToList();
    }
}
