using MediatR;
using Oravity.SharedKernel.Events;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Infrastructure.Messaging;

public class MediatREventBus : IEventBus
{
    private readonly IPublisher _publisher;

    public MediatREventBus(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishAsync<T>(T domainEvent, CancellationToken ct = default) where T : IDomainEvent
        => await _publisher.Publish(domainEvent, ct);
}
