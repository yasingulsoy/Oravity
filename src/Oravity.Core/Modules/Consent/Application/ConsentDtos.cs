using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Consent.Application;

// ─── Response DTO'lar ──────────────────────────────────────────────────────

public record ConsentFormTemplateResponse(
    Guid    PublicId,
    string  Code,
    string  Name,
    string  Language,
    string  Version,
    string  ContentHtml,
    string  CheckboxesJson,
    bool    AppliesToAllTreatments,
    string? TreatmentCategoryIdsJson,
    bool    ShowDentalChart,
    bool    ShowTreatmentTable,
    bool    RequireDoctorSignature,
    bool    IsActive,
    DateTime CreatedAt
);

public record ConsentFormTemplateSummary(
    Guid   PublicId,
    string Code,
    string Name,
    string Language,
    string Version,
    bool   IsActive
);

public record ConsentInstanceResponse(
    Guid   PublicId,
    string ConsentCode,
    long   PatientId,
    long?  TreatmentPlanId,
    Guid?  TreatmentPlanPublicId,
    Guid   FormTemplatePublicId,
    string FormTemplateName,
    string ItemPublicIdsJson,
    string DeliveryMethod,
    string Status,
    string? QrToken,
    DateTime? QrTokenExpiresAt,
    string? SmsToken,
    DateTime? SmsTokenExpiresAt,
    DateTime? SignedAt,
    string? SignerName,
    DateTime CreatedAt
);

// ─── Mapping ──────────────────────────────────────────────────────────────

public static class ConsentMappings
{
    public static ConsentFormTemplateResponse ToResponse(ConsentFormTemplate t) => new(
        t.PublicId,
        t.Code,
        t.Name,
        t.Language,
        t.Version,
        t.ContentHtml,
        t.CheckboxesJson,
        t.AppliesToAllTreatments,
        t.TreatmentCategoryIdsJson,
        t.ShowDentalChart,
        t.ShowTreatmentTable,
        t.RequireDoctorSignature,
        t.IsActive,
        t.CreatedAt
    );

    public static ConsentFormTemplateSummary ToSummary(ConsentFormTemplate t) => new(
        t.PublicId,
        t.Code,
        t.Name,
        t.Language,
        t.Version,
        t.IsActive
    );

    public static string StatusLabel(ConsentInstanceStatus s) => s switch
    {
        ConsentInstanceStatus.Pending   => "İmza Bekliyor",
        ConsentInstanceStatus.Signed    => "İmzalandı",
        ConsentInstanceStatus.Expired   => "Süresi Doldu",
        ConsentInstanceStatus.Cancelled => "İptal",
        _ => s.ToString()
    };

    public static ConsentInstanceResponse ToResponse(ConsentInstance ci, ConsentFormTemplate tpl, Guid? planPublicId = null) => new(
        ci.PublicId,
        ci.ConsentCode,
        ci.PatientId,
        ci.TreatmentPlanId,
        planPublicId,
        tpl.PublicId,
        tpl.Name,
        ci.ItemPublicIdsJson,
        ci.DeliveryMethod,
        StatusLabel(ci.Status),
        ci.QrToken,
        ci.QrTokenExpiresAt,
        ci.SmsToken,
        ci.SmsTokenExpiresAt,
        ci.SignedAt,
        ci.SignerName,
        ci.CreatedAt
    );
}
