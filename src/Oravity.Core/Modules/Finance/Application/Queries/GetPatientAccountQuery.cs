using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Queries;

// ─── DTO'lar ──────────────────────────────────────────────────────────────

public record PatientAccountItemResponse(
    long TreatmentPlanItemId,
    long? DoctorId,
    string? DoctorName,
    long TreatmentId,
    string TreatmentName,
    string? ToothNumber,
    TreatmentItemStatus Status,
    string StatusLabel,
    decimal FinalPrice,       // KDV hariç
    decimal TotalAmount,      // KDV dahil
    decimal AllocatedAmount,  // bu kaleme dağıtılmış tutar
    decimal RemainingAmount   // kalan borç
);

public record PatientAccountPaymentResponse(
    Guid PublicId,
    long Id,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    string MethodLabel,
    DateOnly PaymentDate,
    decimal AllocatedAmount,
    decimal UnallocatedAmount,
    bool IsRefunded
);

public record PatientAccountSummaryResponse(
    long PatientId,
    string? PatientName,
    decimal TotalTreatmentAmount,    // Tüm tedavi kalemlerinin toplamı
    decimal TotalPaid,               // Hastadan alınan toplam (iade hariç)
    decimal TotalAllocated,          // Dağıtılmış toplam
    decimal UnallocatedAmount,       // Dağıtılmamış (henüz bir tedaviye gitmeyen)
    decimal TotalRemaining,          // Bekleyen borç
    decimal Balance,                 // TotalPaid - TotalTreatmentAmount
    string BalanceLabel,
    IReadOnlyList<PatientAccountItemResponse> Items,
    IReadOnlyList<PatientAccountPaymentResponse> Payments
);

// ─── Query ────────────────────────────────────────────────────────────────

public record GetPatientAccountQuery(long PatientId) : IRequest<PatientAccountSummaryResponse>;

public class GetPatientAccountQueryHandler
    : IRequestHandler<GetPatientAccountQuery, PatientAccountSummaryResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetPatientAccountQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PatientAccountSummaryResponse> Handle(
        GetPatientAccountQuery r, CancellationToken ct)
    {
        var patient = await _db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == r.PatientId, ct)
            ?? throw new NotFoundException($"Hasta bulunamadı: {r.PatientId}");

        // ── Kalemler (tüm planlardan, iptal hariç) ────────────────────────
        var itemsQ = _db.TreatmentPlanItems.AsNoTracking()
            .Where(i => i.Plan.PatientId == r.PatientId && i.Status != TreatmentItemStatus.Cancelled);

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            itemsQ = itemsQ.Where(i => i.Plan.BranchId == _tenant.BranchId.Value);
        else if (_tenant.IsCompanyAdmin && _tenant.CompanyId.HasValue)
            itemsQ = itemsQ.Where(i => i.Plan.Branch.CompanyId == _tenant.CompanyId.Value);

        var raw = await (from i in itemsQ
                         join t in _db.Treatments.AsNoTracking() on i.TreatmentId equals t.Id
                         select new
                         {
                             i.Id,
                             DoctorId = i.DoctorId ?? i.Plan.DoctorId,
                             i.TreatmentId,
                             TreatmentName = t.Name,
                             i.ToothNumber,
                             i.Status,
                             i.FinalPrice,
                             i.TotalAmount
                         }).ToListAsync(ct);

        var doctorIds = raw.Select(x => x.DoctorId).Distinct().ToList();
        var doctorNames = await _db.Users.AsNoTracking()
            .Where(u => doctorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var items = raw.Select(x => new
        {
            x.Id,
            DoctorId = (long?)x.DoctorId,
            DoctorName = doctorNames.TryGetValue(x.DoctorId, out var n) ? n : null,
            x.TreatmentId,
            x.TreatmentName,
            x.ToothNumber,
            x.Status,
            x.FinalPrice,
            x.TotalAmount
        }).ToList();

        var itemIds = items.Select(x => x.Id).ToList();

        // ── Her kaleme düşen dağıtım toplamı ──────────────────────────────
        var allocByItem = await _db.PaymentAllocations.AsNoTracking()
            .Where(a => itemIds.Contains(a.TreatmentPlanItemId) && !a.IsRefunded)
            .GroupBy(a => a.TreatmentPlanItemId)
            .Select(g => new { ItemId = g.Key, Amount = g.Sum(x => x.AllocatedAmount) })
            .ToListAsync(ct);

        var allocDict = allocByItem.ToDictionary(x => x.ItemId, x => x.Amount);

        // ── Ödemeler ──────────────────────────────────────────────────────
        var paymentsQ = _db.Payments.AsNoTracking()
            .Where(p => p.PatientId == r.PatientId);

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            paymentsQ = paymentsQ.Where(p => p.BranchId == _tenant.BranchId.Value);
        else if (_tenant.IsCompanyAdmin && _tenant.CompanyId.HasValue)
            paymentsQ = paymentsQ.Where(p => p.Branch.CompanyId == _tenant.CompanyId.Value);

        var payments = await paymentsQ
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new
            {
                p.PublicId, p.Id, p.Amount, p.Currency, p.Method,
                p.PaymentDate, p.IsRefunded
            })
            .ToListAsync(ct);

        var paymentIds = payments.Select(p => p.Id).ToList();
        var allocByPayment = await _db.PaymentAllocations.AsNoTracking()
            .Where(a => a.PaymentId.HasValue && paymentIds.Contains(a.PaymentId!.Value) && !a.IsRefunded)
            .GroupBy(a => a.PaymentId!.Value)
            .Select(g => new { PaymentId = g.Key, Amount = g.Sum(x => x.AllocatedAmount) })
            .ToListAsync(ct);
        var payAllocDict = allocByPayment.ToDictionary(x => x.PaymentId, x => x.Amount);

        // ── Item DTO'ları ─────────────────────────────────────────────────
        var itemDtos = items.Select(x =>
        {
            var allocated = allocDict.GetValueOrDefault(x.Id);
            return new PatientAccountItemResponse(
                x.Id, x.DoctorId, x.DoctorName,
                x.TreatmentId, x.TreatmentName, x.ToothNumber,
                x.Status, StatusLabel(x.Status),
                x.FinalPrice, x.TotalAmount,
                allocated,
                Math.Max(0, x.TotalAmount - allocated));
        }).ToList();

        // ── Payment DTO'ları ──────────────────────────────────────────────
        var paymentDtos = payments.Select(p =>
        {
            var allocated = p.IsRefunded ? 0 : payAllocDict.GetValueOrDefault(p.Id);
            return new PatientAccountPaymentResponse(
                p.PublicId, p.Id, p.Amount, p.Currency, p.Method,
                FinanceMappings.MethodLabel(p.Method),
                p.PaymentDate, allocated, p.Amount - allocated, p.IsRefunded);
        }).ToList();

        // ── Özet ──────────────────────────────────────────────────────────
        var totalTreatment = itemDtos.Sum(x => x.TotalAmount);
        var totalPaid      = payments.Where(p => !p.IsRefunded).Sum(p => p.Amount);
        var totalAllocated = payAllocDict.Values.Sum();
        var unallocated    = totalPaid - totalAllocated;
        var remaining      = itemDtos.Sum(x => x.RemainingAmount);
        var balance        = totalPaid - totalTreatment;
        var balanceLabel   = balance switch
        {
            > 0 => $"+{balance:N2} TL (Alacak)",
            < 0 => $"{balance:N2} TL (Borç)",
            _   => "0,00 TL (Dengede)"
        };

        return new PatientAccountSummaryResponse(
            r.PatientId, $"{patient.FirstName} {patient.LastName}".Trim(),
            totalTreatment, totalPaid, totalAllocated, unallocated, remaining,
            balance, balanceLabel, itemDtos, paymentDtos);
    }

    private static string StatusLabel(TreatmentItemStatus s) => s switch
    {
        TreatmentItemStatus.Planned   => "Planlandı",
        TreatmentItemStatus.Approved  => "Onaylandı",
        TreatmentItemStatus.Completed => "Tamamlandı",
        TreatmentItemStatus.Cancelled => "İptal",
        _ => s.ToString()
    };
}
