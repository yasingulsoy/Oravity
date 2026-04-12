using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Infrastructure.Database;

namespace Oravity.Core.Modules.Visit.Application.Queries;

public record SearchIcdCodesQuery(string? Query, int? Type = null, int Limit = 20)
    : IRequest<IReadOnlyList<IcdCodeResponse>>;

public class SearchIcdCodesQueryHandler : IRequestHandler<SearchIcdCodesQuery, IReadOnlyList<IcdCodeResponse>>
{
    private readonly AppDbContext _db;

    public SearchIcdCodesQueryHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<IcdCodeResponse>> Handle(SearchIcdCodesQuery request, CancellationToken ct)
    {
        var q = _db.IcdCodes.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted);

        if (request.Type.HasValue)
            q = q.Where(x => x.Type == request.Type.Value);

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var term = request.Query.Trim().ToLower();
            q = q.Where(x =>
                x.Code.ToLower().StartsWith(term) ||
                x.Description.ToLower().Contains(term));
        }

        var items = await q
            .OrderBy(x => x.Code)
            .Take(request.Limit)
            .Select(x => new IcdCodeResponse(x.Id, x.Code, x.Description, x.Category, x.Type))
            .ToListAsync(ct);

        return items;
    }
}
