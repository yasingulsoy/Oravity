using Oravity.SharedKernel.Events;

namespace Oravity.SharedKernel.Interfaces;

public interface IEventBus
{
    Task PublishAsync<T>(T domainEvent, CancellationToken ct = default) where T : IDomainEvent;
}
