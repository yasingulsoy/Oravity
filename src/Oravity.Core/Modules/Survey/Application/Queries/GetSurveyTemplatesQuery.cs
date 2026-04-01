using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Survey.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Survey.Application.Queries;

public record GetSurveyTemplatesQuery : IRequest<IReadOnlyList<SurveyTemplateResponse>>;

public class GetSurveyTemplatesQueryHandler
    : IRequestHandler<GetSurveyTemplatesQuery, IReadOnlyList<SurveyTemplateResponse>>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetSurveyTemplatesQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<SurveyTemplateResponse>> Handle(
        GetSurveyTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenant.CompanyId
            ?? throw new UnauthorizedAccessException("Şirket bağlamı bulunamadı.");

        var templates = await _db.SurveyTemplates
            .Include(t => t.Questions)
            .Where(t => t.CompanyId == companyId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return templates.Select(SurveyMappings.ToResponse).ToList();
    }
}
