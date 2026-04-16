using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using Oravity.SharedKernel.Services;

namespace Oravity.Core.Modules.Core.Pricing.Application.Queries;

public record GetTreatmentPriceQuery(
    Guid    TreatmentPublicId,
    long?   BranchId       = null,
    long?   InstitutionId  = null,
    bool    IsOss          = false,
    string? CampaignCode   = null
) : IRequest<TreatmentPriceResponse>;

public class GetTreatmentPriceQueryHandler
    : IRequestHandler<GetTreatmentPriceQuery, TreatmentPriceResponse>
{
    private readonly AppDbContext  _db;
    private readonly ITenantContext _tenant;
    private readonly PricingEngine  _engine;

    public GetTreatmentPriceQueryHandler(
        AppDbContext db,
        ITenantContext tenant,
        PricingEngine engine)
    {
        _db     = db;
        _tenant = tenant;
        _engine = engine;
    }

    public async Task<TreatmentPriceResponse> Handle(
        GetTreatmentPriceQuery request,
        CancellationToken cancellationToken)
    {
        // CompanyId'yi sırayla çöz: JWT → BranchId → UserRoleAssignment
        var companyId = _tenant.CompanyId;

        if (companyId == null && _tenant.BranchId.HasValue)
            companyId = await _db.Branches.AsNoTracking()
                .Where(b => b.Id == _tenant.BranchId.Value)
                .Select(b => (long?)b.CompanyId)
                .FirstOrDefaultAsync(cancellationToken);

        if (companyId == null && _tenant.UserId > 0)
        {
            var assignment = await _db.UserRoleAssignments.AsNoTracking()
                .Where(a => a.UserId == _tenant.UserId && a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (assignment != null)
            {
                companyId = assignment.CompanyId;

                if (companyId == null && assignment.BranchId.HasValue)
                    companyId = await _db.Branches.AsNoTracking()
                        .Where(b => b.Id == assignment.BranchId.Value)
                        .Select(b => (long?)b.CompanyId)
                        .FirstOrDefaultAsync(cancellationToken);
            }
        }

        // Şirket bağlamı çözülemediyse fiyatsız dön — kullanıcı elle girer
        if (companyId == null)
            return new TreatmentPriceResponse(0, 0, "TRY", null, "NoPriceConfigured");

        // Tedaviyi bul
        var treatment = await _db.Treatments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.PublicId == request.TreatmentPublicId
                  && (t.CompanyId == null || t.CompanyId == companyId),
                cancellationToken)
            ?? throw new NotFoundException("Tedavi bulunamadı.");

        // Adım 1: Bu tedavinin mapping'lerini çek
        var mappings = await _db.TreatmentMappings
            .AsNoTracking()
            .Where(m => m.InternalTreatmentId == treatment.Id)
            .Select(m => new { m.ReferenceListId, m.ReferenceCode })
            .ToListAsync(cancellationToken);

        // Adım 2: Mapping'lerdeki referans fiyatları + liste kodlarını çek
        var referencePrices = new Dictionary<string, decimal>();
        string? tdbCurrency = null;

        foreach (var mapping in mappings)
        {
            var item = await _db.ReferencePriceItems
                .AsNoTracking()
                .Where(rpi => rpi.ListId == mapping.ReferenceListId
                           && rpi.TreatmentCode == mapping.ReferenceCode)
                .Select(rpi => new { rpi.Price, rpi.Currency })
                .FirstOrDefaultAsync(cancellationToken);

            if (item == null) continue;

            var listCode = await _db.ReferencePriceLists
                .AsNoTracking()
                .Where(rpl => rpl.Id == mapping.ReferenceListId)
                .Select(rpl => rpl.Code)
                .FirstOrDefaultAsync(cancellationToken);

            if (listCode == null) continue;

            referencePrices[listCode] = item.Price;
            if (listCode == "TDB") tdbCurrency = item.Currency;
        }

        // TDB veya TDB_* kodlu listeyi bul, yoksa ilk listeyi kullan
        var tdbKey   = referencePrices.Keys.FirstOrDefault(k => k.StartsWith("TDB", StringComparison.OrdinalIgnoreCase))
                    ?? referencePrices.Keys.FirstOrDefault();
        var tdbPrice = tdbKey != null ? referencePrices[tdbKey] : 0m;

        // Şirkete ait aktif kuralları çek (branch'e özel + şirket geneli)
        var branchId = request.BranchId ?? _tenant.BranchId;

        // Şube fiyat çarpanını çek (MULTI değişkeni için)
        var pricingMultiplier = 1.0m;
        if (branchId.HasValue)
        {
            pricingMultiplier = await _db.Branches.AsNoTracking()
                .Where(b => b.Id == branchId.Value)
                .Select(b => b.PricingMultiplier)
                .FirstOrDefaultAsync(cancellationToken);
            if (pricingMultiplier <= 0) pricingMultiplier = 1.0m;
        }

        var rules = await _db.PricingRules
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.IsActive)
            .Where(r => r.BranchId == null || r.BranchId == branchId)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

        if (rules.Count > 0 && referencePrices.Count > 0)
        {
            var evalCtx = new RuleEvalContext
            {
                TreatmentId       = treatment.Id,
                CategoryId        = treatment.CategoryId,
                TreatmentCode     = treatment.Code,
                ReferencePrices   = referencePrices,
                PricingMultiplier = pricingMultiplier,
                InstitutionId     = request.InstitutionId,
                IsOss             = request.IsOss,
                CampaignCode      = request.CampaignCode,
            };

            var result = _engine.CalculateWithRules(evalCtx, rules);
            if (result != null)
            {
                return new TreatmentPriceResponse(
                    UnitPrice      : result.FinalPrice,
                    ReferencePrice : result.OriginalPrice,
                    Currency       : result.Currency,
                    AppliedRuleName: result.AppliedRuleName,
                    Strategy       : "Rule");
            }
        }

        // Kural yoksa veya eşleşme yoksa → TDB fiyatını direkt kullan
        if (tdbPrice > 0)
        {
            var currency = tdbCurrency ?? referencePrices.Keys.Select(_ => "TRY").FirstOrDefault() ?? "TRY";
            return new TreatmentPriceResponse(
                UnitPrice      : tdbPrice,
                ReferencePrice : tdbPrice,
                Currency       : currency,
                AppliedRuleName: null,
                Strategy       : "ReferencePrice");
        }

        return new TreatmentPriceResponse(
            UnitPrice      : 0,
            ReferencePrice : 0,
            Currency       : "TRY",
            AppliedRuleName: null,
            Strategy       : "NoPriceConfigured");
    }
}
