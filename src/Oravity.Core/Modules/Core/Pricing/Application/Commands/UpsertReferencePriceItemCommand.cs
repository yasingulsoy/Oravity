using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Core.Pricing.Application.Commands;

public record UpsertReferencePriceItemCommand(
    long     ListId,
    string   TreatmentCode,
    string   TreatmentName,
    decimal  Price,
    decimal  PriceKdv,
    string   Currency       = "TRY",
    DateTime? ValidFrom     = null,
    DateTime? ValidUntil    = null
) : IRequest<ReferencePriceItemResponse>;

public class UpsertReferencePriceItemCommandHandler
    : IRequestHandler<UpsertReferencePriceItemCommand, ReferencePriceItemResponse>
{
    private readonly AppDbContext _db;

    public UpsertReferencePriceItemCommandHandler(AppDbContext db) => _db = db;

    public async Task<ReferencePriceItemResponse> Handle(
        UpsertReferencePriceItemCommand request,
        CancellationToken cancellationToken)
    {
        var list = await _db.ReferencePriceLists
            .FirstOrDefaultAsync(l => l.Id == request.ListId, cancellationToken)
            ?? throw new NotFoundException("Referans fiyat listesi bulunamadı.");

        var code = request.TreatmentCode.Trim().ToUpperInvariant();

        var item = await _db.ReferencePriceItems
            .FirstOrDefaultAsync(i => i.ListId == request.ListId && i.TreatmentCode == code, cancellationToken);

        if (item is null)
        {
            item = ReferencePriceItem.Create(
                request.ListId,
                code,
                request.TreatmentName,
                request.Price,
                request.PriceKdv,
                request.Currency,
                request.ValidFrom.HasValue ? DateTime.SpecifyKind(request.ValidFrom.Value, DateTimeKind.Utc) : null,
                request.ValidUntil.HasValue ? DateTime.SpecifyKind(request.ValidUntil.Value, DateTimeKind.Utc) : null);
            _db.ReferencePriceItems.Add(item);
        }
        else
        {
            item.SetPrice(request.Price, request.PriceKdv, request.Currency);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new ReferencePriceItemResponse(
            item.Id,
            item.TreatmentCode,
            item.TreatmentName,
            item.Price,
            item.PriceKdv,
            item.Currency,
            item.ValidFrom,
            item.ValidUntil);
    }
}
