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
        var trace = new List<PricingTraceStep>();

        // CompanyId'yi sırayla çöz: JWT → BranchId → UserRoleAssignment
        var companyId = _tenant.CompanyId;
        var companySource = "JWT claim";

        if (companyId == null && _tenant.BranchId.HasValue)
        {
            companyId = await _db.Branches.AsNoTracking()
                .Where(b => b.Id == _tenant.BranchId.Value)
                .Select(b => (long?)b.CompanyId)
                .FirstOrDefaultAsync(cancellationToken);
            if (companyId != null) companySource = $"Branch({_tenant.BranchId}) → Company";
        }

        if (companyId == null && _tenant.UserId > 0)
        {
            var assignment = await _db.UserRoleAssignments.AsNoTracking()
                .Where(a => a.UserId == _tenant.UserId && a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (assignment != null)
            {
                companyId = assignment.CompanyId;
                companySource = "UserRoleAssignment";

                if (companyId == null && assignment.BranchId.HasValue)
                {
                    companyId = await _db.Branches.AsNoTracking()
                        .Where(b => b.Id == assignment.BranchId.Value)
                        .Select(b => (long?)b.CompanyId)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (companyId != null) companySource = $"UserRoleAssignment.Branch({assignment.BranchId}) → Company";
                }
            }
        }

        trace.Add(new("Tenant", $"UserId={_tenant.UserId}, CompanyId={companyId?.ToString() ?? "null"} (kaynak: {companySource}), BranchId={_tenant.BranchId?.ToString() ?? "null"}"));

        if (companyId == null)
        {
            trace.Add(new("Sonuç", "Şirket bağlamı çözülemedi → fiyat hesaplanamaz", "⚠️ HATA"));
            return new TreatmentPriceResponse(0, 0, "TRY", null, "NoPriceConfigured", ToDto(trace));
        }

        // Tedaviyi bul
        var treatment = await _db.Treatments
            .AsNoTracking()
            .Include(t => t.Category)
            .FirstOrDefaultAsync(
                t => t.PublicId == request.TreatmentPublicId
                  && (t.CompanyId == null || t.CompanyId == companyId),
                cancellationToken)
            ?? throw new NotFoundException("Tedavi bulunamadı.");

        trace.Add(new("Tedavi", $"\"{treatment.Name}\" (kod: {treatment.Code}, ID: {treatment.Id}, kategori: {treatment.Category?.Name ?? "yok"})"));

        // Adım 1: Mapping'leri çek
        var mappings = await _db.TreatmentMappings
            .AsNoTracking()
            .Where(m => m.InternalTreatmentId == treatment.Id)
            .Select(m => new { m.ReferenceListId, m.ReferenceCode })
            .ToListAsync(cancellationToken);

        trace.Add(new("Eşleştirme", mappings.Count > 0
            ? $"{mappings.Count} referans eşleştirme bulundu"
            : "Referans eşleştirme bulunamadı → referans fiyat çekilemez"));

        // Adım 2: Referans fiyatlar
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

            trace.Add(new("Referans Fiyat", $"{listCode} → kod \"{mapping.ReferenceCode}\" → {item.Price:N2} {item.Currency}"));
        }

        var tdbKey   = referencePrices.Keys.FirstOrDefault(k => k.StartsWith("TDB", StringComparison.OrdinalIgnoreCase))
                    ?? referencePrices.Keys.FirstOrDefault();
        var tdbPrice = tdbKey != null ? referencePrices[tdbKey] : 0m;

        if (tdbKey != null)
            trace.Add(new("TDB Fiyat", $"Temel referans: {tdbKey} = {tdbPrice:N2}"));

        // Şube + MULTI
        var branchId = request.BranchId ?? _tenant.BranchId;
        var pricingMultiplier = 1.0m;
        string? branchName = null;

        if (branchId.HasValue)
        {
            var branchInfo = await _db.Branches.AsNoTracking()
                .Where(b => b.Id == branchId.Value)
                .Select(b => new { b.PricingMultiplier, b.Name })
                .FirstOrDefaultAsync(cancellationToken);
            if (branchInfo != null)
            {
                pricingMultiplier = branchInfo.PricingMultiplier > 0 ? branchInfo.PricingMultiplier : 1.0m;
                branchName = branchInfo.Name;
            }
        }

        trace.Add(new("Şube", branchId.HasValue
            ? $"\"{branchName}\" (ID: {branchId}), MULTI çarpanı = {pricingMultiplier}"
            : "Şube seçilmedi, MULTI = 1.0"));

        // Kuralları çek
        var rules = await _db.PricingRules
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId && r.IsActive)
            .Where(r => r.BranchId == null || r.BranchId == branchId)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

        trace.Add(new("Kurallar", $"{rules.Count} aktif kural bulundu (şirket geneli + şubeye özel)"));

        if (rules.Count > 0 && referencePrices.Count > 0)
        {
            var evalCtx = new RuleEvalContext
            {
                TreatmentId       = treatment.Id,
                CategoryId        = treatment.CategoryId,
                CategoryPublicId  = treatment.Category?.PublicId,
                TreatmentCode     = treatment.Code,
                ReferencePrices   = referencePrices,
                PricingMultiplier = pricingMultiplier,
                InstitutionId     = request.InstitutionId,
                IsOss             = request.IsOss,
                CampaignCode      = request.CampaignCode,
            };

            var result = _engine.CalculateWithRules(evalCtx, rules, trace);
            if (result != null)
            {
                return new TreatmentPriceResponse(
                    UnitPrice      : result.FinalPrice,
                    ReferencePrice : result.OriginalPrice,
                    Currency       : result.Currency,
                    AppliedRuleName: result.AppliedRuleName,
                    Strategy       : "Rule",
                    Trace          : ToDto(trace));
            }
        }

        if (tdbPrice > 0)
        {
            var currency = tdbCurrency ?? "TRY";
            trace.Add(new("Sonuç", $"Eşleşen kural yok → TDB referans fiyatı kullanılıyor: {tdbPrice:N2} {currency}", "📋 REFERANS"));
            return new TreatmentPriceResponse(
                UnitPrice      : tdbPrice,
                ReferencePrice : tdbPrice,
                Currency       : currency,
                AppliedRuleName: null,
                Strategy       : "ReferencePrice",
                Trace          : ToDto(trace));
        }

        trace.Add(new("Sonuç", "Ne kural ne referans fiyat bulundu → fiyat hesaplanamaz", "⚠️ FİYAT YOK"));
        return new TreatmentPriceResponse(
            UnitPrice      : 0,
            ReferencePrice : 0,
            Currency       : "TRY",
            AppliedRuleName: null,
            Strategy       : "NoPriceConfigured",
            Trace          : ToDto(trace));
    }

    private static List<TraceStepDto> ToDto(List<PricingTraceStep> trace)
        => trace.Select(t => new TraceStepDto(t.Phase, t.Detail, t.Result)).ToList();
}
