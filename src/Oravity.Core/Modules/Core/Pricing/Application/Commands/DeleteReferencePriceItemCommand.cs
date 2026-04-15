using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Pricing.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Pricing.Application.Commands;

public record DeleteReferencePriceItemCommand(long ListId, string Code) : IRequest;

public class DeleteReferencePriceItemCommandHandler : IRequestHandler<DeleteReferencePriceItemCommand>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public DeleteReferencePriceItemCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task Handle(DeleteReferencePriceItemCommand request, CancellationToken cancellationToken)
    {
        await TenantCompanyResolver.ResolveCompanyIdAsync(_tenant, _db, cancellationToken);

        var item = await _db.ReferencePriceItems
            .FirstOrDefaultAsync(i => i.ListId == request.ListId
                                   && i.TreatmentCode == request.Code.Trim().ToUpperInvariant(),
                                  cancellationToken)
            ?? throw new NotFoundException("Kalem bulunamadı.");

        _db.ReferencePriceItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
