using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Randevu durumu.
/// Tek yönlü geçiş şeması: Planned → Confirmed → Arrived → InRoom → Completed
/// Her zaman → Cancelled veya NoShow
/// </summary>
public enum AppointmentStatus
{
    Planned   = 1,  // Planlandı
    Confirmed = 2,  // Onaylandı
    Arrived   = 3,  // Geldi (check-in)
    InRoom    = 4,  // Odaya Alındı
    Completed = 5,  // Tamamlandı
    Cancelled = 6,  // İptal
    NoShow    = 7   // Gelmedi
}

/// <summary>
/// Randevu kaydı.
/// Slot çakışması: SPEC §2.1 — unique partial index (doctor_id, branch_id, start_time)
/// Optimistic lock: RowVersion — MoveAppointment handler versiyonu kontrol eder.
/// </summary>
public class Appointment : AuditableEntity
{
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    public long DoctorId { get; private set; }
    public User Doctor { get; private set; } = default!;

    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }

    public AppointmentStatus Status { get; private set; } = AppointmentStatus.Planned;

    public string? Notes { get; private set; }

    /// <summary>
    /// Optimistic lock sayacı.
    /// EF Core concurrency token olarak yapılandırılır.
    /// MoveAppointment/UpdateStatus handler'larında artırılır.
    /// </summary>
    public int RowVersion { get; private set; } = 1;

    private Appointment() { }

    public static Appointment Create(
        long branchId,
        long patientId,
        long doctorId,
        DateTime startTime,
        DateTime endTime,
        string? notes = null)
    {
        if (endTime <= startTime)
            throw new ArgumentException("Bitiş zamanı başlangıç zamanından sonra olmalıdır.");

        return new Appointment
        {
            BranchId  = branchId,
            PatientId = patientId,
            DoctorId  = doctorId,
            StartTime = startTime.ToUniversalTime(),
            EndTime   = endTime.ToUniversalTime(),
            Status    = AppointmentStatus.Planned,
            Notes     = notes,
            RowVersion = 1
        };
    }

    /// <summary>Geçerli durum geçişi kontrolü ile durumu günceller.</summary>
    public void UpdateStatus(AppointmentStatus newStatus)
    {
        if (!IsValidTransition(Status, newStatus))
            throw new InvalidOperationException(
                $"'{Status}' → '{newStatus}' geçişi geçersiz.");
        Status = newStatus;
        IncrementRowVersion();
        MarkUpdated();
    }

    /// <summary>Randevuyu yeni zaman dilimine taşır.</summary>
    public void MoveTo(DateTime newStart, DateTime newEnd, long? newDoctorId = null)
    {
        if (newEnd <= newStart)
            throw new ArgumentException("Bitiş zamanı başlangıç zamanından sonra olmalıdır.");

        if (Status is AppointmentStatus.Completed
                    or AppointmentStatus.Cancelled
                    or AppointmentStatus.NoShow)
            throw new InvalidOperationException("Terminal durumundaki randevu taşınamaz.");

        StartTime = newStart.ToUniversalTime();
        EndTime   = newEnd.ToUniversalTime();
        if (newDoctorId.HasValue) DoctorId = newDoctorId.Value;
        IncrementRowVersion();
        MarkUpdated();
    }

    public void Cancel(string? reason = null)
    {
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new InvalidOperationException("Bu randevu zaten sonlandırılmış.");
        Notes = string.IsNullOrWhiteSpace(reason) ? Notes : reason;
        Status = AppointmentStatus.Cancelled;
        IncrementRowVersion();
        MarkUpdated();
    }

    public void AddNote(string note)
    {
        Notes = note;
        MarkUpdated();
    }

    public void IncrementRowVersion() => RowVersion++;

    // ─── Geçerli durum geçişleri ──────────────────────────────────────────
    private static readonly Dictionary<AppointmentStatus, AppointmentStatus[]> AllowedTransitions = new()
    {
        [AppointmentStatus.Planned]   = [AppointmentStatus.Confirmed, AppointmentStatus.Arrived,    AppointmentStatus.Cancelled, AppointmentStatus.NoShow],
        [AppointmentStatus.Confirmed] = [AppointmentStatus.Arrived,   AppointmentStatus.Cancelled,  AppointmentStatus.NoShow],
        [AppointmentStatus.Arrived]   = [AppointmentStatus.InRoom,    AppointmentStatus.Cancelled],
        [AppointmentStatus.InRoom]    = [AppointmentStatus.Completed, AppointmentStatus.Cancelled],
        [AppointmentStatus.Completed] = [],
        [AppointmentStatus.Cancelled] = [],
        [AppointmentStatus.NoShow]    = []
    };

    public static bool IsValidTransition(AppointmentStatus from, AppointmentStatus to)
        => AllowedTransitions.TryGetValue(from, out var allowed) && Array.Exists(allowed, s => s == to);
}
