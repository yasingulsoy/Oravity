using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Core.Pricing.Application.Queries;

public record GetReferencePriceItemsQuery(
    long   ListId,
    string? Search   = null,
    int    Page      = 1,
    int    PageSize  = 50
) : IRequest<ReferencePriceItemsPagedResponse>;

public record ReferencePriceItemsPagedResponse(
    IReadOnlyList<ReferencePriceItemResponse> Items,
    int Total,
    int Page,
    int PageSize
);

public class GetReferencePriceItemsQueryHandler
    : IRequestHandler<GetReferencePriceItemsQuery, ReferencePriceItemsPagedResponse>
{
    private readonly AppDbContext _db;

    public GetReferencePriceItemsQueryHandler(AppDbContext db) => _db = db;

    public async Task<ReferencePriceItemsPagedResponse> Handle(
        GetReferencePriceItemsQuery request,
        CancellationToken cancellationToken)
    {
        var listExists = await _db.ReferencePriceLists.AnyAsync(l => l.Id == request.ListId, cancellationToken);
        if (!listExists)
            throw new NotFoundException("Referans fiyat listesi bulunamadı.");

        var query = _db.ReferencePriceItems
            .AsNoTracking()
            .Where(i => i.ListId == request.ListId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.ToLower();
            query = query.Where(i =>
                i.TreatmentCode.ToLower().Contains(term) ||
                i.TreatmentName.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(i => i.TreatmentCode)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new ReferencePriceItemResponse(
                i.Id,
                i.TreatmentCode,
                i.TreatmentName,
                i.Price,
                i.PriceKdv,
                i.Currency,
                i.ValidFrom,
                i.ValidUntil))
            .ToListAsync(cancellationToken);

        return new ReferencePriceItemsPagedResponse(items, total, request.Page, request.PageSize);
    }
}
