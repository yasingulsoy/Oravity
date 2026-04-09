using Oravity.SharedKernel.Entities;
using AppointmentEntity = Oravity.SharedKernel.Entities.Appointment;

namespace Oravity.Core.Modules.Appointment.Application;

// ─── Response DTO'lar ──────────────────────────────────────────────────────

public record AppointmentResponse(
    Guid PublicId,
    long BranchId,
    long? PatientId,
    string? PatientName,
    long DoctorId,
    string? DoctorName,
    DateTime StartTime,
    DateTime EndTime,
    int StatusId,
    string StatusLabel,
    string? Notes,
    bool IsUrgent,
    bool IsEarlierRequest,
    int RowVersion,
    DateTime CreatedAt,
    string? AppointmentTypeName = null
);

/// <summary>SignalR broadcast için hafif DTO</summary>
public record AppointmentBroadcastDto(
    Guid PublicId,
    long BranchId,
    long? PatientId,
    long DoctorId,
    DateTime StartTime,
    DateTime EndTime,
    int StatusId,
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
    SlotReleased,

    // Vizite & Protokol (SPEC §VİZİTE & PROTOKOL MİMARİSİ §6)
    VisitCheckedIn,
    VisitCheckedOut,
    ProtocolCreated,
    ProtocolCompleted,
    ProtocolUpdated,
}

// ─── Broadcast service arayüzü ────────────────────────────────────────────

public record VisitBroadcastDto(
    Guid   PublicId,
    long   BranchId,
    long   PatientId,
    string PatientName,
    bool   IsWalkIn,
    int    Status
);

public record ProtocolBroadcastDto(
    Guid   PublicId,
    long   BranchId,
    long   VisitId,
    long   PatientId,
    string PatientName,
    long   DoctorId,
    string DoctorName,
    string ProtocolNo,
    int    ProtocolType,
    int    Status
);

public interface ICalendarBroadcastService
{
    Task BroadcastAsync(
        long branchId,
        AppointmentBroadcastDto appointment,
        CalendarEventType eventType,
        CancellationToken ct = default);

    Task BroadcastVisitAsync(
        long branchId,
        VisitBroadcastDto visit,
        CalendarEventType eventType,
        CancellationToken ct = default);

    Task BroadcastProtocolAsync(
        long branchId,
        ProtocolBroadcastDto protocol,
        CalendarEventType eventType,
        CancellationToken ct = default);
}

// ─── Mapping ──────────────────────────────────────────────────────────────

public static class AppointmentMappings
{
    public static AppointmentResponse ToResponse(AppointmentEntity a) => new(
        a.PublicId, a.BranchId, a.PatientId,
        a.Patient is not null ? $"{a.Patient.FirstName} {a.Patient.LastName}" : null,
        a.DoctorId,
        a.Doctor?.FullName,
        a.StartTime, a.EndTime, a.StatusId, StatusLabel(a.StatusId),
        a.Notes, a.IsUrgent, a.IsEarlierRequest, a.RowVersion, a.CreatedAt,
        a.AppointmentType?.Name);

    public static AppointmentBroadcastDto ToBroadcast(AppointmentEntity a) => new(
        a.PublicId, a.BranchId, a.PatientId, a.DoctorId,
        a.StartTime, a.EndTime, a.StatusId, StatusLabel(a.StatusId), a.RowVersion);

    public static string StatusLabel(int statusId) => statusId switch
    {
        AppointmentStatus.WellKnownIds.Planned   => "Planlandı",
        AppointmentStatus.WellKnownIds.Confirmed => "Onaylandı",
        AppointmentStatus.WellKnownIds.Arrived   => "Geldi",
        AppointmentStatus.WellKnownIds.InRoom    => "Odaya Alındı",
        AppointmentStatus.WellKnownIds.Left      => "Ayrıldı",
        AppointmentStatus.WellKnownIds.Cancelled => "İptal",
        AppointmentStatus.WellKnownIds.Completed => "Tamamlandı",
        AppointmentStatus.WellKnownIds.NoShow    => "Gelmedi",
        _ => statusId.ToString()
    };

    public static CalendarEventType StatusToEventType(int statusId) => statusId switch
    {
        AppointmentStatus.WellKnownIds.Arrived   => CalendarEventType.PatientArrived,
        AppointmentStatus.WellKnownIds.InRoom    => CalendarEventType.PatientInRoom,
        AppointmentStatus.WellKnownIds.Completed => CalendarEventType.Completed,
        AppointmentStatus.WellKnownIds.Cancelled => CalendarEventType.Cancelled,
        AppointmentStatus.WellKnownIds.NoShow    => CalendarEventType.NoShow,
        _ => CalendarEventType.StatusChanged
    };
}
