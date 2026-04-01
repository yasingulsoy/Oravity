using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Survey.Application.Commands;

public record AnswerInput(
    long QuestionId,
    int? Score,
    bool? BoolAnswer,
    string? TextAnswer,
    string? SelectedOption
);

public record SubmitSurveyResponseCommand(
    string Token,
    IReadOnlyList<AnswerInput> Answers
) : IRequest<bool>;

public class SubmitSurveyResponseCommandHandler
    : IRequestHandler<SubmitSurveyResponseCommand, bool>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public SubmitSurveyResponseCommandHandler(AppDbContext db, IMediator mediator)
    {
        _db       = db;
        _mediator = mediator;
    }

    public async Task<bool> Handle(
        SubmitSurveyResponseCommand request,
        CancellationToken cancellationToken)
    {
        var response = await _db.SurveyResponses
            .Include(r => r.Template)
            .FirstOrDefaultAsync(r => r.Token == request.Token, cancellationToken)
            ?? throw new NotFoundException("Anket bulunamadı.");

        if (!response.IsTokenValid())
            throw new InvalidOperationException("Anket linki süresi dolmuş veya zaten tamamlanmış.");

        // Cevapları kaydet
        foreach (var input in request.Answers)
        {
            SurveyAnswer answer;

            if (input.Score.HasValue)
                answer = SurveyAnswer.CreateStarAnswer(response.Id, input.QuestionId, input.Score.Value);
            else if (input.BoolAnswer.HasValue)
                answer = SurveyAnswer.CreateYesNoAnswer(response.Id, input.QuestionId, input.BoolAnswer.Value);
            else if (!string.IsNullOrWhiteSpace(input.SelectedOption))
                answer = SurveyAnswer.CreateMultipleChoiceAnswer(response.Id, input.QuestionId, input.SelectedOption);
            else
                answer = SurveyAnswer.CreateTextAnswer(response.Id, input.QuestionId, input.TextAnswer ?? "");

            _db.SurveyAnswers.Add(answer);
        }

        // Ortalama skoru hesapla
        var scoredAnswers = request.Answers.Where(a => a.Score.HasValue).ToList();
        var averageScore  = scoredAnswers.Any()
            ? (decimal)scoredAnswers.Average(a => a.Score!.Value)
            : 0m;

        // NPS sorusu: 0-10 aralığında skor (varsa)
        var npsAnswer = request.Answers.FirstOrDefault(a =>
            a.Score.HasValue && a.Score.Value >= 0 && a.Score.Value <= 10);
        var npsScore = npsAnswer?.Score;

        response.Complete(averageScore, npsScore);
        await _db.SaveChangesAsync(cancellationToken);

        // Düşük puan eşiği kontrolü → otomatik şikayet
        if (averageScore > 0 && averageScore <= 3.0m)
        {
            await _mediator.Send(new CreateComplaintCommand(
                CompanyId:        response.CompanyId,
                BranchId:         response.BranchId,
                PatientId:        response.PatientId,
                Source:           ComplaintSource.Survey,
                Subject:          $"Düşük Anket Puanı — {averageScore:F1}/5",
                Description:      request.Answers.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.TextAnswer))
                                      ?.TextAnswer ?? "Anket üzerinden düşük puan verildi.",
                Priority:         ComplaintPriority.Normal,
                CreatedBy:        0,   // sistem oluşturdu
                SurveyResponseId: response.Id),
                cancellationToken);
        }

        return true;
    }
}
