using Oravity.SharedKernel.Entities;
using PlanEntity = Oravity.SharedKernel.Entities.TreatmentPlan;
using ItemEntity = Oravity.SharedKernel.Entities.TreatmentPlanItem;

namespace Oravity.Core.Modules.Treatment.Application;

// ─── Response DTO'lar ──────────────────────────────────────────────────────

public record TreatmentPlanItemResponse(
    Guid     PublicId,
    long     PlanId,
    long     TreatmentId,
    Guid?    TreatmentPublicId,
    string?  TreatmentCode,
    string?  TreatmentName,
    string?  ToothNumber,
    string?  ToothSurfaces,
    string?  BodyRegionCode,
    TreatmentItemStatus Status,
    string   StatusLabel,
    /// <summary>Referans fiyat listesindeki ham fiyat (kampanya/kural öncesi). Null → bilinmiyor.</summary>
    decimal? ListPrice,
    decimal  UnitPrice,
    decimal DiscountRate,
    decimal FinalPrice,
    decimal KdvRate,
    decimal KdvAmount,
    decimal TotalAmount,
    /// <summary>Fiyat para birimi (TRY, EUR, USD, …).</summary>
    string  PriceCurrency,
    /// <summary>Fiyat oluşturulurken kullanılan döviz kuru.</summary>
    decimal PriceExchangeRate,
    /// <summary>TRY bazında hesaplanan nihai fiyat.</summary>
    decimal PriceBaseAmount,
    long?   DoctorId,
    /// <summary>Kalemi gerçekleştirecek / gerçekleştiren hekim. Null ise plan hekimi.</summary>
    string? DoctorName,
    /// <summary>Kalemi "Onaylandı" statüsüne geçiren kullanıcı.</summary>
    string? ApprovedByName,
    /// <summary>Kalemin onaylandığı zaman.</summary>
    DateTime? ApprovedAt,
    string? Notes,
    DateTime? CompletedAt,
    DateTime  CreatedAt,
    /// <summary>Kur kilitleme tipi: 1=Plan anı, 2=Yapıldı anı, 3=Manuel.</summary>
    int RateLockType,
    /// <summary>Yapıldı anında kilitlenen kur (RateLockType=2 ise dolu).</summary>
    decimal? RateLockedValue,
    /// <summary>Provizyon kurumunun bu kalem için ödeyeceği tutar (TZH onayı). Null = girilmedi.</summary>
    decimal? InstitutionContributionAmount,
    /// <summary>Hastanın ödemesi gereken tutar = PriceBaseAmount - InstitutionContributionAmount.</summary>
    decimal PatientAmount
);

public record TreatmentPlanResponse(
    Guid PublicId,
    long PatientId,
    long BranchId,
    /// <summary>Planın bağlı olduğu şube adı.</summary>
    string? BranchName,
    long DoctorId,
    /// <summary>Planı oluşturan / sorumlu hekim adı.</summary>
    string? DoctorName,
    string Name,
    TreatmentPlanStatus Status,
    string StatusLabel,
    string? Notes,
    DateTime CreatedAt,
    /// <summary>Bağlı anlaşmalı kurum. Null = bireysel hasta.</summary>
    long? InstitutionId,
    string? InstitutionName,
    /// <summary>1=İndirim, 2=Provizyon. Null = kurum yok.</summary>
    int? InstitutionPaymentModel,
    IReadOnlyList<TreatmentPlanItemResponse> Items
);

// ─── Mapping ──────────────────────────────────────────────────────────────

public static class TreatmentPlanMappings
{
    public static TreatmentPlanItemResponse ToResponse(ItemEntity i) => new(
        i.PublicId,
        i.PlanId,
        i.TreatmentId,
        i.Treatment?.PublicId,
        i.Treatment?.Code,
        i.Treatment?.Name,
        i.ToothNumber,
        i.ToothSurfaces,
        i.BodyRegionCode,
        i.Status,
        ItemStatusLabel(i.Status),
        i.ListPrice,
        i.UnitPrice,
        i.DiscountRate,
        i.FinalPrice,
        i.KdvRate,
        i.KdvAmount,
        i.TotalAmount,
        i.PriceCurrency,
        i.PriceExchangeRate,
        i.PriceBaseAmount,
        i.DoctorId,
        i.Doctor?.FullName ?? i.Plan?.Doctor?.FullName,
        i.ApprovedBy?.FullName,
        i.ApprovedAt,
        i.Notes,
        i.CompletedAt,
        i.CreatedAt,
        i.RateLockType,
        i.RateLockedValue,
        i.InstitutionContributionAmount,
        i.PatientAmount
    );

    public static TreatmentPlanResponse ToResponse(PlanEntity p, IReadOnlyList<TreatmentPlanItemResponse>? items = null) => new(
        p.PublicId,
        p.PatientId,
        p.BranchId,
        p.Branch?.Name,
        p.DoctorId,
        p.Doctor?.FullName,
        p.Name,
        p.Status,
        PlanStatusLabel(p.Status),
        p.Notes,
        p.CreatedAt,
        p.InstitutionId,
        p.Institution?.Name,
        p.Institution != null ? (int)p.Institution.PaymentModel : null,
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
