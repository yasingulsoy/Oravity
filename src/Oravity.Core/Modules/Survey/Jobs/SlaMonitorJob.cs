using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oravity.Core.Modules.Notification.Application.Commands;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Survey.Jobs;

/// <summary>
/// Her 30 dakikada çalışır — SLA süresi yaklaşan şikayetleri izler.
/// SLA 2 saatinin altındaysa sorumluya uyarı bildirim gönderir.
/// SLA süresi aşılmış şikayetler escalation için işaretlenir.
/// </summary>
public class SlaMonitorJob
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;
    private readonly ILogger<SlaMonitorJob> _logger;

    public SlaMonitorJob(
        AppDbContext db,
        IMediator mediator,
        ILogger<SlaMonitorJob> logger)
    {
        _db       = db;
        _mediator = mediator;
        _logger   = logger;
    }

    [DisableConcurrentExecution(120)]
    public async Task Execute()
    {
        await NotifyApproachingSla();
        await EscalateBreachedComplaints();
    }

    /// <summary>SLA süresi 2 saat içinde dolacak şikayetler için sorumluya bildirim.</summary>
    private async Task NotifyApproachingSla()
    {
        var threshold   = DateTime.UtcNow.AddHours(2);
        var approaching = await _db.Complaints
            .Where(c => c.Status != ComplaintStatus.Resolved &&
                        c.Status != ComplaintStatus.Closed &&
                        c.SlaDueAt.HasValue &&
                        c.SlaDueAt > DateTime.UtcNow &&
                        c.SlaDueAt <= threshold &&
                        c.AssignedTo.HasValue)
            .AsNoTracking()
            .ToListAsync();

        foreach (var complaint in approaching)
        {
            var remaining = (complaint.SlaDueAt!.Value - DateTime.UtcNow).TotalHours;

            await _mediator.Send(new SendInAppNotificationCommand(
                BranchId:          complaint.BranchId,
                Type:              NotificationType.Urgent,
                Title:             "SLA Uyarısı",
                Message:           $"Şikayet '{complaint.Subject}' için SLA süreniz yaklaşıyor. Kalan: {remaining:F0} saat.",
                ToUserId:          complaint.AssignedTo!.Value,
                CompanyId:         complaint.CompanyId,
                IsUrgent:          true,
                RelatedEntityType: "Complaint",
                RelatedEntityId:   complaint.Id));

            _logger.LogInformation(
                "SlaMonitorJob: SLA uyarısı gönderildi — Complaint {Id}, Remaining {Hours:F1}h",
                complaint.Id, remaining);
        }
    }

    /// <summary>SLA süresi dolmuş (breached) açık şikayetleri Escalated olarak işaretler.</summary>
    private async Task EscalateBreachedComplaints()
    {
        var breached = await _db.Complaints
            .Where(c => c.Status != ComplaintStatus.Resolved &&
                        c.Status != ComplaintStatus.Closed &&
                        c.Status != ComplaintStatus.Escalated &&
                        c.SlaDueAt.HasValue &&
                        c.SlaDueAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var complaint in breached)
        {
            complaint.UpdateStatus(ComplaintStatus.Escalated);

            _logger.LogWarning(
                "SlaMonitorJob: SLA ihlali — Complaint {Id} '{Subject}' eskalasyona alındı.",
                complaint.Id, complaint.Subject);
        }

        if (breached.Count > 0)
            await _db.SaveChangesAsync();
    }
}
