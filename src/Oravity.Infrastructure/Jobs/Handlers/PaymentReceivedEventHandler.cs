using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Events;

namespace Oravity.Infrastructure.Jobs.Handlers;

public class PaymentReceivedEventHandler
    : INotificationHandler<PaymentReceivedEvent>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentReceivedEventHandler> _logger;

    public PaymentReceivedEventHandler(
        AppDbContext db,
        IMediator mediator,
        ILogger<PaymentReceivedEventHandler> logger)
    {
        _db       = db;
        _mediator = mediator;
        _logger   = logger;
    }

    public async Task Handle(PaymentReceivedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "PaymentReceivedEventHandler: Ödeme {PaymentPublicId} işleniyor — {Amount} {Currency}",
            notification.PublicId, notification.Amount, notification.Currency);

        // ── 1. Ödeme varlığını doğrula ───────────────────────────────────
        var payment = await _db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PublicId == notification.PublicId, ct);

        if (payment is null)
        {
            _logger.LogWarning(
                "PaymentReceivedEventHandler: Ödeme bulunamadı — {PublicId}",
                notification.PublicId);
            return;
        }

        // ── 2. Hasta bakiyesini hesapla (logging / audit) ────────────────
        var totalTreatment = await _db.TreatmentPlanItems
            .Where(i => i.Plan.PatientId == notification.PatientId &&
                        i.Status == SharedKernel.Entities.TreatmentItemStatus.Completed)
            .SumAsync(i => (decimal?)i.FinalPrice ?? 0m, ct);

        var totalPayments = await _db.Payments
            .Where(p => p.PatientId == notification.PatientId && !p.IsRefunded)
            .SumAsync(p => (decimal?)p.Amount ?? 0m, ct);

        var balance = totalTreatment - totalPayments;

        _logger.LogInformation(
            "Hasta bakiyesi güncellendi: PatientId={PatientId}, Toplam Tedavi={Treatment:F2} TL, " +
            "Toplam Ödeme={Payments:F2} TL, Bakiye={Balance:F2} TL",
            notification.PatientId, totalTreatment, totalPayments, balance);

        // ── 3. Makbuz SMS kuyruğuna ekle ─────────────────────────────────
        var patient = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == notification.PatientId, ct);

        if (patient is not null && !string.IsNullOrWhiteSpace(patient.Phone))
        {
            var methodLabel = notification.Method switch
            {
                "Cash"         => "Nakit",
                "CreditCard"   => "Kredi Kartı",
                "BankTransfer" => "Havale/EFT",
                "Installment"  => "Taksit",
                "Check"        => "Çek",
                _              => notification.Method
            };

            var smsMessage = $"Sayın {patient.FirstName} {patient.LastName}, " +
                             $"{notification.Amount:F2} {notification.Currency} tutarındaki ödemeniz " +
                             $"({methodLabel}) başarıyla alındı. " +
                             $"Kalan bakiyeniz: {(balance > 0 ? balance : 0m):F2} {notification.Currency}. " +
                             $"Teşekkür ederiz.";

            var smsQueue = SharedKernel.Entities.SmsQueue.Create(
                companyId:   0,        // TODO: branch.CompanyId — sonraki fazda resolve edilecek
                providerId:  1,
                toPhone:     patient.Phone,
                message:     smsMessage,
                sourceType:  "PAYMENT_RECEIPT");

            _db.SmsQueues.Add(smsQueue);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Makbuz SMS kuyruğuna eklendi: {Phone}", patient.Phone);
        }
    }
}
