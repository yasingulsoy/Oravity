using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Notification.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Notification.Application.Queries;

public record GetMyNotificationsQuery(
    int Page = 1,
    int PageSize = 30,
    bool? UnreadOnly = null
) : IRequest<PagedNotificationResult>;

public class GetMyNotificationsQueryHandler
    : IRequestHandler<GetMyNotificationsQuery, PagedNotificationResult>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly ITenantContext _tenant;

    public GetMyNotificationsQueryHandler(
        AppDbContext db, ICurrentUser user, ITenantContext tenant)
    {
        _db = db;
        _user = user;
        _tenant = tenant;
    }

    public async Task<PagedNotificationResult> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _user.UserId;
        var branchId = _tenant.BranchId;

        // Kullanıcıya özel + şube geneli (rol bazlı) bildirimler
        var query = _db.Notifications
            .AsNoTracking()
            .Where(n =>
                n.ToUserId == userId ||
                (branchId.HasValue && n.BranchId == branchId.Value && n.ToUserId == null));

        if (request.UnreadOnly == true)
            query = query.Where(n => !n.IsRead);

        var totalCount = await query.CountAsync(cancellationToken);
        var unreadCount = await query.CountAsync(n => !n.IsRead, cancellationToken);

        // Okunmamışlar önce, sonra oluşturma tarihine göre azalan sıra
        var items = await query
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => NotificationMappings.ToResponse(n))
            .ToListAsync(cancellationToken);

        return new PagedNotificationResult(
            items, totalCount, unreadCount, request.Page, request.PageSize);
    }
}
