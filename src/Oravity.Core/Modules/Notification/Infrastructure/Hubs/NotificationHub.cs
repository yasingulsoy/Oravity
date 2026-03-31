using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Oravity.Core.Modules.Notification.Infrastructure.Hubs;

/// <summary>
/// Klinik içi bildirim hub'ı.
/// Kullanıcılar kişisel grubu ve şube grubunu dinler.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    /// <summary>Kullanıcının kişisel bildirim grubuna katılır: "user_{userId}"</summary>
    public async Task JoinUserGroup(long userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
    }

    /// <summary>Şube geneli bildirimler için gruba katılır: "branch_{branchId}"</summary>
    public async Task JoinBranchGroup(long branchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, BranchGroup(branchId));
    }

    public override async Task OnConnectedAsync()
    {
        // JWT'den otomatik kişisel gruba katıl
        var userId = GetUserId();
        if (userId > 0)
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

        await base.OnConnectedAsync();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────
    public static string UserGroup(long userId)     => $"user_{userId}";
    public static string BranchGroup(long branchId) => $"branch_{branchId}";

    private long GetUserId()
    {
        var claim = Context.User?.FindFirstValue("user_id")
                    ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(claim, out var id) ? id : 0;
    }
}
