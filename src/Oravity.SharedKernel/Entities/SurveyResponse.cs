using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum SurveyResponseStatus
{
    Sent      = 1,  // Gönderildi
    Completed = 2,  // Tamamlandı
    Expired   = 3   // Süresi Doldu
}

public enum SurveyChannel
{
    Sms       = 1,
    Email     = 2,
    WhatsApp  = 3,
    Portal    = 4
}

/// <summary>
/// Belirli bir hastaya gönderilen anket örneği.
/// token ile anonim erişim sağlanır — UNIQUE.
/// nps_score: 0-10 NPS hesaplaması için; average_score: 1-5 ortalama.
/// </summary>
public class SurveyResponse : BaseEntity
{
    public long TemplateId { get; private set; }
    public SurveyTemplate Template { get; private set; } = default!;

    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    public long? AppointmentId { get; private set; }
    public Appointment? Appointment { get; private set; }

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public long CompanyId { get; private set; }
    public Company Company { get; private set; } = default!;

    public SurveyResponseStatus Status { get; private set; } = SurveyResponseStatus.Sent;

    /// <summary>UUID tabanlı güvenli tek kullanımlık erişim tokeni.</summary>
    public string Token { get; private set; } = default!;
    public DateTime TokenExpiresAt { get; private set; }

    public DateTime SentAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public SurveyChannel Channel { get; private set; }

    /// <summary>NPS sorusu skoru (0-10); yoksa null.</summary>
    public int? NpsScore { get; private set; }

    /// <summary>1-5 arası ortalama skor (submit sonrası hesaplanır).</summary>
    public decimal? AverageScore { get; private set; }

    public ICollection<SurveyAnswer> Answers { get; private set; } = [];

    private SurveyResponse() { }

    public static SurveyResponse Create(
        long templateId, long patientId, long branchId, long companyId,
        SurveyChannel channel, long? appointmentId = null,
        int tokenExpiryHours = 72)
    {
        return new SurveyResponse
        {
            TemplateId     = templateId,
            PatientId      = patientId,
            AppointmentId  = appointmentId,
            BranchId       = branchId,
            CompanyId      = companyId,
            Status         = SurveyResponseStatus.Sent,
            Token          = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            TokenExpiresAt = DateTime.UtcNow.AddHours(tokenExpiryHours),
            SentAt         = DateTime.UtcNow,
            Channel        = channel
        };
    }

    public void Complete(decimal averageScore, int? npsScore)
    {
        Status       = SurveyResponseStatus.Completed;
        AverageScore = averageScore;
        NpsScore     = npsScore;
        CompletedAt  = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Expire()
    {
        Status = SurveyResponseStatus.Expired;
        MarkUpdated();
    }

    public bool IsTokenValid() =>
        Status == SurveyResponseStatus.Sent && TokenExpiresAt > DateTime.UtcNow;
}
