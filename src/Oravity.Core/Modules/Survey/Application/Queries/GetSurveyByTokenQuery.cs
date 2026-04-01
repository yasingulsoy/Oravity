using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Survey.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Survey.Application.Queries;

public record GetSurveyByTokenQuery(string Token) : IRequest<PublicSurveyResponse>;

public class GetSurveyByTokenQueryHandler
    : IRequestHandler<GetSurveyByTokenQuery, PublicSurveyResponse>
{
    private readonly AppDbContext _db;

    public GetSurveyByTokenQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PublicSurveyResponse> Handle(
        GetSurveyByTokenQuery request,
        CancellationToken cancellationToken)
    {
        var response = await _db.SurveyResponses
            .Include(r => r.Template)
                .ThenInclude(t => t.Questions)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Token == request.Token, cancellationToken)
            ?? throw new NotFoundException("Anket bulunamadı.");

        if (response.TokenExpiresAt < DateTime.UtcNow)
        {
            response.Expire();
            await _db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Anket linki süresi dolmuştur.");
        }

        if (response.Status == Oravity.SharedKernel.Entities.SurveyResponseStatus.Completed)
            throw new InvalidOperationException("Bu anket zaten tamamlanmış.");

        var questions = response.Template.Questions
            .OrderBy(q => q.SortOrder)
            .Select(SurveyMappings.ToResponse)
            .ToList();

        return new PublicSurveyResponse(
            response.PublicId,
            response.Template.Name,
            response.Status,
            questions);
    }
}
