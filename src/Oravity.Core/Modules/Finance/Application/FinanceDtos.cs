using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Finance.Application;

// ─── Payment ──────────────────────────────────────────────────────────────

public record PaymentResponse(
    Guid PublicId,
    long Id,
    long PatientId,
    long BranchId,
    decimal Amount,
    string Currency,
    decimal ExchangeRate,
    decimal BaseAmount,
    PaymentMethod Method,
    string MethodLabel,
    DateOnly PaymentDate,
    string? Notes,
    bool IsRefunded,
    DateTime CreatedAt
);

public record PaymentAllocationResponse(
    long Id,
    long? PaymentId,
    long? InstitutionPaymentId,
    long TreatmentPlanItemId,
    AllocationSource Source,
    AllocationMethod Method,
    decimal AllocatedAmount,
    bool IsRefunded,
    DateTime CreatedAt
);

// ─── Balance ──────────────────────────────────────────────────────────────

public record PatientBalanceResponse(
    long PatientId,
    decimal TotalTreatmentAmount,
    decimal TotalPaid,
    decimal TotalAllocated,
    decimal Balance,
    string BalanceLabel
);

// ─── Commission ───────────────────────────────────────────────────────────

public record DoctorCommissionResponse(
    long Id,
    long DoctorId,
    long TreatmentPlanItemId,
    long BranchId,
    decimal GrossAmount,
    decimal CommissionRate,
    decimal CommissionAmount,
    CommissionStatus Status,
    string StatusLabel,
    DateTime? DistributedAt,
    DateTime CreatedAt
);

// ─── Daily Report ─────────────────────────────────────────────────────────

public record DailyReportResponse(
    DateOnly Date,
    long BranchId,
    IReadOnlyList<PaymentMethodTotal> ByMethod,
    decimal TotalAmount,
    int TotalCount
);

public record PaymentMethodTotal(
    PaymentMethod Method,
    string MethodLabel,
    decimal Amount,
    int Count
);

// ─── Mappings ─────────────────────────────────────────────────────────────

public static class FinanceMappings
{
    public static PaymentResponse ToResponse(Payment p) => new(
        p.PublicId, p.Id, p.PatientId, p.BranchId,
        p.Amount, p.Currency, p.ExchangeRate, p.BaseAmount,
        p.Method, MethodLabel(p.Method),
        p.PaymentDate, p.Notes, p.IsRefunded, p.CreatedAt);

    public static PaymentAllocationResponse ToResponse(PaymentAllocation a) => new(
        a.Id, a.PaymentId, a.InstitutionPaymentId, a.TreatmentPlanItemId,
        a.Source, a.Method,
        a.AllocatedAmount, a.IsRefunded, a.CreatedAt);

    public static DoctorCommissionResponse ToResponse(DoctorCommission c) => new(
        c.Id, c.DoctorId, c.TreatmentPlanItemId, c.BranchId,
        c.GrossAmount, c.CommissionRate, c.CommissionAmount,
        c.Status, CommissionStatusLabel(c.Status),
        c.DistributedAt, c.CreatedAt);

    public static string MethodLabel(PaymentMethod m) => m switch
    {
        PaymentMethod.Cash         => "Nakit",
        PaymentMethod.CreditCard   => "Kredi Kartı",
        PaymentMethod.BankTransfer => "Havale/EFT",
        PaymentMethod.Installment  => "Taksit",
        PaymentMethod.Check        => "Çek",
        _ => m.ToString()
    };

    public static string CommissionStatusLabel(CommissionStatus s) => s switch
    {
        CommissionStatus.Pending     => "Bekliyor",
        CommissionStatus.Distributed => "Dağıtıldı",
        CommissionStatus.Cancelled   => "İptal",
        _ => s.ToString()
    };
}
