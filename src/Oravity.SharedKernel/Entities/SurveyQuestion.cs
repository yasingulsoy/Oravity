namespace Oravity.SharedKernel.Entities;

public enum QuestionType
{
    Star           = 1,  // Yıldız (1-5)
    YesNo          = 2,  // Evet/Hayır
    MultipleChoice = 3,  // Çoktan Seçmeli
    Text           = 4   // Metin
}

/// <summary>
/// Anket sorusu — şablona bağlı, sıralı.
/// options JSONB: çoktan seçmeli sorular için seçenek listesi.
/// </summary>
public class SurveyQuestion
{
    public long Id { get; private set; }

    public long TemplateId { get; private set; }
    public SurveyTemplate Template { get; private set; } = default!;

    public int SortOrder { get; private set; }
    public string QuestionText { get; private set; } = default!;
    public QuestionType QuestionType { get; private set; }

    /// <summary>JSON: ["Çok İyi","İyi","Orta","Kötü"] — MultipleChoice için.</summary>
    public string? Options { get; private set; }

    public bool IsRequired { get; private set; } = true;

    private SurveyQuestion() { }

    public static SurveyQuestion Create(
        long templateId, string questionText, QuestionType questionType,
        int sortOrder = 0, string? options = null, bool isRequired = true)
    {
        return new SurveyQuestion
        {
            TemplateId   = templateId,
            QuestionText = questionText,
            QuestionType = questionType,
            SortOrder    = sortOrder,
            Options      = options,
            IsRequired   = isRequired
        };
    }
}
