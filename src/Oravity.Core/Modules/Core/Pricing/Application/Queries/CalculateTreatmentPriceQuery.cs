using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Pricing.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using Oravity.SharedKernel.Services;

namespace Oravity.Core.Modules.Core.Pricing.Application.Queries;

public record CalculateTreatmentPriceQuery(
    Guid    TreatmentPublicId,
    decimal BasePrice,
    decimal? InstitutionPrice       = null,
    decimal? CampaignDiscountRate   = null,
    bool     IsInstitutionAgreement = false
) : IRequest<CalculatePriceResponse>;

public class CalculateTreatmentPriceQueryHandler
    : IRequestHandler<CalculateTreatmentPriceQuery, CalculatePriceResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;
    private readonly PricingEngine  _pricingEngine;

    public CalculateTreatmentPriceQueryHandler(
        AppDbContext db,
        ITenantContext tenant,
        PricingEngine pricingEngine)
    {
        _db            = db;
        _tenant        = tenant;
        _pricingEngine = pricingEngine;
    }

    public async Task<CalculatePriceResponse> Handle(
        CalculateTreatmentPriceQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenant.CompanyId
            ?? throw new ForbiddenException("Fiyat hesaplamak için şirket bağlamı gereklidir.");

        var treatment = await _db.Treatments
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.PublicId == request.TreatmentPublicId
                                   && (t.CompanyId == null || t.CompanyId == companyId), cancellationToken)
            ?? throw new NotFoundException("Tedavi bulunamadı.");

        var ctx = new PricingContext
        {
            BasePrice               = request.BasePrice,
            InstitutionPrice        = request.InstitutionPrice,
            CampaignDiscountRate    = request.CampaignDiscountRate,
            IsInstitutionAgreement  = request.IsInstitutionAgreement
        };

        var result = _pricingEngine.Calculate(ctx);

        return new CalculatePriceResponse(
            result.FinalPrice,
            result.OriginalPrice,
            result.TotalDiscount,
            result.AppliedStrategy,
            "TRY");
    }
}
