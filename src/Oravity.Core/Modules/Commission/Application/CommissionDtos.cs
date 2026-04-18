using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Commission.Application;

// ─── Template ────────────────────────────────────────────────────────────

public record CommissionTemplateResponse(
    Guid PublicId,
    long Id,
    string Name,
    CommissionWorkingStyle WorkingStyle,
    string WorkingStyleLabel,
    CommissionPaymentType PaymentType,
    string PaymentTypeLabel,
    JobStartCalculation? JobStartCalculation,
    decimal FixedFee,
    decimal PrimRate,
    bool ClinicTargetEnabled,
    decimal? ClinicTargetBonusRate,
    bool DoctorTargetEnabled,
    decimal? DoctorTargetBonusRate,
    bool InstitutionPayOnInvoice,
    bool DeductTreatmentPlanCommission,
    bool DeductLabCost,
    bool DeductTreatmentCost,
    bool DeductCreditCardCommission,
    bool KdvEnabled,
    decimal? KdvRate,
    string? KdvAppliedPaymentTypes,
    bool ExtraExpenseEnabled,
    decimal? ExtraExpenseRate,
    bool WithholdingTaxEnabled,
    decimal? WithholdingTaxRate,
    bool IsActive,
    IReadOnlyList<JobStartPriceResponse> JobStartPrices,
    DateTime CreatedAt
);

public record JobStartPriceResponse(
    long Id,
    long TreatmentId,
    JobStartPriceType PriceType,
    decimal Value
);

public record JobStartPriceRequest(
    long TreatmentId,
    JobStartPriceType PriceType,
    decimal Value
);

// ─── Assignment ──────────────────────────────────────────────────────────

public record TemplateAssignmentResponse(
    Guid PublicId,
    long DoctorId,
    string DoctorName,
    long TemplateId,
    string TemplateName,
    DateOnly EffectiveDate,
    DateOnly? ExpiryDate,
    bool IsActive,
    DateTime CreatedAt
);

// ─── Targets ──────────────────────────────────────────────────────────────

public record DoctorTargetResponse(
    Guid PublicId,
    long DoctorId,
    string? DoctorName,
    long BranchId,
    int Year,
    int Month,
    decimal TargetAmount,
    DateTime CreatedAt
);

public record BranchTargetResponse(
    Guid PublicId,
    long BranchId,
    int Year,
    int Month,
    decimal TargetAmount,
    DateTime CreatedAt
);

// ─── Mappings ─────────────────────────────────────────────────────────────

public static class CommissionMappings
{
    public static CommissionTemplateResponse ToResponse(DoctorCommissionTemplate t)
        => new(
            t.PublicId, t.Id, t.Name,
            t.WorkingStyle, WorkingStyleLabel(t.WorkingStyle),
            t.PaymentType, PaymentTypeLabel(t.PaymentType),
            t.JobStartCalculation,
            t.FixedFee, t.PrimRate,
            t.ClinicTargetEnabled, t.ClinicTargetBonusRate,
            t.DoctorTargetEnabled, t.DoctorTargetBonusRate,
            t.InstitutionPayOnInvoice,
            t.DeductTreatmentPlanCommission, t.DeductLabCost, t.DeductTreatmentCost,
            t.DeductCreditCardCommission,
            t.KdvEnabled, t.KdvRate, t.KdvAppliedPaymentTypes,
            t.ExtraExpenseEnabled, t.ExtraExpenseRate,
            t.WithholdingTaxEnabled, t.WithholdingTaxRate,
            t.IsActive,
            t.JobStartPrices.Select(p => new JobStartPriceResponse(p.Id, p.TreatmentId, p.PriceType, p.Value)).ToList(),
            t.CreatedAt);

    public static TemplateAssignmentResponse ToResponse(DoctorTemplateAssignment a, string doctorName, string templateName)
        => new(a.PublicId, a.DoctorId, doctorName, a.TemplateId, templateName,
               a.EffectiveDate, a.ExpiryDate, a.IsActive, a.CreatedAt);

    public static DoctorTargetResponse ToResponse(DoctorTarget t, string? doctorName = null)
        => new(t.PublicId, t.DoctorId, doctorName, t.BranchId, t.Year, t.Month, t.TargetAmount, t.CreatedAt);

    public static BranchTargetResponse ToResponse(BranchTarget t)
        => new(t.PublicId, t.BranchId, t.Year, t.Month, t.TargetAmount, t.CreatedAt);

    public static string WorkingStyleLabel(CommissionWorkingStyle s) => s switch
    {
        CommissionWorkingStyle.Accrual    => "Tahakkuk",
        CommissionWorkingStyle.Collection => "Tahsilat",
        _ => s.ToString()
    };

    public static string PaymentTypeLabel(CommissionPaymentType t) => t switch
    {
        CommissionPaymentType.Fix                       => "Sabit",
        CommissionPaymentType.Prim                      => "Prim",
        CommissionPaymentType.FixPlusPrim               => "Sabit + Prim",
        CommissionPaymentType.PerJob                    => "İş Başı",
        CommissionPaymentType.PerJobSelectedPlusFixPrim => "Seçili İş Başı + Fix/Prim",
        CommissionPaymentType.PriceRange                => "Fiyat Bandı",
        _ => t.ToString()
    };
}
