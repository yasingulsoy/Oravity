using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Commission.Infrastructure;

/// <summary>
/// Hekim hakediş hesaplama servisi.
/// Kesinti zinciri:
///   Gross (TEX) → POS/Kredi Kartı − Lab − Tedavi Maliyeti − Tedavi Planı Komisyonu
///   − Ekstra Gider % → NetBase
///   NetBase × (PrimRate veya BonusRate) + FixedFee = GrossCommission
///   +KDV − Stopaj = NetCommission
/// </summary>
public interface ICommissionCalculator
{
    Task<CommissionCalculation> CalculateAsync(
        long treatmentPlanItemId,
        CancellationToken ct = default);
}

public class CommissionCalculator : ICommissionCalculator
{
    private readonly AppDbContext _db;

    public CommissionCalculator(AppDbContext db) => _db = db;

    public async Task<CommissionCalculation> CalculateAsync(
        long treatmentPlanItemId, CancellationToken ct = default)
    {
        var item = await _db.TreatmentPlanItems.AsNoTracking()
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(i => i.Id == treatmentPlanItemId, ct)
            ?? throw new InvalidOperationException($"Tedavi kalemi bulunamadı: {treatmentPlanItemId}");

        var doctorId = item.DoctorId ?? item.Plan.DoctorId;
        var branchId = item.Plan.BranchId;
        var period   = DateTime.UtcNow;

        // ── 1. Şablon bul ─────────────────────────────────────────────────
        var assignment = await _db.DoctorTemplateAssignments.AsNoTracking()
            .Where(a => a.DoctorId == doctorId && a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);

        DoctorCommissionTemplate? template = null;
        if (assignment != null)
            template = await _db.DoctorCommissionTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == assignment.TemplateId, ct);

        // Şablon yoksa default: Prim %30, kesinti yok
        var primRate         = template?.PrimRate ?? 30m;
        var fixedFee         = template?.FixedFee ?? 0m;
        var deductLab        = template?.DeductLabCost ?? false;
        var deductCost       = template?.DeductTreatmentCost ?? false;
        var deductPlanCom    = template?.DeductTreatmentPlanCommission ?? false;
        var deductCc         = template?.DeductCreditCardCommission ?? true;
        var extraEnabled     = template?.ExtraExpenseEnabled ?? false;
        var extraRate        = template?.ExtraExpenseRate ?? 0m;
        var kdvEnabled       = template?.KdvEnabled ?? false;
        var kdvRate          = template?.KdvRate ?? 0m;
        var stopajEnabled    = template?.WithholdingTaxEnabled ?? false;
        var stopajRate       = template?.WithholdingTaxRate ?? 0m;

        // ── 2. Gross ──────────────────────────────────────────────────────
        var gross        = item.FinalPrice;
        var currency     = item.PriceCurrency;
        var exchangeRate = item.PriceExchangeRate;

        // ── 3. Kesintiler ─────────────────────────────────────────────────
        decimal labCost = 0, treatmentCost = 0, planCommission = 0;

        if (deductLab)
        {
            labCost = await _db.LaboratoryWorks.AsNoTracking()
                .Where(w => w.TreatmentPlanItemId == treatmentPlanItemId)
                .SumAsync(w => (decimal?)w.TotalCost, ct) ?? 0m;
        }

        if (deductCost)
        {
            // Treatment.CostPrice * tamamlanan miktar (varsayılan 1 birim)
            var costPrice = await _db.Treatments.AsNoTracking()
                .Where(t => t.Id == item.TreatmentId)
                .Select(t => (decimal?)t.CostPrice)
                .FirstOrDefaultAsync(ct) ?? 0m;

            treatmentCost = costPrice;
        }

        if (deductPlanCom)
        {
            // Tedavi planı komisyonu — danışman/satış primi varsa. Şimdilik 0.
            planCommission = 0m;
        }

        // POS komisyonu: KK ödemesi varsa PatientAllocation'dan türetilir.
        // SPEC'te %1.75 varsayılan; company setting yoksa 0 bırakıyoruz.
        decimal posRate = 0m, posAmount = 0m;
        if (deductCc)
        {
            var kkPaid = await _db.PaymentAllocations.AsNoTracking()
                .Where(a => a.TreatmentPlanItemId == treatmentPlanItemId
                    && a.PaymentId.HasValue
                    && !a.IsRefunded)
                .Join(_db.Payments.AsNoTracking(), a => a.PaymentId, p => p.Id, (a, p) => new { a, p })
                .Where(x => x.p.Method == PaymentMethod.CreditCard)
                .SumAsync(x => (decimal?)x.a.AllocatedAmount, ct) ?? 0m;

            posRate = 1.75m;
            posAmount = Math.Round(kkPaid * posRate / 100m, 2);
        }

        var afterDeductions = gross - labCost - treatmentCost - planCommission - posAmount;

        decimal extraAmount = 0m;
        if (extraEnabled && extraRate > 0)
            extraAmount = Math.Round(afterDeductions * extraRate / 100m, 2);

        var netBase = Math.Max(0, afterDeductions - extraAmount);

        // ── 4. Hedef Bonusu (SPEC 9584-9587: en yüksek bonus uygulanır) ────
        var appliedRate = primRate;
        var bonusApplied = false;

        if (template != null)
        {
            // Hekim hedefi kontrolü
            if (template.DoctorTargetEnabled && template.DoctorTargetBonusRate.HasValue)
            {
                var target = await _db.DoctorTargets.AsNoTracking()
                    .FirstOrDefaultAsync(t =>
                        t.DoctorId == doctorId && t.BranchId == branchId
                        && t.Year == period.Year && t.Month == period.Month, ct);

                if (target != null)
                {
                    var monthRevenue = await _db.TreatmentPlanItems.AsNoTracking()
                        .Where(x => (x.DoctorId ?? x.Plan.DoctorId) == doctorId
                            && x.Plan.BranchId == branchId
                            && x.Status == TreatmentItemStatus.Completed
                            && x.CompletedAt != null
                            && x.CompletedAt.Value.Year == period.Year
                            && x.CompletedAt.Value.Month == period.Month)
                        .SumAsync(x => (decimal?)x.FinalPrice, ct) ?? 0m;

                    if (monthRevenue >= target.TargetAmount)
                    {
                        appliedRate = Math.Max(appliedRate, template.DoctorTargetBonusRate.Value);
                        bonusApplied = true;
                    }
                }
            }

            // Klinik hedefi kontrolü — hekim bonusundan bağımsız olarak değerlendirilir,
            // ikisi de sağlanırsa en yüksek oran kullanılır (SPEC 9557).
            if (template.ClinicTargetEnabled && template.ClinicTargetBonusRate.HasValue)
            {
                var branchTarget = await _db.BranchTargets.AsNoTracking()
                    .FirstOrDefaultAsync(t =>
                        t.BranchId == branchId && t.Year == period.Year && t.Month == period.Month, ct);

                if (branchTarget != null)
                {
                    var monthRevenue = await _db.TreatmentPlanItems.AsNoTracking()
                        .Where(x => x.Plan.BranchId == branchId
                            && x.Status == TreatmentItemStatus.Completed
                            && x.CompletedAt != null
                            && x.CompletedAt.Value.Year == period.Year
                            && x.CompletedAt.Value.Month == period.Month)
                        .SumAsync(x => (decimal?)x.FinalPrice, ct) ?? 0m;

                    if (monthRevenue >= branchTarget.TargetAmount)
                    {
                        appliedRate = Math.Max(appliedRate, template.ClinicTargetBonusRate.Value);
                        bonusApplied = true;
                    }
                }
            }
        }

        // ── 5. Brüt Hakediş (SPEC 9590) ──────────────────────────────────
        decimal primAmount;
        switch (template?.PaymentType)
        {
            case CommissionPaymentType.Fix:
                primAmount = fixedFee;
                break;

            case CommissionPaymentType.FixPlusPrim:
                primAmount = fixedFee + Math.Round(netBase * appliedRate / 100m, 2);
                break;

            case CommissionPaymentType.PerJob:
            {
                // Tedavi bazında şablonda tanımlı sabit ücret veya yüzdeyi kullan;
                // eşleşme yoksa normal prim hesabına düş.
                var jobAmount = await GetJobStartAmountAsync(template, item.TreatmentId, netBase, ct);
                primAmount = jobAmount ?? Math.Round(netBase * appliedRate / 100m, 2);
                break;
            }

            case CommissionPaymentType.PerJobSelectedPlusFixPrim:
            {
                // Tedaviye özel iş başı tanımı varsa onu kullan;
                // yoksa Fix+Prim formülüne dön.
                var jobAmount = await GetJobStartAmountAsync(template, item.TreatmentId, netBase, ct);
                primAmount = jobAmount.HasValue
                    ? jobAmount.Value
                    : fixedFee + Math.Round(netBase * appliedRate / 100m, 2);
                break;
            }

            case CommissionPaymentType.PriceRange:
            {
                // Brüt bedele göre eşleşen bant oranını uygula;
                // hiç bant tanımlı değilse şablondaki primRate'e dön.
                var bandRate = await GetPriceRangeRateAsync(template, gross, ct);
                primAmount = Math.Round(netBase * (bandRate ?? appliedRate) / 100m, 2);
                break;
            }

            default: // Prim
                primAmount = Math.Round(netBase * appliedRate / 100m, 2);
                break;
        }

        // ── 6. KDV (SPEC 9147-9149, 9593-9594) ───────────────────────────
        //    KDV yalnızca KdvAppliedPaymentTypes listesinde yer alan ödeme
        //    tipiyle tahsil edilen tedavilere uygulanır. Hiç ödeme yoksa KDV 0.
        bool kdvShouldApply = false;
        if (kdvEnabled && kdvRate > 0)
        {
            var appliedTypes = ParsePaymentTypes(template?.KdvAppliedPaymentTypes);
            if (appliedTypes.Count == 0)
            {
                // Seçili tip tanımlı değilse tüm ödeme tipleri için uygulanır.
                kdvShouldApply = true;
            }
            else
            {
                // TPI'a dağıtılmış ödemelerden en az birinin yöntemi listede olmalı.
                var usedMethods = await _db.PaymentAllocations.AsNoTracking()
                    .Where(a => a.TreatmentPlanItemId == treatmentPlanItemId
                        && a.PaymentId.HasValue && !a.IsRefunded)
                    .Join(_db.Payments.AsNoTracking(), a => a.PaymentId, p => p.Id, (a, p) => (int)p.Method)
                    .Distinct()
                    .ToListAsync(ct);

                kdvShouldApply = usedMethods.Any(m => appliedTypes.Contains(m));
            }
        }

        decimal kdvAmount = kdvShouldApply
            ? Math.Round(primAmount * kdvRate / 100m, 2)
            : 0m;

        // ── 7. Stopaj — KDV dahil brüt hakediş üzerinden (SPEC 9597) ─────
        decimal grossWithKdv = primAmount + kdvAmount;
        decimal stopajAmount = stopajEnabled && stopajRate > 0
            ? Math.Round(grossWithKdv * stopajRate / 100m, 2)
            : 0m;

        var netCommission = grossWithKdv - stopajAmount;

        return new CommissionCalculation
        {
            DoctorId             = doctorId,
            TreatmentPlanItemId  = treatmentPlanItemId,
            BranchId             = branchId,
            TemplateId           = template?.Id,
            PeriodYear           = period.Year,
            PeriodMonth          = period.Month,

            GrossAmount          = gross,
            Currency             = currency,
            ExchangeRate         = exchangeRate,

            PosCommissionRate                = posRate,
            PosCommissionAmount              = posAmount,
            LabCostDeducted                  = labCost,
            TreatmentCostDeducted            = treatmentCost,
            TreatmentPlanCommissionDeducted  = planCommission,
            ExtraExpenseRate                 = extraRate,
            ExtraExpenseAmount               = extraAmount,
            NetBaseAmount                    = netBase,

            AppliedPrimRate  = appliedRate,
            BonusApplied     = bonusApplied,
            FixedFee         = template?.PaymentType is CommissionPaymentType.Fix
                                   or CommissionPaymentType.FixPlusPrim
                                   or CommissionPaymentType.PerJobSelectedPlusFixPrim
                               ? fixedFee : 0m,
            GrossCommission  = primAmount,

            KdvRate              = kdvRate,
            KdvAmount            = kdvAmount,
            WithholdingTaxRate   = stopajRate,
            WithholdingTaxAmount = stopajAmount,

            NetCommissionAmount  = netCommission
        };
    }

    /// <summary>
    /// PerJob / PerJobSelectedPlusFixPrim için tedaviye özel iş başı tutarını hesaplar.
    /// Şablonda bu tedavi için kayıt yoksa null döner (çağıran fallback uygular).
    /// </summary>
    private async Task<decimal?> GetJobStartAmountAsync(
        DoctorCommissionTemplate template, long treatmentId, decimal netBase, CancellationToken ct)
    {
        var entry = await _db.TemplateJobStartPrices.AsNoTracking()
            .FirstOrDefaultAsync(j => j.TemplateId == template.Id && j.TreatmentId == treatmentId, ct);

        if (entry == null)
            return null;

        return entry.PriceType == JobStartPriceType.FixedAmount
            ? entry.Value
            : Math.Round(netBase * entry.Value / 100m, 2);
    }

    /// <summary>
    /// PriceRange tipinde brüt bedele göre eşleşen bandın oranını döner.
    /// Hiç bant tanımlı değilse veya eşleşme yoksa null döner (çağıran primRate'e düşer).
    /// </summary>
    private async Task<decimal?> GetPriceRangeRateAsync(
        DoctorCommissionTemplate template, decimal gross, CancellationToken ct)
    {
        var ranges = await _db.TemplatePriceRanges.AsNoTracking()
            .Where(r => r.TemplateId == template.Id)
            .OrderBy(r => r.MinAmount)
            .ToListAsync(ct);

        var matched = ranges.FirstOrDefault(r =>
            gross >= r.MinAmount && (r.MaxAmount == null || gross < r.MaxAmount));

        return matched?.Rate;
    }

    /// <summary>
    /// KdvAppliedPaymentTypes JSON'unu int listesi olarak parse eder.
    /// Boş/null/geçersiz veri için boş liste döner.
    /// </summary>
    private static HashSet<int> ParsePaymentTypes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new HashSet<int>();

        try
        {
            var arr = JsonSerializer.Deserialize<int[]>(json);
            return arr == null ? new HashSet<int>() : new HashSet<int>(arr);
        }
        catch
        {
            return new HashSet<int>();
        }
    }
}
