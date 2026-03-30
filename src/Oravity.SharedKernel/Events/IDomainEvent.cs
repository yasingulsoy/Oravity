using MediatR;

namespace Oravity.SharedKernel.Events;

public interface IDomainEvent : INotification
{
    DateTime OccurredAt { get; }
}
