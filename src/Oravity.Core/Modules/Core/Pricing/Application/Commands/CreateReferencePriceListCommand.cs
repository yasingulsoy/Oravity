using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Core.Pricing.Application.Commands;

public record CreateReferencePriceListCommand(
    string Code,
    string Name,
    string SourceType,
    int    Year
) : IRequest<ReferencePriceListResponse>;

public class CreateReferencePriceListCommandHandler
    : IRequestHandler<CreateReferencePriceListCommand, ReferencePriceListResponse>
{
    private readonly AppDbContext _db;

    public CreateReferencePriceListCommandHandler(AppDbContext db) => _db = db;

    public async Task<ReferencePriceListResponse> Handle(
        CreateReferencePriceListCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        var exists = await _db.ReferencePriceLists
            .AnyAsync(l => l.Code == code, cancellationToken);

        if (exists)
            throw new InvalidOperationException($"'{code}' kodlu liste zaten mevcut.");

        var list = ReferencePriceList.Create(code, request.Name, request.SourceType, request.Year);
        _db.ReferencePriceLists.Add(list);
        await _db.SaveChangesAsync(cancellationToken);

        return new ReferencePriceListResponse(list.Id, list.Code, list.Name, list.SourceType, list.Year, list.IsActive, 0);
    }
}
