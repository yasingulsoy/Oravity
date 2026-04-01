using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Survey.Application.Commands;

public record SchedulePostAppointmentSurveyCommand(
    long AppointmentId,
    long CompanyId
) : IRequest;

public class SchedulePostAppointmentSurveyCommandHandler
    : IRequestHandler<SchedulePostAppointmentSurveyCommand>
{
    private readonly AppDbContext _db;
    private readonly IBackgroundJobClient _hangfire;
    private readonly ITenantContext _tenant;

    public SchedulePostAppointmentSurveyCommandHandler(
        AppDbContext db, IBackgroundJobClient hangfire, ITenantContext tenant)
    {
        _db      = db;
        _hangfire = hangfire;
        _tenant  = tenant;
    }

    public async Task Handle(
        SchedulePostAppointmentSurveyCommand request,
        CancellationToken cancellationToken)
    {
        // Şirkete ait aktif randevu-sonrası anket şablonlarını bul
        var templates = await _db.SurveyTemplates
            .Where(t => t.CompanyId == request.CompanyId &&
                        t.IsActive &&
                        t.TriggerType == SurveyTriggerType.PostAppointment)
            .Select(t => new { t.Id, t.TriggerDelayHours })
            .ToListAsync(cancellationToken);

        foreach (var template in templates)
        {
            // Hangfire delayed job: trigger_delay_hours sonra SendSurveyCommand gönder
            var delay = TimeSpan.FromHours(template.TriggerDelayHours);
            _hangfire.Schedule<ISendSurveyJob>(
                job => job.SendForAppointment(template.Id, request.AppointmentId),
                delay);
        }
    }
}

/// <summary>Hangfire tarafından çağrılan anket gönderim arayüzü.</summary>
public interface ISendSurveyJob
{
    Task SendForAppointment(long templateId, long appointmentId);
}
