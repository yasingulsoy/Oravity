using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public class Company : BaseEntity
{
    public string Name { get; private set; } = default!;
    public long VerticalId { get; private set; }
    public string DefaultLanguageCode { get; private set; } = "tr";
    public bool IsActive { get; private set; } = true;
    public DateTime? SubscriptionEndsAt { get; private set; }

    public Vertical Vertical { get; private set; } = default!;
    public ICollection<Branch> Branches { get; private set; } = [];
    public ICollection<UserRoleAssignment> UserRoleAssignments { get; private set; } = [];

    private Company() { }

    public static Company Create(string name, long verticalId, string defaultLanguageCode = "tr")
    {
        return new Company
        {
            Name = name,
            VerticalId = verticalId,
            DefaultLanguageCode = defaultLanguageCode,
            IsActive = true
        };
    }

    public void SetSubscription(DateTime endsAt) => SubscriptionEndsAt = endsAt;
    public void SetActive(bool value) => IsActive = value;
    public void SetLanguage(string code) => DefaultLanguageCode = code;
}
