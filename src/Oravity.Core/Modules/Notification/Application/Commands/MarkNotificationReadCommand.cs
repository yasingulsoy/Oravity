using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Notification.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Notification.Application.Commands;

public record MarkNotificationReadCommand(Guid PublicId) : IRequest<NotificationResponse>;

public class MarkNotificationReadCommandHandler
    : IRequestHandler<MarkNotificationReadCommand, NotificationResponse>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public MarkNotificationReadCommandHandler(AppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<NotificationResponse> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.PublicId == request.PublicId, cancellationToken)
            ?? throw new NotFoundException($"Bildirim bulunamadı: {request.PublicId}");

        // Sadece kendine gelen bildirim okunabilir (platform admin hariç)
        if (!_user.HasPermission("platform_admin") &&
            notification.ToUserId.HasValue &&
            notification.ToUserId != _user.UserId)
        {
            throw new ForbiddenException("Bu bildirimi okuma yetkiniz bulunmuyor.");
        }

        notification.MarkRead();
        await _db.SaveChangesAsync(cancellationToken);

        return NotificationMappings.ToResponse(notification);
    }
}
