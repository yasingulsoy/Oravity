using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Randevu kaydı.
/// Durum geçişleri: AppointmentStatus.AllowedNextStatusIds JSON alanından okunur (uygulama katmanı).
/// Terminal durumlar: CANCELLED, NO_SHOW, LEFT — bu durumlarda randevu taşınamaz / iptal edilemez.
/// Slot çakışması: SPEC §2.1 — unique partial index (doctor_id, branch_id, start_time, status_id NOT IN terminal)
/// Optimistic lock: RowVersion — MoveAppointment handler versiyonu kontrol eder.
/// </summary>
public class Appointment : AuditableEntity
{
    // ─── Terminal durum kodları (taşıma / iptal kontrolü) ─────────────────
    public static readonly string[] TerminalCodes = ["CANCELLED", "NO_SHOW", "LEFT"];

    // ─── İlişkiler ─────────────────────────────────────────────────────────
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public long? PatientId { get; private set; }
    public Patient? Patient { get; private set; }

    public long DoctorId { get; private set; }
    public User Doctor { get; private set; } = default!;

    public int StatusId { get; private set; }
    public AppointmentStatus Status { get; private set; } = default!;

    public int? AppointmentTypeId { get; private set; }
    public AppointmentType? AppointmentType { get; private set; }

    public int? SpecializationId { get; private set; }
    public Specialization? Specialization { get; private set; }

    // ─── Zaman ────────────────────────────────────────────────────────────
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }

    // ─── Zaman izleme (hasta akışı) ───────────────────────────────────────
    public DateTime? PatientArrivedAt { get; private set; }
    public DateTime? EnteredRoomAt { get; private set; }
    public DateTime? LeftRoomAt { get; private set; }
    public DateTime? LeftClinicAt { get; private set; }

    // ─── Detaylar ─────────────────────────────────────────────────────────
    /// <summary>APT-2025-0001 formatında otomatik üretilir</summary>
    public string? AppointmentNo { get; private set; }

    /// <summary>online | phone | walk_in | manual</summary>
    public string BookingSource { get; private set; } = "manual";

    public string? Notes { get; private set; }
    public bool IsUrgent { get; private set; }
    public bool IsEarlierRequest { get; private set; }
    public bool IsNewPatient { get; private set; }
    public bool SendSmsNotification { get; private set; } = true;

    /// <summary>
    /// Optimistic lock sayacı.
    /// EF Core concurrency token olarak yapılandırılır.
    /// </summary>
    public int RowVersion { get; private set; } = 1;

    private Appointment() { }

    public static Appointment Create(
        long branchId,
        long? patientId,
        long doctorId,
        int statusId,
        DateTime startTime,
        DateTime endTime,
        int? appointmentTypeId = null,
        int? specializationId = null,
        string bookingSource = "manual",
        string? notes = null,
        bool isUrgent = false,
        bool isEarlierRequest = false,
        bool isNewPatient = false)
    {
        if (endTime <= startTime)
            throw new ArgumentException("Bitiş zamanı başlangıç zamanından sonra olmalıdır.");

        return new Appointment
        {
            BranchId            = branchId,
            PatientId           = patientId,
            DoctorId            = doctorId,
            StatusId            = statusId,
            StartTime           = startTime.ToUniversalTime(),
            EndTime             = endTime.ToUniversalTime(),
            AppointmentTypeId   = appointmentTypeId,
            SpecializationId    = specializationId,
            BookingSource       = bookingSource,
            Notes               = notes,
            IsUrgent            = isUrgent,
            IsEarlierRequest    = isEarlierRequest,
            IsNewPatient        = isNewPatient,
            SendSmsNotification = true,
            RowVersion          = 1
        };
    }

    /// <summary>
    /// Durumu günceller. Geçiş validasyonu uygulama katmanında yapılır
    /// (AppointmentStatus.AllowedNextStatusIds JSON'u okunarak).
    /// </summary>
    public void SetStatus(int newStatusId)
    {
        StatusId = newStatusId;
        IncrementRowVersion();
        MarkUpdated();
    }

    /// <summary>Randevuyu yeni zaman dilimine taşır. Terminal durumlarda çağrılmamalı.</summary>
    public void MoveTo(DateTime newStart, DateTime newEnd, long? newDoctorId = null)
    {
        if (newEnd <= newStart)
            throw new ArgumentException("Bitiş zamanı başlangıç zamanından sonra olmalıdır.");

        StartTime = newStart.ToUniversalTime();
        EndTime   = newEnd.ToUniversalTime();
        if (newDoctorId.HasValue) DoctorId = newDoctorId.Value;
        IncrementRowVersion();
        MarkUpdated();
    }

    public void ReassignDoctor(long newDoctorId)
    {
        DoctorId = newDoctorId;
        IncrementRowVersion();
        MarkUpdated();
    }

    public void SetAppointmentNo(string no) => AppointmentNo = no;

    public void AddNote(string note) { Notes = note; MarkUpdated(); }

    public void MarkArrived()     { PatientArrivedAt = DateTime.UtcNow; MarkUpdated(); }
    public void MarkEnteredRoom() { EnteredRoomAt    = DateTime.UtcNow; MarkUpdated(); }
    public void MarkLeftRoom()    { LeftRoomAt       = DateTime.UtcNow; MarkUpdated(); }
    public void MarkLeftClinic()  { LeftClinicAt     = DateTime.UtcNow; MarkUpdated(); }

    public void IncrementRowVersion() => RowVersion++;
}
