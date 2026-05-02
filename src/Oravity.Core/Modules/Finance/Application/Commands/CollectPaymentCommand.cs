using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Finance.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Commands;

/// <summary>
/// Ödeme al + tamamlanan kalemlere otomatik dağıt (tek transaction).
/// Dağıtım: CompletedAt'a göre FIFO (en eski kalem önce kapanır).
/// Ödeme fazlaysa kalan dağıtılmamış olarak kalır (UnallocatedAmount).
/// </summary>
public record CollectPaymentCommand(
    long      PatientId,
    decimal   Amount,
    PaymentMethod Method,
    DateOnly  PaymentDate,
    string    Currency      = "TRY",
    decimal   ExchangeRate  = 1m,
    string?   Notes         = null,
    Guid?     PosTerminalId = null,
    Guid?     BankAccountId = null
) : IRequest<CollectPaymentResult>;

public record CollectPaymentResult(
    PaymentResponse                    Payment,
    IReadOnlyList<PaymentAllocationResponse> Allocations,
    decimal                            TotalAllocated,
    decimal                            UnallocatedAmount
);

public class CollectPaymentCommandHandler
    : IRequestHandler<CollectPaymentCommand, CollectPaymentResult>
{
    private readonly AppDbContext  _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser  _user;

    public CollectPaymentCommandHandler(
        AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db     = db;
        _tenant = tenant;
        _user   = user;
    }

    public async Task<CollectPaymentResult> Handle(
        CollectPaymentCommand request, CancellationToken ct)
    {
        // BranchId: branch-level kullanıcıda JWT'den gelir; company admin için hastanın aktif planından çözümlenir.
        long branchId;
        if (_tenant.BranchId.HasValue)
        {
            branchId = _tenant.BranchId.Value;
        }
        else
        {
            // Company admin veya platform admin: hastanın en son tamamlanan planının şubesini kullan
            var patientBranchId = await _db.TreatmentPlans.AsNoTracking()
                .Where(p => p.PatientId == request.PatientId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => (long?)p.BranchId)
                .FirstOrDefaultAsync(ct);

            branchId = patientBranchId
                ?? throw new InvalidOperationException("Hastanın şube bilgisi belirlenemedi.");
        }

        // ── Geçmiş tarih kontrolü ─────────────────────────────────────────
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.PaymentDate < today && !_user.HasPermission("payment.backdate"))
            throw new ForbiddenException("Geçmiş tarihli ödeme girmek için yetkiniz yok.");

        // ── 1. Ödemeyi oluştur ────────────────────────────────────────────
        long? posTerminalId = null;
        if (request.PosTerminalId.HasValue)
            posTerminalId = await _db.PosTerminals.AsNoTracking()
                .Where(p => p.PublicId == request.PosTerminalId.Value && !p.IsDeleted)
                .Select(p => (long?)p.Id).FirstOrDefaultAsync(ct);

        long? bankAccountId = null;
        if (request.BankAccountId.HasValue)
            bankAccountId = await _db.BankAccounts.AsNoTracking()
                .Where(b => b.PublicId == request.BankAccountId.Value && !b.IsDeleted)
                .Select(b => (long?)b.Id).FirstOrDefaultAsync(ct);

        var payment = Payment.Create(
            patientId:    request.PatientId,
            branchId:     branchId,
            amount:       request.Amount,
            method:       request.Method,
            paymentDate:  request.PaymentDate,
            currency:     request.Currency,
            exchangeRate: request.Currency == "TRY" ? 1m : request.ExchangeRate,
            notes:        request.Notes,
            posTerminalId: posTerminalId,
            bankAccountId: bankAccountId);

        if (_user.IsAuthenticated)
            payment.SetCreatedBy(_user.UserId, _user.TenantId);

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct); // Id gerekiyor

        // ── 2. Tamamlanan kalemleri al (FIFO: en eski önce) ──────────────
        var completedItems = await (
            from i in _db.TreatmentPlanItems.AsNoTracking()
            join plan in _db.TreatmentPlans.AsNoTracking() on i.PlanId equals plan.Id
            where plan.PatientId  == request.PatientId
               && plan.BranchId   == branchId
               && i.Status        == TreatmentItemStatus.Completed
               && !i.IsDeleted
            orderby i.CompletedAt ascending, i.Id ascending
            select new
            {
                i.Id,
                i.PatientAmount,   // TRY (kurum payı düşülmüş)
                i.PriceCurrency,   // aynı döviz karşılaştırması için
                i.TotalAmount,     // orijinal para birimi cinsinden tutar (ör. 350 EUR)
                i.PriceBaseAmount, // TRY karşılığı (tamamlandı anındaki kur)
                i.CompletedAt
            }
        ).ToListAsync(ct);

        if (completedItems.Count == 0)
        {
            // Ödeme kaydedildi ama dağıtılacak tamamlanmış kalem yok
            var outboxPayload = JsonSerializer.Serialize(new
            {
                payment.PublicId, payment.PatientId, payment.BranchId,
                payment.Amount, payment.Currency,
                Method      = payment.Method.ToString(),
                payment.PaymentDate
            });
            _db.OutboxMessages.Add(OutboxMessage.Create("PaymentReceived", outboxPayload));
            await _db.SaveChangesAsync(ct);

            return new CollectPaymentResult(
                FinanceMappings.ToResponse(payment), [], 0, payment.BaseAmount);
        }

        // ── 3. Mevcut allocation'ları çek ─────────────────────────────────
        var itemIds      = completedItems.Select(i => i.Id).ToList();
        var allocByItem  = await _db.PaymentAllocations.AsNoTracking()
            .Where(a => itemIds.Contains(a.TreatmentPlanItemId) && !a.IsRefunded)
            .GroupBy(a => a.TreatmentPlanItemId)
            .Select(g => new { ItemId = g.Key, Allocated = g.Sum(x => x.AllocatedAmount) })
            .ToListAsync(ct);
        var allocDict = allocByItem.ToDictionary(x => x.ItemId, x => x.Allocated);

        // ── 4. FIFO dağıtım ──────────────────────────────────────────────
        // Aynı döviz kuralı: ödeme EUR, kalem EUR ise → EUR cinsinden karşılaştır.
        // 350 EUR ödeyen hasta 350 EUR'luk implantı tam kapatır; kur farkı kliniğe aittir.
        //
        // Sıralama: döviz ödemede aynı para birimli kalemler önce gelir (TRY bütçesi
        // yanlışlıkla TRY kalemlere harcanmadan EUR kalemler kapanır); sonra TRY kalemler.
        var userId       = _user.IsAuthenticated ? _user.UserId : 0L;
        var remainingTry = payment.BaseAmount;
        var remainingFx  = payment.Amount;
        bool isFxPayment = payment.Currency != "TRY";

        var orderedItems = isFxPayment
            ? completedItems
                .OrderBy(i => i.PriceCurrency == payment.Currency ? 0 : 1)
                .ThenBy(i => i.CompletedAt)
                .ThenBy(i => i.Id)
                .ToList()
            : completedItems;

        var created = new List<PaymentAllocation>();

        foreach (var item in orderedItems)
        {
            // Döviz ödemede: hem TRY hem orijinal döviz tükendiyse dur
            if (isFxPayment ? (remainingFx <= 0.001m && remainingTry <= 0) : remainingTry <= 0)
                break;

            var alreadyPaid = allocDict.GetValueOrDefault(item.Id);
            decimal toAllocate;

            if (isFxPayment && payment.Currency == item.PriceCurrency && item.TotalAmount > 0)
            {
                // Aynı döviz: EUR bazında karşılaştır
                // Tamamlandı anındaki kur: PriceBaseAmount / TotalAmount  (ör. 14.000 / 350 = 40 TRY/EUR)
                var completionRate    = item.PriceBaseAmount / item.TotalAmount;
                var alreadyPaidFx     = completionRate > 0 ? alreadyPaid / completionRate : 0m;
                var itemRemainingFx   = item.TotalAmount - alreadyPaidFx;

                if (itemRemainingFx <= 0.001m || remainingFx <= 0.001m) continue;

                var fxToAllocate = Math.Min(remainingFx, itemRemainingFx);
                bool fullClose   = fxToAllocate >= itemRemainingFx - 0.001m;

                // Tam kapanıyorsa: kalemde kalan TRY'nin tamamı → kur farkı klinik üstlenir
                // Kısmi kapanma: ödeme anındaki kurdan TRY'ye çevir
                toAllocate = fullClose
                    ? Math.Max(0m, item.PatientAmount - alreadyPaid)
                    : Math.Round(fxToAllocate * payment.ExchangeRate, 2);

                if (toAllocate <= 0) continue;

                remainingFx  -= fxToAllocate;
                remainingTry -= toAllocate; // kur farkı varsa negatife düşebilir (normal)
            }
            else
            {
                // Standart TRY bazlı dağıtım
                if (remainingTry <= 0) continue;
                var itemRemainingTry = item.PatientAmount - alreadyPaid;
                if (itemRemainingTry <= 0) continue;

                // Döviz ödemede zorunlu yuvarlama kaynaklı kuruş farkı (≤ ₺1):
                // klinik üstlenir — ileride kambiyo kârı/zararı olarak muhasebeleştirilecek.
                bool kurFarkiKapat = isFxPayment
                    && itemRemainingTry > remainingTry
                    && (itemRemainingTry - remainingTry) <= 1m;

                toAllocate    = kurFarkiKapat ? itemRemainingTry : Math.Min(remainingTry, itemRemainingTry);
                remainingTry -= toAllocate;
            }

            var alloc = PaymentAllocation.CreateFromPatient(
                paymentId:           payment.Id,
                treatmentPlanItemId: item.Id,
                branchId:            branchId,
                allocatedAmount:     toAllocate,
                allocatedByUserId:   userId,
                method:              AllocationMethod.Automatic);

            _db.PaymentAllocations.Add(alloc);
            created.Add(alloc);
        }

        // ── 5. Outbox event ───────────────────────────────────────────────
        var payload = JsonSerializer.Serialize(new
        {
            payment.PublicId, payment.PatientId, payment.BranchId,
            payment.Amount, payment.Currency,
            Method      = payment.Method.ToString(),
            payment.PaymentDate
        });
        _db.OutboxMessages.Add(OutboxMessage.Create("PaymentReceived", payload));

        await _db.SaveChangesAsync(ct);

        return new CollectPaymentResult(
            FinanceMappings.ToResponse(payment),
            created.Select(FinanceMappings.ToResponse).ToList(),
            created.Sum(a => a.AllocatedAmount),
            Math.Max(0m, remainingTry));
    }
}
