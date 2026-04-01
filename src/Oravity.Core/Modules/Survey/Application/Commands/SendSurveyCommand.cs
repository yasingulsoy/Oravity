using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Notification.Application.Commands;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Survey.Application.Commands;

public record SendSurveyCommand(
    long TemplateId,
    long PatientId,
    long BranchId,
    long CompanyId,
    SurveyChannel Channel,
    long? AppointmentId = null
) : IRequest<Guid>;

public class SendSurveyCommandHandler : IRequestHandler<SendSurveyCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public SendSurveyCommandHandler(AppDbContext db, IMediator mediator)
    {
        _db       = db;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(
        SendSurveyCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _db.SurveyTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.IsActive, cancellationToken)
            ?? throw new NotFoundException($"Anket şablonu bulunamadı: {request.TemplateId}");

        var patient = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new NotFoundException($"Hasta bulunamadı: {request.PatientId}");

        var surveyResponse = SurveyResponse.Create(
            request.TemplateId, request.PatientId,
            request.BranchId, request.CompanyId,
            request.Channel, request.AppointmentId,
            tokenExpiryHours: 72);

        _db.SurveyResponses.Add(surveyResponse);
        await _db.SaveChangesAsync(cancellationToken);

        // SMS/Email kuyruğuna ekle (portal linki ile)
        var surveyLink = $"https://portal.disineplus.com/survey/{surveyResponse.Token}";
        var message    = $"Merhaba {patient.FirstName}, kliniğimiz hakkında ne düşündüğünüzü merak ediyoruz. Anket için: {surveyLink}";

        if (request.Channel == SurveyChannel.Sms && !string.IsNullOrWhiteSpace(patient.Phone))
        {
            await _mediator.Send(new QueueSmsCommand(
                patient.Phone, message, "SURVEY"), cancellationToken);
        }

        return surveyResponse.PublicId;
    }
}
