using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum SurveyTriggerType
{
    PostAppointment = 1,  // Randevu Sonrası
    PostTreatment   = 2,  // Tedavi Sonrası
    Manual          = 3,  // Manuel
    Periodic        = 4   // Periyodik
}

/// <summary>
/// Anket şablonu (SPEC §HASTA ANKETLERİ VE ŞİKAYET YÖNETİMİ §2.1).
/// Her şirket kendi anket şablonlarını tanımlar.
/// trigger_delay_hours: tetikleyiciden kaç saat sonra gönderilsin.
/// </summary>
public class SurveyTemplate : BaseEntity
{
    public long CompanyId { get; private set; }
    public Company Company { get; private set; } = default!;

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    public SurveyTriggerType TriggerType { get; private set; }
    public int TriggerDelayHours { get; private set; } = 24;

    public bool IsActive { get; private set; } = true;

    public long CreatedBy { get; private set; }
    public User Creator { get; private set; } = default!;

    public ICollection<SurveyQuestion> Questions { get; private set; } = [];

    private SurveyTemplate() { }

    public static SurveyTemplate Create(
        long companyId, string name, SurveyTriggerType triggerType,
        long createdBy, string? description = null, int triggerDelayHours = 24)
    {
        return new SurveyTemplate
        {
            CompanyId         = companyId,
            Name              = name,
            Description       = description,
            TriggerType       = triggerType,
            TriggerDelayHours = triggerDelayHours,
            CreatedBy         = createdBy
        };
    }

    public void Update(string name, string? description, SurveyTriggerType triggerType,
        int triggerDelayHours, bool isActive)
    {
        Name              = name;
        Description       = description;
        TriggerType       = triggerType;
        TriggerDelayHours = triggerDelayHours;
        IsActive          = isActive;
        MarkUpdated();
    }
}
