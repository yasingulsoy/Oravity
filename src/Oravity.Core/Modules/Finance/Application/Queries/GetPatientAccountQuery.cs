using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Queries;

// ─── DTO'lar ──────────────────────────────────────────────────────────────

public record ItemAllocationDetail(
    decimal AllocatedAmount,   // TRY cinsinden dağıtılan tutar
    DateOnly PaymentDate,
    string   PaymentCurrency,  // ödemenin orijinal para birimi
    decimal  PaymentAmount,    // ödemenin orijinal para birimi cinsinden tutarı
    decimal  ExchangeRate,     // ödeme anındaki kur (TRY ise 1)
    string   MethodLabel       // Nakit, Kart, vb.
);

public record PatientAccountItemResponse(
    long TreatmentPlanItemId,
    Guid ItemPublicId,         // frontend item eşleştirmesi için
    long? DoctorId,
    string? DoctorName,
    long TreatmentId,
    string TreatmentName,
    string? ToothNumber,
    TreatmentItemStatus Status,
    string StatusLabel,
    string PriceCurrency,      // orijinal para birimi
    decimal FinalPrice,        // KDV hariç, orijinal para birimi
    decimal TotalAmount,       // KDV dahil, orijinal para birimi
    decimal TotalAmountTry,    // KDV dahil, TRY karşılığı (allocation bazı)
    decimal PatientAmount,     // hastanın gerçek borcu (TRY, kurum payı düşülmüş)
    decimal AllocatedAmount,   // bu kaleme dağıtılmış TRY tutarı
    decimal RemainingAmount,   // kalan TRY borç
    DateTime? CompletedAt,
    Guid PlanPublicId,                        // ContribInput için plan endpoint'i
    int? InstitutionPaymentModel,             // null=kurum yok, 1=indirim, 2=provizyon
    decimal? InstitutionContributionAmount,   // mevcut kurum payı (null=girilmemiş)
    IReadOnlyList<ItemAllocationDetail> AllocationDetails  // kalem bazında ödeme geçmişi
);

public record PatientAccountPaymentResponse(
    Guid PublicId,
    long Id,
    decimal Amount,       // Orijinal para birimi cinsinden
    string Currency,
    decimal ExchangeRate,
    decimal BaseAmount,   // TRY karşılığı (dövizde Amount × ExchangeRate)
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
    decimal TotalPlanned,            // Planned + Approved kalemlerin toplamı (gelecek borç)
    decimal TotalCompleted,          // Completed kalemlerin toplamı (gerçek borç)
    decimal TotalPaid,               // Hastadan alınan toplam (iade hariç)
    decimal TotalAllocated,          // Dağıtılmış toplam
    decimal UnallocatedAmount,       // Dağıtılmamış (henüz bir tedaviye gitmeyen)
    decimal TotalRemaining,          // Kalan borç (Completed - Allocated)
    decimal Balance,                 // TotalPaid - TotalCompleted
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
        // Completed + Planned/Approved hepsini göster; borç hesabında sadece Completed sayılır.
        var itemsQ = _db.TreatmentPlanItems.AsNoTracking()
            .Where(i => i.Plan.PatientId == r.PatientId
                     && i.Status != TreatmentItemStatus.Cancelled
                     && !i.IsDeleted);

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            itemsQ = itemsQ.Where(i => i.Plan.BranchId == _tenant.BranchId.Value);
        else if (_tenant.IsCompanyAdmin && _tenant.CompanyId.HasValue)
            itemsQ = itemsQ.Where(i => i.Plan.Branch.CompanyId == _tenant.CompanyId.Value);

        var raw = await (from i in itemsQ
                         join t in _db.Treatments.AsNoTracking() on i.TreatmentId equals t.Id
                         select new
                         {
                             i.Id,
                             i.PublicId,
                             DoctorId = i.DoctorId ?? i.Plan.DoctorId,
                             i.TreatmentId,
                             TreatmentName = t.Name,
                             i.ToothNumber,
                             i.Status,
                             i.PriceCurrency,
                             i.FinalPrice,
                             i.TotalAmount,
                             // TRY bazında tutar — her zaman karşılaştırılabilir
                             TotalAmountTry = i.PriceCurrency == "TRY"
                                 ? i.TotalAmount
                                 : i.PriceBaseAmount,
                             i.PatientAmount,
                             i.CompletedAt,
                             PlanPublicId                    = i.Plan.PublicId,
                             InstitutionPaymentModel         = i.Plan.InstitutionId != null ? (int?)i.Plan.Institution!.PaymentModel : null,
                             i.InstitutionContributionAmount,
                         }).ToListAsync(ct);

        var doctorIds = raw.Select(x => x.DoctorId).Distinct().ToList();
        var doctorNames = await _db.Users.AsNoTracking()
            .Where(u => doctorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var items = raw.Select(x => new
        {
            x.Id,
            x.PublicId,
            DoctorId = (long?)x.DoctorId,
            DoctorName = doctorNames.TryGetValue(x.DoctorId, out var n) ? n : null,
            x.TreatmentId,
            x.TreatmentName,
            x.ToothNumber,
            x.Status,
            x.PriceCurrency,
            x.FinalPrice,
            x.TotalAmount,
            x.TotalAmountTry,  // allocation ve özet hesapları için
            x.PatientAmount,
            x.CompletedAt,
            x.PlanPublicId,
            x.InstitutionPaymentModel,
            x.InstitutionContributionAmount,
        }).ToList();

        var itemIds = items.Select(x => x.Id).ToList();

        // ── Her kaleme düşen dağıtım detayları (payment join) ─────────────
        var allocRaw = await (
            from a in _db.PaymentAllocations.AsNoTracking()
            join p in _db.Payments.AsNoTracking() on a.PaymentId equals p.Id into pj
            from p in pj.DefaultIfEmpty()
            where itemIds.Contains(a.TreatmentPlanItemId) && !a.IsRefunded
            orderby p != null ? p.PaymentDate : default, a.Id
            select new
            {
                a.TreatmentPlanItemId,
                a.AllocatedAmount,
                PaymentDate     = p != null ? p.PaymentDate     : default(DateOnly),
                PaymentCurrency = p != null ? p.Currency        : "TRY",
                PaymentAmount   = p != null ? p.Amount          : a.AllocatedAmount,
                ExchangeRate    = p != null ? p.ExchangeRate    : 1m,
                Method          = p != null ? p.Method          : PaymentMethod.Cash,
            }
        ).ToListAsync(ct);

        // Toplam ve detay grupla
        var allocDict        = allocRaw
            .GroupBy(x => x.TreatmentPlanItemId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.AllocatedAmount));

        var allocDetailByItem = allocRaw
            .GroupBy(x => x.TreatmentPlanItemId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ItemAllocationDetail>)g
                .Select(x => new ItemAllocationDetail(
                    x.AllocatedAmount,
                    x.PaymentDate,
                    x.PaymentCurrency,
                    x.PaymentAmount,
                    x.ExchangeRate,
                    FinanceMappings.MethodLabel(x.Method)))
                .ToList());

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
                p.PublicId, p.Id,
                p.Amount, p.Currency, p.ExchangeRate, p.BaseAmount,
                p.Method, p.PaymentDate, p.IsRefunded
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
            var details   = allocDetailByItem.GetValueOrDefault(x.Id)
                            ?? (IReadOnlyList<ItemAllocationDetail>)[];
            return new PatientAccountItemResponse(
                x.Id, x.PublicId, x.DoctorId, x.DoctorName,
                x.TreatmentId, x.TreatmentName, x.ToothNumber,
                x.Status, StatusLabel(x.Status),
                x.PriceCurrency,
                x.FinalPrice, x.TotalAmount, x.TotalAmountTry,
                x.PatientAmount,
                allocated,
                Math.Max(0, x.PatientAmount - allocated),
                x.CompletedAt,
                x.PlanPublicId,
                x.InstitutionPaymentModel,
                x.InstitutionContributionAmount,
                details);
        }).ToList();

        // ── Payment DTO'ları ──────────────────────────────────────────────
        var paymentDtos = payments.Select(p =>
        {
            var allocated = p.IsRefunded ? 0 : payAllocDict.GetValueOrDefault(p.Id);
            // UnallocatedAmount = TRY bazında kalan (BaseAmount cinsinden)
            var unallocated = Math.Max(0, p.BaseAmount - allocated);
            return new PatientAccountPaymentResponse(
                p.PublicId, p.Id,
                p.Amount, p.Currency, p.ExchangeRate, p.BaseAmount,
                p.Method, FinanceMappings.MethodLabel(p.Method),
                p.PaymentDate, allocated, unallocated, p.IsRefunded);
        }).ToList();

        // ── Özet ──────────────────────────────────────────────────────────
        // Özetler TRY bazında — dövizli kalemlerde TotalAmountTry kullanılır
        var totalPlanned   = itemDtos
            .Where(x => x.Status is TreatmentItemStatus.Planned or TreatmentItemStatus.Approved)
            .Sum(x => x.PatientAmount);
        var totalCompleted = itemDtos
            .Where(x => x.Status == TreatmentItemStatus.Completed)
            .Sum(x => x.PatientAmount);
        // BaseAmount = TRY karşılığı; döviz ödemelerinde Amount ≠ TRY
        var totalPaid      = payments.Where(p => !p.IsRefunded).Sum(p => p.BaseAmount);
        var totalAllocated = payAllocDict.Values.Sum();
        var unallocated    = totalPaid - totalAllocated;
        // Kalan borç: sadece tamamlanan tedavilerden hesaplanır
        var remaining      = itemDtos
            .Where(x => x.Status == TreatmentItemStatus.Completed)
            .Sum(x => x.RemainingAmount);
        var balance        = totalPaid - totalCompleted;
        var balanceLabel   = balance switch
        {
            > 0 => $"+{balance:N2} TL (Alacak)",
            < 0 => $"{balance:N2} TL (Borç)",
            _   => "0,00 TL (Dengede)"
        };

        return new PatientAccountSummaryResponse(
            r.PatientId, $"{patient.FirstName} {patient.LastName}".Trim(),
            totalPlanned, totalCompleted, totalPaid, totalAllocated, unallocated, remaining,
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
