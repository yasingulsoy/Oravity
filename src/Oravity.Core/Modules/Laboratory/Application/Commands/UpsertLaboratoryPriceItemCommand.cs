using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Laboratory.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Commands;

public record UpsertLaboratoryPriceItemCommand(
    Guid     LaboratoryPublicId,
    Guid?    PublicId,          // null → create, değilse update
    string   ItemName,
    string?  ItemCode,
    string?  Description,
    decimal  Price,
    string   Currency,
    string?  PricingType,
    int?     EstimatedDeliveryDays,
    string?  Category,
    DateOnly? ValidFrom,
    DateOnly? ValidUntil,
    bool     IsActive
) : IRequest<LaboratoryPriceItemResponse>;

public class UpsertLaboratoryPriceItemCommandHandler
    : IRequestHandler<UpsertLaboratoryPriceItemCommand, LaboratoryPriceItemResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public UpsertLaboratoryPriceItemCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<LaboratoryPriceItemResponse> Handle(
        UpsertLaboratoryPriceItemCommand request,
        CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct)
            ?? throw new ForbiddenException("Şirket bağlamı gerekli.");

        var lab = await _db.Laboratories.AsNoTracking()
            .FirstOrDefaultAsync(l => l.PublicId == request.LaboratoryPublicId
                                       && l.CompanyId == companyId, ct)
            ?? throw new NotFoundException("Laboratuvar bulunamadı.");

        LaboratoryPriceItem item;

        if (request.PublicId is { } pid)
        {
            item = await _db.LaboratoryPriceItems
                .FirstOrDefaultAsync(p => p.PublicId == pid && p.LaboratoryId == lab.Id, ct)
                ?? throw new NotFoundException("Fiyat kalemi bulunamadı.");

            item.Update(request.ItemName, request.ItemCode, request.Description,
                request.Price, request.Currency, request.PricingType,
                request.EstimatedDeliveryDays, request.Category,
                request.ValidFrom, request.ValidUntil);
            item.SetActive(request.IsActive);
        }
        else
        {
            item = LaboratoryPriceItem.Create(
                lab.Id, request.ItemName, request.ItemCode, request.Description,
                request.Price, request.Currency, request.PricingType,
                request.EstimatedDeliveryDays, request.Category,
                request.ValidFrom, request.ValidUntil);
            if (!request.IsActive) item.SetActive(false);
            _db.LaboratoryPriceItems.Add(item);
        }

        await _db.SaveChangesAsync(ct);

        return LaboratoryMappings.ToResponse(item);
    }
}
