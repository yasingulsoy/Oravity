using Microsoft.AspNetCore.SignalR;
using Oravity.Core.Modules.Appointment.Application;

namespace Oravity.Core.Modules.Appointment.Infrastructure.Hubs;

/// <summary>
/// Merkezi broadcast servisi (SPEC §TAKVİM REAL-TIME BÖLÜM 2).
/// Randevu, vizite ve protokol değişikliklerini SignalR üzerinden yayınlar.
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

    public async Task BroadcastVisitAsync(
        long branchId,
        VisitBroadcastDto visit,
        CalendarEventType eventType,
        CancellationToken ct = default)
    {
        await _hub.Clients
            .Group($"calendar_{branchId}")
            .SendAsync("VisitUpdated", new
            {
                EventType = eventType.ToString(),
                Visit     = visit,
                Timestamp = DateTime.UtcNow
            }, ct);
    }

    public async Task BroadcastProtocolAsync(
        long branchId,
        ProtocolBroadcastDto protocol,
        CalendarEventType eventType,
        CancellationToken ct = default)
    {
        await _hub.Clients
            .Group($"calendar_{branchId}")
            .SendAsync("ProtocolUpdated", new
            {
                EventType = eventType.ToString(),
                Protocol  = protocol,
                Timestamp = DateTime.UtcNow
            }, ct);
    }

    public async Task BroadcastPatientCalledAsync(
        long branchId,
        string patientName,
        string doctorName,
        CancellationToken ct = default)
    {
        await _hub.Clients
            .Group($"calendar_{branchId}")
            .SendAsync("PatientCalled", new
            {
                PatientName = patientName,
                DoctorName  = doctorName,
                Timestamp   = DateTime.UtcNow
            }, ct);
    }
}
