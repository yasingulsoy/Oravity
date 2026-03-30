using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Oravity.Core.Modules.Appointment.Infrastructure.Hubs;

/// <summary>
/// Real-time takvim hub'ı (SPEC §REAL-TIME TAKVİM — BÖLÜM 1).
/// Client'lar şube grubuna katılarak aynı anda aktif kullanıcıları ve slot kilitleri görür.
/// </summary>
[Authorize]
public class CalendarHub : Hub
{
    /// <summary>Takvimi açan kullanıcı şube grubuna katılır.</summary>
    public async Task JoinCalendar(long branchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(branchId));

        await Clients.Group(GroupName(branchId))
            .SendAsync("UserJoined", new
            {
                UserId   = GetUserId(),
                UserName = GetUserName(),
                JoinedAt = DateTime.UtcNow
            });
    }

    /// <summary>Takvimi kapatan kullanıcı gruptan ayrılır.</summary>
    public async Task LeaveCalendar(long branchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(branchId));

        await Clients.Group(GroupName(branchId))
            .SendAsync("UserLeft", new { UserId = GetUserId() });
    }

    /// <summary>
    /// Kullanıcı bir slota tıkladığında diğerlerine bildirir.
    /// Diğer kullanıcılarda slot "düzenleniyor" (sarı) gösterilir.
    /// </summary>
    public async Task SlotFocused(long branchId, long doctorId, DateTime slotTime)
    {
        await Clients.OthersInGroup(GroupName(branchId))
            .SendAsync("SlotBeingEdited", new
            {
                DoctorId  = doctorId,
                SlotTime  = slotTime,
                EditingBy = GetUserName()
            });
    }

    /// <summary>Kullanıcı slottan vazgeçti — kilit kalkar.</summary>
    public async Task SlotReleased(long branchId, long doctorId, DateTime slotTime)
    {
        await Clients.OthersInGroup(GroupName(branchId))
            .SendAsync("SlotReleased", new
            {
                DoctorId = doctorId,
                SlotTime = slotTime
            });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Bağlantı koptu — tüm odaklanan slotlar otomatik serbest bırakılır
        await Clients.All.SendAsync("UserDisconnected", new { UserId = GetUserId() });
        await base.OnDisconnectedAsync(exception);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────
    private static string GroupName(long branchId) => $"calendar_{branchId}";

    private long GetUserId()
    {
        var claim = Context.User?.FindFirstValue("user_id")
                    ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(claim, out var id) ? id : 0;
    }

    private string GetUserName()
        => Context.User?.FindFirstValue("full_name")
           ?? Context.User?.FindFirstValue(ClaimTypes.Name)
           ?? "Bilinmiyor";
}
