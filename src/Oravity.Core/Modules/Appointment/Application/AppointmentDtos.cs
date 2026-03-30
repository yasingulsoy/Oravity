using Oravity.SharedKernel.Entities;
using AppointmentEntity = Oravity.SharedKernel.Entities.Appointment;

namespace Oravity.Core.Modules.Appointment.Application;

// ─── Response DTO'lar ──────────────────────────────────────────────────────

public record AppointmentResponse(
    Guid PublicId,
    long BranchId,
    long PatientId,
    long DoctorId,
    DateTime StartTime,
    DateTime EndTime,
    AppointmentStatus Status,
    string StatusLabel,
    string? Notes,
    int RowVersion,
    DateTime CreatedAt
);

/// <summary>SignalR broadcast için hafif DTO</summary>
public record AppointmentBroadcastDto(
    Guid PublicId,
    long BranchId,
    long PatientId,
    long DoctorId,
    DateTime StartTime,
    DateTime EndTime,
    AppointmentStatus Status,
    string StatusLabel,
    int RowVersion
);

public record TimeSlotDto(DateTime Start, DateTime End, bool IsAvailable);

// ─── CalendarEventType enum (SPEC §TAKVİM REAL-TIME BÖLÜM 2) ──────────────

public enum CalendarEventType
{
    // Randevu yaşam döngüsü
    Created,
    Updated,
    Cancelled,
    Deleted,

    // Durum geçişleri
    PatientArrived,
    PatientInRoom,
    TreatmentStarted,
    PatientLeaving,
    Completed,
    NoShow,

    // Değişiklikler
    Moved,
    DoctorChanged,
    DurationChanged,
    NoteAdded,
    StatusChanged,

    // Slot yönetimi
    SlotBeingEdited,
    SlotReleased
}

// ─── Broadcast service arayüzü ────────────────────────────────────────────

public interface ICalendarBroadcastService
{
    Task BroadcastAsync(
        long branchId,
        AppointmentBroadcastDto appointment,
        CalendarEventType eventType,
        CancellationToken ct = default);
}

// ─── Mapping ──────────────────────────────────────────────────────────────

public static class AppointmentMappings
{
    public static AppointmentResponse ToResponse(AppointmentEntity a) => new(
        a.PublicId, a.BranchId, a.PatientId, a.DoctorId,
        a.StartTime, a.EndTime, a.Status, StatusLabel(a.Status),
        a.Notes, a.RowVersion, a.CreatedAt);

    public static AppointmentBroadcastDto ToBroadcast(AppointmentEntity a) => new(
        a.PublicId, a.BranchId, a.PatientId, a.DoctorId,
        a.StartTime, a.EndTime, a.Status, StatusLabel(a.Status), a.RowVersion);

    public static string StatusLabel(AppointmentStatus s) => s switch
    {
        AppointmentStatus.Planned   => "Planlandı",
        AppointmentStatus.Confirmed => "Onaylandı",
        AppointmentStatus.Arrived   => "Geldi",
        AppointmentStatus.InRoom    => "Odaya Alındı",
        AppointmentStatus.Completed => "Tamamlandı",
        AppointmentStatus.Cancelled => "İptal",
        AppointmentStatus.NoShow    => "Gelmedi",
        _ => s.ToString()
    };

    public static CalendarEventType StatusToEventType(AppointmentStatus s) => s switch
    {
        AppointmentStatus.Arrived   => CalendarEventType.PatientArrived,
        AppointmentStatus.InRoom    => CalendarEventType.PatientInRoom,
        AppointmentStatus.Completed => CalendarEventType.Completed,
        AppointmentStatus.Cancelled => CalendarEventType.Cancelled,
        AppointmentStatus.NoShow    => CalendarEventType.NoShow,
        _ => CalendarEventType.StatusChanged
    };
}
