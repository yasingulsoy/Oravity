using Microsoft.AspNetCore.SignalR;
using Oravity.Core.Modules.Appointment.Application;

namespace Oravity.Core.Modules.Appointment.Infrastructure.Hubs;

/// <summary>
/// Merkezi broadcast servisi (SPEC §TAKVİM REAL-TIME BÖLÜM 2).
/// Tüm randevu handler'ları bu servis üzerinden SignalR mesajı gönderir.
/// CalendarUpdated event'i — frontend tek handler ile tüm tipleri karşılar.
/// </summary>
public class CalendarBroadcastService : ICalendarBroadcastService
{
    private readonly IHubContext<CalendarHub> _hub;

    public CalendarBroadcastService(IHubContext<CalendarHub> hub)
    {
        _hub = hub;
    }

    public async Task BroadcastAsync(
        long branchId,
        AppointmentBroadcastDto appointment,
        CalendarEventType eventType,
        CancellationToken ct = default)
    {
        await _hub.Clients
            .Group($"calendar_{branchId}")
            .SendAsync("CalendarUpdated", new
            {
                EventType   = eventType.ToString(),
                Appointment = appointment,
                Timestamp   = DateTime.UtcNow
            }, ct);
    }
}
