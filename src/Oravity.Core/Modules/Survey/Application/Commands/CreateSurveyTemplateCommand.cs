using MediatR;
using Oravity.Core.Modules.Survey.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Survey.Application.Commands;

public record CreateSurveyTemplateCommand(
    string Name,
    SurveyTriggerType TriggerType,
    int TriggerDelayHours = 24,
    string? Description = null
) : IRequest<SurveyTemplateResponse>;

public class CreateSurveyTemplateCommandHandler
    : IRequestHandler<CreateSurveyTemplateCommand, SurveyTemplateResponse>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly ITenantContext _tenant;

    public CreateSurveyTemplateCommandHandler(
        AppDbContext db, ICurrentUser user, ITenantContext tenant)
    {
        _db     = db;
        _user   = user;
        _tenant = tenant;
    }

    public async Task<SurveyTemplateResponse> Handle(
        CreateSurveyTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenant.CompanyId
            ?? throw new UnauthorizedAccessException("Şirket bağlamı bulunamadı.");

        var template = SurveyTemplate.Create(
            companyId, request.Name, request.TriggerType,
            _user.UserId, request.Description, request.TriggerDelayHours);

        _db.SurveyTemplates.Add(template);
        await _db.SaveChangesAsync(cancellationToken);

        return SurveyMappings.ToResponse(template);
    }
}
