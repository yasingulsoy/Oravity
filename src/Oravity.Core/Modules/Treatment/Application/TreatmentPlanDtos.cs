using Oravity.SharedKernel.Entities;
using PlanEntity = Oravity.SharedKernel.Entities.TreatmentPlan;
using ItemEntity = Oravity.SharedKernel.Entities.TreatmentPlanItem;

namespace Oravity.Core.Modules.Treatment.Application;

// ─── Response DTO'lar ──────────────────────────────────────────────────────

public record TreatmentPlanItemResponse(
    Guid PublicId,
    long PlanId,
    long TreatmentId,
    string? ToothNumber,
    string? ToothSurfaces,
    string? BodyRegionCode,
    TreatmentItemStatus Status,
    string StatusLabel,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal FinalPrice,
    long? DoctorId,
    string? Notes,
    DateTime? CompletedAt,
    DateTime CreatedAt
);

public record TreatmentPlanResponse(
    Guid PublicId,
    long PatientId,
    long BranchId,
    long DoctorId,
    string Name,
    TreatmentPlanStatus Status,
    string StatusLabel,
    string? Notes,
    DateTime CreatedAt,
    IReadOnlyList<TreatmentPlanItemResponse> Items
);

// ─── Mapping ──────────────────────────────────────────────────────────────

public static class TreatmentPlanMappings
{
    public static TreatmentPlanItemResponse ToResponse(ItemEntity i) => new(
        i.PublicId,
        i.PlanId,
        i.TreatmentId,
        i.ToothNumber,
        i.ToothSurfaces,
        i.BodyRegionCode,
        i.Status,
        ItemStatusLabel(i.Status),
        i.UnitPrice,
        i.DiscountRate,
        i.FinalPrice,
        i.DoctorId,
        i.Notes,
        i.CompletedAt,
        i.CreatedAt
    );

    public static TreatmentPlanResponse ToResponse(PlanEntity p, IReadOnlyList<TreatmentPlanItemResponse>? items = null) => new(
        p.PublicId,
        p.PatientId,
        p.BranchId,
        p.DoctorId,
        p.Name,
        p.Status,
        PlanStatusLabel(p.Status),
        p.Notes,
        p.CreatedAt,
        items ?? p.Items.Select(ToResponse).ToList()
    );

    public static string PlanStatusLabel(TreatmentPlanStatus s) => s switch
    {
        TreatmentPlanStatus.Draft     => "Taslak",
        TreatmentPlanStatus.Approved  => "Onaylandı",
        TreatmentPlanStatus.Completed => "Tamamlandı",
        TreatmentPlanStatus.Cancelled => "İptal",
        _ => s.ToString()
    };

    public static string ItemStatusLabel(TreatmentItemStatus s) => s switch
    {
        TreatmentItemStatus.Planned   => "Planlandı",
        TreatmentItemStatus.Approved  => "Onaylandı",
        TreatmentItemStatus.Completed => "Tamamlandı",
        TreatmentItemStatus.Cancelled => "İptal",
        _ => s.ToString()
    };
}
