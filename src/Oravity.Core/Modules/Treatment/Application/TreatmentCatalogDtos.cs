using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Treatment.Application;

// ─── Response DTO'lar ──────────────────────────────────────────────────────

public record TreatmentCategoryResponse(
    Guid    PublicId,
    string  Name,
    Guid?   ParentPublicId,
    int     SortOrder,
    bool    IsActive
);

public record TreatmentResponse(
    Guid                        PublicId,
    string                      Code,
    string?                     SutCode,
    string                      Name,
    TreatmentCategoryResponse?  Category,
    string?                     Tags,
    decimal                     KdvRate,
    decimal?                    CostPrice,
    bool                        RequiresSurfaceSelection,
    bool                        RequiresLaboratory,
    int[]                       AllowedScopes,
    bool                        IsActive,
    bool                        IsGlobal,
    DateTime                    CreatedAt,
    string?                     ChartSymbolCode
);

public record PagedTreatmentResponse(
    IReadOnlyList<TreatmentResponse> Items,
    int Total,
    int Page,
    int PageSize
);

// ─── Mappings ──────────────────────────────────────────────────────────────

public static class TreatmentCatalogMappings
{
    public static TreatmentCategoryResponse ToResponse(TreatmentCategory c)
        => new(c.PublicId, c.Name, null, c.SortOrder, c.IsActive);

    public static TreatmentResponse ToResponse(SharedKernel.Entities.Treatment t)
        => new(
            t.PublicId,
            t.Code,
            t.SutCode,
            t.Name,
            t.Category is null ? null : ToResponse(t.Category),
            t.Tags,
            t.KdvRate,
            t.CostPrice,
            t.RequiresSurfaceSelection,
            t.RequiresLaboratory,
            t.AllowedScopes,
            t.IsActive,
            t.CompanyId is null,
            t.CreatedAt,
            t.ChartSymbolCode
        );
}
