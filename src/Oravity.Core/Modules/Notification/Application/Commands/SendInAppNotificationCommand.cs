using MediatR;
using Oravity.Core.Modules.Notification.Application;
using Oravity.Core.Modules.Notification.Infrastructure.Services;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using NotifEntity = Oravity.SharedKernel.Entities.Notification;

namespace Oravity.Core.Modules.Notification.Application.Commands;

public record SendInAppNotificationCommand(
    long BranchId,
    NotificationType Type,
    string Title,
    string Message,
    long? ToUserId = null,
    int? ToRole = null,
    long? CompanyId = null,
    bool IsUrgent = false,
    string? RelatedEntityType = null,
    long? RelatedEntityId = null
) : IRequest<NotificationResponse>;

public class SendInAppNotificationCommandHandler
    : IRequestHandler<SendInAppNotificationCommand, NotificationResponse>
{
    private readonly AppDbContext _db;
    private readonly INotificationHubService _hub;
    private readonly ITenantContext _tenant;

    public SendInAppNotificationCommandHandler(
        AppDbContext db,
        INotificationHubService hub,
        ITenantContext tenant)
    {
        _db = db;
        _hub = hub;
        _tenant = tenant;
    }

    public async Task<NotificationResponse> Handle(
        SendInAppNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var notification = NotifEntity.Create(
            branchId:          request.BranchId,
            type:              request.Type,
            title:             request.Title,
            message:           request.Message,
            toUserId:          request.ToUserId,
            toRole:            request.ToRole,
            companyId:         request.CompanyId,
            isUrgent:          request.IsUrgent,
            relatedEntityType: request.RelatedEntityType,
            relatedEntityId:   request.RelatedEntityId);

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(cancellationToken);

        // SignalR push
        var payload = new NotificationPayload(
            notification.PublicId,
            notification.Type,
            NotificationMappings.TypeLabel(notification.Type),
            notification.Title,
            notification.Message,
            notification.IsUrgent,
            notification.RelatedEntityType,
            notification.RelatedEntityId,
            notification.CreatedAt);

        if (request.ToUserId.HasValue)
        {
            // Belirli kullanıcıya
            await _hub.SendToUser(request.ToUserId.Value, payload, cancellationToken);
        }
        else
        {
            // Şube geneline (rol filtresi frontend'de yapılır)
            await _hub.SendToBranch(request.BranchId, payload, cancellationToken);
        }

        return NotificationMappings.ToResponse(notification);
    }
}
