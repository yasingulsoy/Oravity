using Microsoft.AspNetCore.SignalR;
using Oravity.Core.Modules.Notification.Infrastructure.Hubs;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Notification.Infrastructure.Services;

/// <summary>
/// Merkezi SignalR bildirim servisi.
/// Command handler'ları bu servis üzerinden push gönderir.
/// </summary>
public class NotificationHubService : INotificationHubService
{
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationHubService(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    /// <summary>Belirli bir kullanıcıya bildirim gönder.</summary>
    public async Task SendToUser(
        long userId,
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        await _hub.Clients
            .Group(NotificationHub.UserGroup(userId))
            .SendAsync("NewNotification", payload, ct);
    }

    /// <summary>Şubedeki tüm bağlı kullanıcılara bildirim gönder.</summary>
    public async Task SendToBranch(
        long branchId,
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        await _hub.Clients
            .Group(NotificationHub.BranchGroup(branchId))
            .SendAsync("NewNotification", payload, ct);
    }
}

// ─── Interface ────────────────────────────────────────────────────────────

public interface INotificationHubService
{
    Task SendToUser(long userId, NotificationPayload payload, CancellationToken ct = default);
    Task SendToBranch(long branchId, NotificationPayload payload, CancellationToken ct = default);
}

// ─── Payload DTO ──────────────────────────────────────────────────────────

public record NotificationPayload(
    Guid PublicId,
    NotificationType Type,
    string TypeLabel,
    string Title,
    string Message,
    bool IsUrgent,
    string? RelatedEntityType,
    long? RelatedEntityId,
    DateTime CreatedAt
);
