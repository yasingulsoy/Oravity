using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Commission.Infrastructure;
using Oravity.Core.Modules.Finance.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Commission.Application.Commands;

/// <summary>
/// Tamamlanan tedavi için hekim hakediş kaydı oluşturur.
/// SPEC 9345-9359 koşulları:
///   1) Tedavi YAPILDI
///   2) Ödeme tam dağıtılmış (hasta + kurum)
///   3) Lab işi varsa onaylanmış
///   4) Kurum ödemesi:
///        - Template "Fatura Kesilince" → fatura kesilmiş mi?
///        - Template "Kurum Ödeyince"  → kurum ödemesi gelmiş mi?
///   5) Hekim komisyon oranı tanımlı
/// </summary>
public record CalculateCommissionCommand(long TreatmentPlanItemId)
    : IRequest<DoctorCommissionResponse>;

public class CalculateCommissionCommandHandler
    : IRequestHandler<CalculateCommissionCommand, DoctorCommissionResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICommissionCalculator _calculator;

    public CalculateCommissionCommandHandler(
        AppDbContext db, ITenantContext tenant, ICommissionCalculator calculator)
    {
        _db = db;
        _tenant = tenant;
        _calculator = calculator;
    }

    public async Task<DoctorCommissionResponse> Handle(
        CalculateCommissionCommand r, CancellationToken ct)
    {
        var item = await _db.TreatmentPlanItems.AsNoTracking()
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(i => i.Id == r.TreatmentPlanItemId, ct)
            ?? throw new NotFoundException($"Tedavi kalemi bulunamadı: {r.TreatmentPlanItemId}");

        // ── SPEC 9350: Koşul 1 ─ Tedavi tamamlanmış olmalı ──────────────────
        if (item.Status != TreatmentItemStatus.Completed)
            throw new InvalidOperationException("Hakediş yalnızca tamamlanmış tedaviler için hesaplanabilir.");

        if (_tenant.IsBranchLevel && item.Plan.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu tedaviye erişim yetkiniz bulunmuyor.");

        var doctorId = item.DoctorId ?? item.Plan.DoctorId;

        // ── SPEC 9356: Koşul 5 ─ Hekim komisyon oranı tanımlı olmalı ────────
        var assignment = await _db.DoctorTemplateAssignments.AsNoTracking()
            .Where(a => a.DoctorId == doctorId && a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);

        DoctorCommissionTemplate? template = null;
        if (assignment != null)
        {
            template = await _db.DoctorCommissionTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == assignment.TemplateId, ct);
        }

        if (template == null)
            throw new InvalidOperationException(
                "Hekime atanmış aktif bir hakediş şablonu bulunmuyor. Hakediş hesaplanamaz.");

        // ── SPEC 9352: Koşul 3 ─ Lab işi varsa onaylanmış olmalı ────────────
        var openLabWorks = await _db.LaboratoryWorks.AsNoTracking()
            .Where(w => w.TreatmentPlanItemId == r.TreatmentPlanItemId
                && w.Status != LaboratoryWorkStatus.Approved
                && w.Status != LaboratoryWorkStatus.Cancelled
                && w.Status != LaboratoryWorkStatus.Rejected)
            .AnyAsync(ct);

        if (openLabWorks)
            throw new InvalidOperationException(
                "Bu tedaviye bağlı laboratuvar işleri henüz onaylanmamış. Hakediş hesaplanamaz.");

        // ── SPEC 9351: Koşul 2 ─ Ödeme tam dağıtılmış olmalı ────────────────
        //    + SPEC 9353-9355: Koşul 4 ─ Kurum ödemesi zamanlaması
        var patient = await _db.Patients.AsNoTracking()
            .Where(p => p.Id == item.Plan.PatientId)
            .Select(p => new { p.AgreementInstitutionId })
            .FirstOrDefaultAsync(ct);

        var allocatedPatient = await _db.PaymentAllocations.AsNoTracking()
            .Where(a => a.TreatmentPlanItemId == r.TreatmentPlanItemId
                && a.Source == AllocationSource.Patient
                && !a.IsRefunded)
            .SumAsync(a => (decimal?)a.AllocatedAmount, ct) ?? 0m;

        var allocatedInstitution = await _db.PaymentAllocations.AsNoTracking()
            .Where(a => a.TreatmentPlanItemId == r.TreatmentPlanItemId
                && a.Source == AllocationSource.Institution
                && !a.IsRefunded)
            .SumAsync(a => (decimal?)a.AllocatedAmount, ct) ?? 0m;

        var totalAllocatedPaid = allocatedPatient + allocatedInstitution;

        // Kurum anlaşması varsa fatura kesilmiş mi kontrol et
        bool hasInstitutionInvoice = false;
        if (patient?.AgreementInstitutionId.HasValue == true)
        {
            hasInstitutionInvoice = await _db.InstitutionInvoices.AsNoTracking()
                .Where(inv =>
                    inv.PatientId == item.Plan.PatientId &&
                    inv.InstitutionId == patient.AgreementInstitutionId.Value &&
                    inv.Status != InstitutionInvoiceStatus.Rejected)
                .AnyAsync(ct);
        }

        // "Dağıtılmış sayılır" yaklaşımı:
        //   - Kurum anlaşması yok → sadece hasta dağıtımları sayılır.
        //   - Kurum anlaşması var + template "Fatura Kesilince" → fatura kesildiyse
        //     kurum payı "hak edilmiş" kabul edilir. Hasta dağıtımı + hasta payı hariç
        //     kalan kısım invoice toplamından çıkarılır. Basitleştirilmiş:
        //         toplam "hak edilen" = hasta dağıtımı + (fatura varsa) kurum ödemesi veya fatura bakiye.
        //   - Kurum anlaşması var + template "Kurum Ödeyince" → kurum ödemesi (allocationInstitution)
        //     tamamlanmalı.
        decimal totalConsideredPaid = totalAllocatedPaid;
        if (patient?.AgreementInstitutionId.HasValue == true && template.InstitutionPayOnInvoice)
        {
            if (!hasInstitutionInvoice)
                throw new InvalidOperationException(
                    "Kurum anlaşmalı tedavi için henüz fatura kesilmemiş. " +
                    "Hekim şablonu 'Fatura Kesilince' olduğundan hakediş hesaplanamaz.");
            // Fatura kesildiğinde kurum payı hak edilmiş sayılır; hasta + fatura bakiyesi ile
            // tam dağıtım oluşmuş kabul edilir.
            totalConsideredPaid = Math.Max(totalConsideredPaid, item.FinalPrice);
        }

        if (totalConsideredPaid + 0.01m < item.FinalPrice)
        {
            throw new InvalidOperationException(
                patient?.AgreementInstitutionId.HasValue == true && !template.InstitutionPayOnInvoice
                    ? "Kurum ödemesi henüz tahsil edilmemiş. " +
                      "Hekim şablonu 'Kurum Ödeyince' olduğundan hakediş hesaplanamaz."
                    : "Tedavi kaleminin ödemesi tam dağıtılmamış. Hakediş hesaplanamaz.");
        }

        // ── Hesaplama ───────────────────────────────────────────────────────
        var calc = await _calculator.CalculateAsync(r.TreatmentPlanItemId, ct);

        var existing = await _db.DoctorCommissions
            .FirstOrDefaultAsync(c =>
                c.TreatmentPlanItemId == r.TreatmentPlanItemId &&
                c.Status == CommissionStatus.Pending, ct);

        if (existing != null)
            _db.DoctorCommissions.Remove(existing);

        var commission = DoctorCommission.CreateCalculated(calc);
        _db.DoctorCommissions.Add(commission);

        await _db.SaveChangesAsync(ct);
        return FinanceMappings.ToResponse(commission);
    }
}
