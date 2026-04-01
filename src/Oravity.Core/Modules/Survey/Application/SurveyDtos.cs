using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Survey.Application;

public record SurveyTemplateResponse(
    Guid PublicId,
    string Name,
    string? Description,
    SurveyTriggerType TriggerType,
    int TriggerDelayHours,
    bool IsActive,
    int QuestionCount
);

public record SurveyQuestionResponse(
    long Id,
    int SortOrder,
    string QuestionText,
    QuestionType QuestionType,
    string? Options,
    bool IsRequired
);

public record PublicSurveyResponse(
    Guid PublicId,
    string TemplateName,
    SurveyResponseStatus Status,
    IReadOnlyList<SurveyQuestionResponse> Questions
);

public record SurveyResultsResponse(
    long TemplateId,
    string TemplateName,
    int TotalSent,
    int TotalCompleted,
    decimal CompletionRate,
    decimal? AverageScore,
    double? NpsScore,
    IReadOnlyList<QuestionResultItem> QuestionResults
);

public record QuestionResultItem(
    long QuestionId,
    string QuestionText,
    QuestionType Type,
    decimal? AverageScore,
    IDictionary<string, int> Distribution
);

public record ComplaintResponse(
    Guid PublicId,
    long CompanyId,
    long BranchId,
    long? PatientId,
    ComplaintSource Source,
    string Subject,
    string Description,
    ComplaintStatus Status,
    string StatusLabel,
    ComplaintPriority Priority,
    string PriorityLabel,
    long? AssignedTo,
    string? Resolution,
    DateTime? ResolvedAt,
    DateTime? SlaDueAt,
    bool IsSlaBreached,
    DateTime CreatedAt
);

public static class SurveyMappings
{
    public static SurveyTemplateResponse ToResponse(SurveyTemplate t) => new(
        t.PublicId, t.Name, t.Description, t.TriggerType,
        t.TriggerDelayHours, t.IsActive, t.Questions.Count);

    public static SurveyQuestionResponse ToResponse(SurveyQuestion q) => new(
        q.Id, q.SortOrder, q.QuestionText, q.QuestionType, q.Options, q.IsRequired);

    public static ComplaintResponse ToResponse(Complaint c) => new(
        c.PublicId, c.CompanyId, c.BranchId, c.PatientId,
        c.Source, c.Subject, c.Description,
        c.Status, StatusLabel(c.Status),
        c.Priority, PriorityLabel(c.Priority),
        c.AssignedTo, c.Resolution, c.ResolvedAt, c.SlaDueAt,
        c.SlaDueAt.HasValue && c.SlaDueAt < DateTime.UtcNow &&
            c.Status < ComplaintStatus.Resolved,
        c.CreatedAt);

    public static string StatusLabel(ComplaintStatus s) => s switch
    {
        ComplaintStatus.New        => "Yeni",
        ComplaintStatus.InProgress => "İnceleniyor",
        ComplaintStatus.Resolved   => "Çözüldü",
        ComplaintStatus.Closed     => "Kapatıldı",
        ComplaintStatus.Escalated  => "Eskalasyonda",
        _                          => s.ToString()
    };

    public static string PriorityLabel(ComplaintPriority p) => p switch
    {
        ComplaintPriority.Low    => "Düşük",
        ComplaintPriority.Normal => "Normal",
        ComplaintPriority.High   => "Yüksek",
        ComplaintPriority.Urgent => "Acil",
        _                        => p.ToString()
    };
}
