using Oravity.SharedKernel.Events;

namespace Oravity.SharedKernel.BaseEntities;

public abstract class BaseEntity
{
    public long Id { get; protected set; }
    public Guid PublicId { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public bool IsDeleted { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents()
        => _domainEvents.Clear();

    public void MarkUpdated()
        => UpdatedAt = DateTime.UtcNow;

    public void SoftDelete()
        => IsDeleted = true;
}
