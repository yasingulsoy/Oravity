using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum CashReportStatus
{
    Open     = 1,  // Gün açık (ödemeler devam ediyor)
    Closed   = 2,  // Gün sonu kapandı (onay bekliyor)
    Approved = 3   // Onaylandı (kasa kesinleşti)
}

/// <summary>
/// Günlük kasa raporu. Şube bazında her gün için bir kayıt.
/// Akış: Open → Closed → Approved → (Reopen) → Closed
/// </summary>
public class DailyCashReport : AuditableEntity
{
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public DateOnly ReportDate { get; private set; }

    public CashReportStatus Status { get; private set; } = CashReportStatus.Open;

    /// <summary>Kapatan kullanıcı Id'si.</summary>
    public long? ClosedByUserId { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? ClosingNotes { get; private set; }

    /// <summary>Onaylayan kullanıcı Id'si (Şube Müdürü veya Muhasebe).</summary>
    public long? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? ApprovalNotes { get; private set; }

    /// <summary>Yeniden açılma sayısı (audit amaçlı).</summary>
    public int ReopenCount { get; private set; }

    private DailyCashReport() { }

    public static DailyCashReport Create(long branchId, DateOnly reportDate)
    {
        return new DailyCashReport
        {
            BranchId   = branchId,
            ReportDate = reportDate,
            Status     = CashReportStatus.Open
        };
    }

    public void Close(long userId, string? notes = null)
    {
        if (Status == CashReportStatus.Approved)
            throw new InvalidOperationException("Onaylanmış kasa raporu kapatılamaz.");

        Status          = CashReportStatus.Closed;
        ClosedByUserId  = userId;
        ClosedAt        = DateTime.UtcNow;
        ClosingNotes    = notes;
        MarkUpdated();
    }

    public void Approve(long userId, string? notes = null)
    {
        if (Status != CashReportStatus.Closed)
            throw new InvalidOperationException("Sadece kapatılmış kasa raporu onaylanabilir.");

        Status           = CashReportStatus.Approved;
        ApprovedByUserId = userId;
        ApprovedAt       = DateTime.UtcNow;
        ApprovalNotes    = notes;
        MarkUpdated();
    }

    public void Reopen()
    {
        if (Status == CashReportStatus.Open)
            throw new InvalidOperationException("Kasa raporu zaten açık.");

        Status      = CashReportStatus.Open;
        ReopenCount++;
        MarkUpdated();
    }
}
