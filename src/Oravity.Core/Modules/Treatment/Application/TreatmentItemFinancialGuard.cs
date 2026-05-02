using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Treatment.Application;

/// <summary>
/// Tedavi kalemi üzerindeki finansal kilitleri kontrol eder.
/// Aşağı akış işlemleri (fatura, hakediş, tahsis) varsa mutasyon reddedilir;
/// önce o işlemlerin iptali gerekir.
/// </summary>
internal static class TreatmentItemFinancialGuard
{
    private static readonly InstitutionInvoiceStatus[] ActiveInstitutionStatuses =
    [
        InstitutionInvoiceStatus.Issued,
        InstitutionInvoiceStatus.PartiallyPaid,
        InstitutionInvoiceStatus.Overdue,
        InstitutionInvoiceStatus.InFollowUp
    ];

    private static readonly PatientInvoiceStatus[] ActivePatientStatuses =
    [
        PatientInvoiceStatus.Issued,
        PatientInvoiceStatus.PartiallyPaid
    ];

    /// <summary>
    /// Kurum katkı tutarı değiştirilemez:
    ///   – aktif kurum faturasına dahilse
    ///   – aktif hasta faturasına dahilse (PatientAmount değişir)
    ///   – ödeme tahsisi varsa (tahsis tutarı geçersizleşir)
    /// </summary>
    public static async Task AssertContributionCanBeChangedAsync(
        long itemId, AppDbContext db, CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(itemId, db, ct);
        await AssertNoActiveInstitutionInvoiceAsync(itemId, patientId, db, ct);
        await AssertNoActivePatientInvoiceAsync(itemId, patientId, db, ct);
        await AssertNoPaymentAllocationAsync(itemId, db, ct);
    }

    /// <summary>
    /// Fiyat / iskonto değiştirilemez: herhangi bir aktif fatura,
    /// dağıtılmış hakediş veya ödeme tahsisi varsa.
    /// </summary>
    public static async Task AssertPriceCanBeChangedAsync(
        long itemId, AppDbContext db, CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(itemId, db, ct);
        await AssertNoActiveInstitutionInvoiceAsync(itemId, patientId, db, ct);
        await AssertNoActivePatientInvoiceAsync(itemId, patientId, db, ct);
        await AssertNoDistributedCommissionAsync(itemId, db, ct);
        await AssertNoPaymentAllocationAsync(itemId, db, ct);
    }

    /// <summary>
    /// Kalem silinemez: herhangi bir aktif fatura, dağıtılmış hakediş
    /// veya ödeme tahsisi varsa.
    /// </summary>
    public static async Task AssertCanBeDeletedAsync(
        long itemId, AppDbContext db, CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(itemId, db, ct);
        await AssertNoActiveInstitutionInvoiceAsync(itemId, patientId, db, ct);
        await AssertNoActivePatientInvoiceAsync(itemId, patientId, db, ct);
        await AssertNoDistributedCommissionAsync(itemId, db, ct);
        await AssertNoPaymentAllocationAsync(itemId, db, ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static async Task<long> GetPatientIdAsync(long itemId, AppDbContext db, CancellationToken ct) =>
        await db.TreatmentPlanItems
            .Where(i => i.Id == itemId)
            .Select(i => i.Plan.PatientId)
            .FirstAsync(ct);

    private static async Task AssertNoActiveInstitutionInvoiceAsync(
        long itemId, long patientId, AppDbContext db, CancellationToken ct)
    {
        var invoices = await db.InstitutionInvoices
            .Where(i => i.PatientId == patientId
                        && ActiveInstitutionStatuses.Contains(i.Status)
                        && i.TreatmentItemIdsJson != null)
            .Select(i => new { i.InvoiceNo, i.TreatmentItemIdsJson })
            .ToListAsync(ct);

        foreach (var inv in invoices)
        {
            if (!ContainsId(inv.TreatmentItemIdsJson, itemId)) continue;
            throw new ConflictException(
                $"Bu tedavi kalemi aktif bir kurum faturasına dahil ({inv.InvoiceNo}). " +
                "Değişiklik için önce faturayı iptal edin.");
        }
    }

    private static async Task AssertNoActivePatientInvoiceAsync(
        long itemId, long patientId, AppDbContext db, CancellationToken ct)
    {
        var invoices = await db.PatientInvoices
            .Where(i => i.PatientId == patientId
                        && ActivePatientStatuses.Contains(i.Status)
                        && i.TreatmentItemIdsJson != null)
            .Select(i => new { i.InvoiceNo, i.TreatmentItemIdsJson })
            .ToListAsync(ct);

        foreach (var inv in invoices)
        {
            if (!ContainsId(inv.TreatmentItemIdsJson, itemId)) continue;
            throw new ConflictException(
                $"Bu tedavi kalemi aktif bir hasta faturasına dahil ({inv.InvoiceNo}). " +
                "Değişiklik için önce faturayı iptal edin.");
        }
    }

    private static async Task AssertNoDistributedCommissionAsync(
        long itemId, AppDbContext db, CancellationToken ct)
    {
        var distributed = await db.DoctorCommissions
            .AnyAsync(c => c.TreatmentPlanItemId == itemId
                           && c.Status == CommissionStatus.Distributed, ct);
        if (distributed)
            throw new ConflictException(
                "Bu tedavi kalemine ait hakediş dağıtılmış. " +
                "Değişiklik için önce hakedişi iptal edin.");
    }

    private static async Task AssertNoPaymentAllocationAsync(
        long itemId, AppDbContext db, CancellationToken ct)
    {
        var hasAllocation = await db.PaymentAllocations
            .AnyAsync(a => a.TreatmentPlanItemId == itemId, ct);
        if (hasAllocation)
            throw new ConflictException(
                "Bu tedaviye ödeme tahsisi yapılmış. " +
                "Değişiklik için önce ödeme tahsisini kaldırın.");
    }

    private static bool ContainsId(string? json, long id)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            var ids = JsonSerializer.Deserialize<long[]>(json);
            return ids is not null && Array.IndexOf(ids, id) >= 0;
        }
        catch { return false; }
    }
}
