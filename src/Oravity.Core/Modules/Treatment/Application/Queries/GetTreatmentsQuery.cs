using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Treatment.Application.Queries;

public record GetTreatmentsQuery(
    long? CompanyId,
    Guid? CategoryPublicId = null,
    string? Search        = null,
    bool   ActiveOnly     = true,
    int    Page           = 1,
    int    PageSize       = 20
) : IRequest<PagedTreatmentResponse>;

public class GetTreatmentsQueryHandler
    : IRequestHandler<GetTreatmentsQuery, PagedTreatmentResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public GetTreatmentsQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<PagedTreatmentResponse> Handle(
        GetTreatmentsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_tenant.IsPlatformAdmin && request.CompanyId.HasValue && _tenant.CompanyId != request.CompanyId)
            throw new ForbiddenException("Bu şirketin tedavilerine erişim yetkisi yok.");

        var query = _db.Treatments
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.CompanyId == null || (request.CompanyId != null && t.CompanyId == request.CompanyId));

        if (request.ActiveOnly)
            query = query.Where(t => t.IsActive);

        if (request.CategoryPublicId.HasValue)
        {
            var cat = await _db.TreatmentCategories
                .FirstOrDefaultAsync(c => c.PublicId == request.CategoryPublicId.Value, cancellationToken)
                ?? throw new NotFoundException("Kategori bulunamadı.");
            query = query.Where(t => t.CategoryId == cat.Id);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(term) ||
                t.Code.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        var page     = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderBy(t => t.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedTreatmentResponse(
            items.Select(TreatmentCatalogMappings.ToResponse).ToList(),
            total,
            page,
            pageSize);
    }
}
