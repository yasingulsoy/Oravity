namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Anket sorusuna verilen tek yanıt.
/// Soru tipine göre answer_text / answer_score / answer_boolean / selected_option dolar.
/// </summary>
public class SurveyAnswer
{
    public long Id { get; private set; }

    public long ResponseId { get; private set; }
    public SurveyResponse Response { get; private set; } = default!;

    public long QuestionId { get; private set; }
    public SurveyQuestion Question { get; private set; } = default!;

    /// <summary>Açık metin yanıtı (Text tipi veya düşük puan açıklaması).</summary>
    public string? AnswerText { get; private set; }

    /// <summary>Yıldız puanı 1-5 (Star tipi).</summary>
    public int? AnswerScore { get; private set; }

    /// <summary>Evet/Hayır (YesNo tipi).</summary>
    public bool? AnswerBoolean { get; private set; }

    /// <summary>Seçilen seçenek etiketi (MultipleChoice tipi).</summary>
    public string? SelectedOption { get; private set; }

    private SurveyAnswer() { }

    public static SurveyAnswer CreateStarAnswer(
        long responseId, long questionId, int score) =>
        new() { ResponseId = responseId, QuestionId = questionId, AnswerScore = score };

    public static SurveyAnswer CreateYesNoAnswer(
        long responseId, long questionId, bool answer) =>
        new() { ResponseId = responseId, QuestionId = questionId, AnswerBoolean = answer };

    public static SurveyAnswer CreateTextAnswer(
        long responseId, long questionId, string text) =>
        new() { ResponseId = responseId, QuestionId = questionId, AnswerText = text };

    public static SurveyAnswer CreateMultipleChoiceAnswer(
        long responseId, long questionId, string selectedOption) =>
        new() { ResponseId = responseId, QuestionId = questionId, SelectedOption = selectedOption };
}
