using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Core.Pricing.Application;

// ─── Response DTO'lar ──────────────────────────────────────────────────────

public record PricingRuleResponse(
    Guid      PublicId,
    string    Name,
    string?   Description,
    string    RuleType,
    int       Priority,
    string?   IncludeFilters,
    string?   ExcludeFilters,
    string?   Formula,
    string    OutputCurrency,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    bool      IsActive,
    bool      StopProcessing
);

public record TreatmentMappingResponse(
    long    Id,
    long    InternalTreatmentId,
    string  InternalTreatmentCode,
    string  InternalTreatmentName,
    long    ReferenceListId,
    string  ReferenceListCode,
    string  ReferenceCode,
    string? MappingQuality,
    string? Notes
);

public record CalculatePriceResponse(
    decimal FinalPrice,
    decimal OriginalPrice,
    decimal TotalDiscount,
    string  AppliedStrategy,
    string  Currency
);

// ─── Mappings ──────────────────────────────────────────────────────────────

public static class PricingMappings
{
    public static PricingRuleResponse ToResponse(PricingRule r)
        => new(r.PublicId, r.Name, r.Description, r.RuleType, r.Priority,
               r.IncludeFilters, r.ExcludeFilters, r.Formula, r.OutputCurrency,
               r.ValidFrom, r.ValidUntil, r.IsActive, r.StopProcessing);

    public static TreatmentMappingResponse ToResponse(TreatmentMapping m)
        => new(m.Id,
               m.InternalTreatmentId,
               m.InternalTreatment?.Code ?? string.Empty,
               m.InternalTreatment?.Name ?? string.Empty,
               m.ReferenceListId,
               m.ReferenceList?.Code ?? string.Empty,
               m.ReferenceCode,
               m.MappingQuality,
               m.Notes);
}
