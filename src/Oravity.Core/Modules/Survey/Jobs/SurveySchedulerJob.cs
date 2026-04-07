using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oravity.Core.Modules.Survey.Application.Commands;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Survey.Jobs;

/// <summary>
/// Her 5 dakikada çalışır — manuel / periyodik anket gönderim takibi.
/// PostAppointment gönderimler SchedulePostAppointmentSurveyCommand üzerinden
/// randevu tamamlandığında delayed job olarak planlanır; bu job bunları çalıştırır.
/// Ayrıca token süresi dolan yanıtları Expired olarak işaretler.
/// </summary>
public class SurveySchedulerJob
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;
    private readonly ILogger<SurveySchedulerJob> _logger;

    public SurveySchedulerJob(
        AppDbContext db,
        IMediator mediator,
        ILogger<SurveySchedulerJob> logger)
    {
        _db       = db;
        _mediator = mediator;
        _logger   = logger;
    }

    [DisableConcurrentExecution(60)]
    public async Task Execute()
    {
        await ExpireStaleResponses();
    }

    /// <summary>Token süresi dolmuş ama hâlâ "Sent" statüsündeki yanıtları Expired yap.</summary>
    private async Task ExpireStaleResponses()
    {
        var stale = await _db.SurveyResponses
            .Where(r => r.Status == SurveyResponseStatus.Sent &&
                        r.TokenExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var r in stale)
            r.Expire();

        if (stale.Count > 0)
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("SurveySchedulerJob: {Count} yanıt Expired olarak işaretlendi.", stale.Count);
        }
    }
}

/// <summary>
/// Hangfire tarafından çağrılan anket gönderim job'ı.
/// SchedulePostAppointmentSurveyCommand tarafından planlanır.
/// </summary>
public class SendSurveyJob : ISendSurveyJob
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _db;
    private readonly ILogger<SendSurveyJob> _logger;

    public SendSurveyJob(IMediator mediator, AppDbContext db, ILogger<SendSurveyJob> logger)
    {
        _mediator = mediator;
        _db       = db;
        _logger   = logger;
    }

    public async Task SendForAppointment(long templateId, long appointmentId)
    {
        var apt = await _db.Appointments
            .Include(a => a.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (apt is null || apt.PatientId is null)
        {
            _logger.LogWarning("SendSurveyJob: Randevu bulunamadı veya hasta yok {AppointmentId}", appointmentId);
            return;
        }

        await _mediator.Send(new SendSurveyCommand(
            templateId,
            apt.PatientId.Value,
            apt.BranchId,
            apt.Branch.CompanyId,
            SurveyChannel.Sms,
            appointmentId));
    }
}
