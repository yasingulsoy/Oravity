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

        // ── 4. Hedef Bonusu ───────────────────────────────────────────────
        var appliedRate = primRate;
        var bonusApplied = false;

        if (template != null && (template.DoctorTargetEnabled || template.ClinicTargetEnabled))
        {
            // Hekim hedefi
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
                        appliedRate = template.DoctorTargetBonusRate.Value;
                        bonusApplied = true;
                    }
                }
            }

            // Klinik hedefi (hekim hedefi uygulanmadıysa)
            if (!bonusApplied && template.ClinicTargetEnabled && template.ClinicTargetBonusRate.HasValue)
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
                        appliedRate = template.ClinicTargetBonusRate.Value;
                        bonusApplied = true;
                    }
                }
            }
        }

        // ── 5. Prim ───────────────────────────────────────────────────────
        decimal primAmount = 0m;
        if (template?.PaymentType is CommissionPaymentType.Fix)
        {
            primAmount = fixedFee;
        }
        else if (template?.PaymentType is CommissionPaymentType.FixPlusPrim)
        {
            primAmount = fixedFee + Math.Round(netBase * appliedRate / 100m, 2);
        }
        else
        {
            // Prim veya PerJob/PriceRange (PerJob/PriceRange için basit fallback)
            primAmount = Math.Round(netBase * appliedRate / 100m, 2);
        }

        // ── 6. Vergi ──────────────────────────────────────────────────────
        decimal kdvAmount = kdvEnabled && kdvRate > 0
            ? Math.Round(primAmount * kdvRate / 100m, 2) : 0m;
        decimal stopajAmount = stopajEnabled && stopajRate > 0
            ? Math.Round(primAmount * stopajRate / 100m, 2) : 0m;

        var netCommission = primAmount + kdvAmount - stopajAmount;

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
            FixedFee         = template?.PaymentType is CommissionPaymentType.Fix or CommissionPaymentType.FixPlusPrim
                                ? fixedFee : 0m,
            GrossCommission  = primAmount,

            KdvRate              = kdvRate,
            KdvAmount            = kdvAmount,
            WithholdingTaxRate   = stopajRate,
            WithholdingTaxAmount = stopajAmount,

            NetCommissionAmount  = netCommission
        };
    }
}
