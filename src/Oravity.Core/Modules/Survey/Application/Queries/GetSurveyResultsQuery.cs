using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Survey.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Survey.Application.Queries;

public record GetSurveyResultsQuery(
    long TemplateId,
    DateTime? From = null,
    DateTime? To = null
) : IRequest<SurveyResultsResponse>;

public class GetSurveyResultsQueryHandler
    : IRequestHandler<GetSurveyResultsQuery, SurveyResultsResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetSurveyResultsQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<SurveyResultsResponse> Handle(
        GetSurveyResultsQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenant.CompanyId
            ?? throw new UnauthorizedAccessException("Şirket bağlamı bulunamadı.");

        var template = await _db.SurveyTemplates
            .Include(t => t.Questions)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.CompanyId == companyId, cancellationToken)
            ?? throw new NotFoundException($"Şablon bulunamadı: {request.TemplateId}");

        var query = _db.SurveyResponses
            .Include(r => r.Answers)
            .Where(r => r.TemplateId == request.TemplateId &&
                        r.CompanyId == companyId);

        if (request.From.HasValue) query = query.Where(r => r.SentAt >= request.From.Value);
        if (request.To.HasValue)   query = query.Where(r => r.SentAt <= request.To.Value);

        var responses = await query.AsNoTracking().ToListAsync(cancellationToken);

        var total     = responses.Count;
        var completed = responses.Count(r => r.Status == SurveyResponseStatus.Completed);
        var rate      = total > 0 ? (decimal)completed / total * 100 : 0m;
        var avgScore  = responses.Where(r => r.AverageScore.HasValue)
                                 .Select(r => r.AverageScore!.Value)
                                 .DefaultIfEmpty()
                                 .Average();

        // NPS skoru: (Promoters-Detractors)/Total * 100
        var npsScores  = responses.Where(r => r.NpsScore.HasValue).Select(r => r.NpsScore!.Value).ToList();
        double? nps    = null;
        if (npsScores.Count > 0)
        {
            var promoters  = npsScores.Count(s => s >= 9);
            var detractors = npsScores.Count(s => s <= 6);
            nps = (double)(promoters - detractors) / npsScores.Count * 100;
        }

        // Soru bazında istatistik
        var allAnswers = responses.SelectMany(r => r.Answers).ToList();
        var questionResults = template.Questions.Select(q =>
        {
            var qAnswers = allAnswers.Where(a => a.QuestionId == q.Id).ToList();

            decimal? qAvg = null;
            var distribution = new Dictionary<string, int>();

            if (q.QuestionType == QuestionType.Star)
            {
                var scores = qAnswers.Where(a => a.AnswerScore.HasValue)
                                     .Select(a => a.AnswerScore!.Value).ToList();
                if (scores.Count > 0) qAvg = (decimal)scores.Average();
                for (int i = 1; i <= 5; i++)
                    distribution[i.ToString()] = scores.Count(s => s == i);
            }
            else if (q.QuestionType == QuestionType.YesNo)
            {
                var yes = qAnswers.Count(a => a.AnswerBoolean == true);
                var no  = qAnswers.Count(a => a.AnswerBoolean == false);
                distribution["Evet"]  = yes;
                distribution["Hayır"] = no;
            }
            else if (q.QuestionType == QuestionType.MultipleChoice)
            {
                foreach (var a in qAnswers.Where(a => a.SelectedOption != null))
                    distribution[a.SelectedOption!] = distribution.GetValueOrDefault(a.SelectedOption!) + 1;
            }

            return new QuestionResultItem(q.Id, q.QuestionText, q.QuestionType, qAvg, distribution);
        }).ToList();

        return new SurveyResultsResponse(
            template.Id, template.Name,
            total, completed, rate,
            avgScore > 0 ? avgScore : null, nps,
            questionResults);
    }
}
