using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;

namespace Oravity.Core.Modules.Core.Pricing.Application.Queries;

public record GetReferencePriceListsQuery : IRequest<IReadOnlyList<ReferencePriceListResponse>>;

public class GetReferencePriceListsQueryHandler
    : IRequestHandler<GetReferencePriceListsQuery, IReadOnlyList<ReferencePriceListResponse>>
{
    private readonly AppDbContext _db;

    public GetReferencePriceListsQueryHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ReferencePriceListResponse>> Handle(
        GetReferencePriceListsQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.ReferencePriceLists
            .AsNoTracking()
            .OrderBy(l => l.Code)
            .Select(l => new ReferencePriceListResponse(
                l.Id,
                l.Code,
                l.Name,
                l.SourceType,
                l.Year,
                l.IsActive,
                l.Items.Count))
            .ToListAsync(cancellationToken);
    }
}
