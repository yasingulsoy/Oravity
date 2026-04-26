using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Finance.Application;

// ─── Kasa raporu durum özeti ──────────────────────────────────────────────

public record DailyCashReportResponse(
    Guid             PublicId,
    long             BranchId,
    DateOnly         ReportDate,
    CashReportStatus Status,
    string           StatusLabel,
    long?            ClosedByUserId,
    DateTime?        ClosedAt,
    string?          ClosingNotes,
    long?            ApprovedByUserId,
    DateTime?        ApprovedAt,
    string?          ApprovalNotes,
    int              ReopenCount
);

// ─── Kasa raporu detay ────────────────────────────────────────────────────

public record CashPaymentLine(
    Guid         PublicId,
    long         Id,
    DateTime     CreatedAt,
    string       PatientName,
    decimal      Amount,
    string       Currency,
    decimal      ExchangeRate,
    decimal      BaseAmount,     // TRY karşılığı
    PaymentMethod Method,
    string       MethodLabel,
    string?      Notes,
    string       RecordedByName
);

public record CashCurrencyTotal(
    string  Currency,
    decimal Amount,     // Orijinal para birimi toplamı
    decimal BaseTry,    // TRY toplamı
    int     Count
);

public record CashMethodTotal(
    PaymentMethod           Method,
    string                  MethodLabel,
    decimal                 TotalTry,
    int                     Count,
    IReadOnlyList<CashCurrencyTotal> ByCurrency
);

public record DailyCashReportDetailResponse(
    DateOnly                      Date,
    long                          BranchId,
    DailyCashReportResponse?      ReportStatus,      // null = henüz kapatılmamış (Open)
    IReadOnlyList<CashPaymentLine> Payments,
    IReadOnlyList<CashMethodTotal> ByMethod,
    IReadOnlyList<CashCurrencyTotal> ByCurrency,
    decimal                        TotalTry,
    int                            TotalCount
);

// ─── Mappings ─────────────────────────────────────────────────────────────

public static class CashReportMappings
{
    public static DailyCashReportResponse ToResponse(DailyCashReport r) => new(
        r.PublicId,
        r.BranchId,
        r.ReportDate,
        r.Status,
        StatusLabel(r.Status),
        r.ClosedByUserId,
        r.ClosedAt,
        r.ClosingNotes,
        r.ApprovedByUserId,
        r.ApprovedAt,
        r.ApprovalNotes,
        r.ReopenCount);

    public static string StatusLabel(CashReportStatus s) => s switch
    {
        CashReportStatus.Open     => "Açık",
        CashReportStatus.Closed   => "Kapatıldı",
        CashReportStatus.Approved => "Onaylandı",
        _ => s.ToString()
    };
}
