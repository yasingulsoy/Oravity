using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Pricing.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Pricing.Application.Commands;

public record BulkUpsertItem(
    string  Code,
    string  Name,
    decimal Price,
    decimal PriceKdv  = 0,
    string  Currency  = "TRY"
);

public record BulkUpsertReferencePriceItemsCommand(
    long            ListId,
    BulkUpsertItem[] Items
) : IRequest<int>;  // returns count of upserted items

public class BulkUpsertReferencePriceItemsCommandHandler
    : IRequestHandler<BulkUpsertReferencePriceItemsCommand, int>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public BulkUpsertReferencePriceItemsCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<int> Handle(
        BulkUpsertReferencePriceItemsCommand request,
        CancellationToken cancellationToken)
    {
        await TenantCompanyResolver.ResolveCompanyIdAsync(_tenant, _db, cancellationToken);

        var listExists = await _db.ReferencePriceLists
            .AnyAsync(l => l.Id == request.ListId, cancellationToken);
        if (!listExists)
            throw new NotFoundException("Referans fiyat listesi bulunamadı.");

        var codes = request.Items
            .Select(i => i.Code.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        var existing = await _db.ReferencePriceItems
            .Where(i => i.ListId == request.ListId && codes.Contains(i.TreatmentCode))
            .ToDictionaryAsync(i => i.TreatmentCode, cancellationToken);

        int count = 0;
        foreach (var item in request.Items)
        {
            var code = item.Code.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code)) continue;

            var currency = string.IsNullOrWhiteSpace(item.Currency) ? "TRY" : item.Currency.ToUpperInvariant();

            if (existing.TryGetValue(code, out var current))
            {
                current.SetPrice(item.Price, item.PriceKdv, currency);
            }
            else
            {
                var newItem = ReferencePriceItem.Create(
                    request.ListId,
                    code,
                    item.Name.Trim(),
                    item.Price,
                    item.PriceKdv,
                    currency,
                    null,
                    null);
                _db.ReferencePriceItems.Add(newItem);
            }
            count++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return count;
    }
}
