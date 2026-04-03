using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Events;

namespace Oravity.Infrastructure.Jobs.Handlers;

/// <summary>
/// AppointmentCompletedEvent işleyicisi — Outbox üzerinden tetiklenir.
///
/// Yapılan işlemler:
///   1. Tamamlanan randevunun tedavi kalemlerini bul.
///   2. Her kalem için DoctorCommission oluştur (yoksa).
///      - Hakediş oranı: Sabit %30 (ileride UserProfile'dan okunacak).
///   3. Post-appointment anket planla (SurveySchedulerHandler).
///
/// SPEC §MİMARİ REVİZYON v2 §2 → "AppointmentCompletedEvent → Hakediş hesaplama"
/// </summary>
public class AppointmentCompletedEventHandler
    : INotificationHandler<AppointmentCompletedEvent>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;
    private readonly ILogger<AppointmentCompletedEventHandler> _logger;

    /// <summary>
    /// Varsayılan hekim hakediş oranı.
    /// Faz 2'de User/Doctor profilinden dinamik okunacak.
    /// </summary>
    private const decimal DefaultCommissionRate = 30m;

    public AppointmentCompletedEventHandler(
        AppDbContext db,
        IMediator mediator,
        ILogger<AppointmentCompletedEventHandler> logger)
    {
        _db       = db;
        _mediator = mediator;
        _logger   = logger;
    }

    public async Task Handle(AppointmentCompletedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "AppointmentCompletedEventHandler: Randevu {AppointmentId} işleniyor",
            notification.AppointmentId);

        // ── 1. Tamamlanan tedavi kalemlerini al ──────────────────────────
        var completedItems = await _db.TreatmentPlanItems
            .Include(i => i.Plan)
            .Where(i => i.Plan.PatientId == notification.PatientId &&
                        i.Status == TreatmentItemStatus.Completed &&
                        i.CompletedAt >= notification.CompletedAt.AddMinutes(-5))
            .ToListAsync(ct);

        // ── 2. Her kalem için komisyon oluştur (idempotent) ──────────────
        foreach (var item in completedItems)
        {
            var alreadyExists = await _db.DoctorCommissions
                .AnyAsync(c => c.TreatmentPlanItemId == item.Id, ct);

            if (alreadyExists) continue;

            var doctorId = item.DoctorId ?? notification.DoctorId;

            var commission = DoctorCommission.Create(
                doctorId:           doctorId,
                treatmentPlanItemId: item.Id,
                branchId:           notification.BranchId,
                grossAmount:        item.FinalPrice,
                commissionRate:     DefaultCommissionRate);

            _db.DoctorCommissions.Add(commission);

            _logger.LogDebug(
                "Komisyon oluşturuldu: Hekim={DoctorId} Tutar={Amount:F2} TL",
                doctorId, commission.CommissionAmount);
        }

        if (completedItems.Count > 0)
            await _db.SaveChangesAsync(ct);

        // ── 3. Randevu-sonrası anket planla ─────────────────────────────
        // Post-appointment survey scheduling için SurveySchedulerHandler
        // MediatR üzerinden ayrı INotification olarak tetiklenebilir.
        // Şimdilik loglama yapılıyor; SurveySchedulerJob kendi döngüsünde
        // randevu tamamlananları zaten otomatik tarar.
        _logger.LogInformation(
            "AppointmentCompletedEventHandler: tamamlandı — Randevu {AppointmentId}, " +
            "{Count} komisyon kaydı",
            notification.AppointmentId, completedItems.Count);
    }
}
