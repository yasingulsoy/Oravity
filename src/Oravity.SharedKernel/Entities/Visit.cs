using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum VisitStatus
{
    Waiting        = 1,  // Bekliyor
    ProtocolOpened = 2,  // Protokol Açıldı
    Completed      = 3,  // Tamamlandı
    Cancelled      = 4   // İptal
}

/// <summary>
/// Hastanın fiziksel kliniğe girişi (SPEC §VİZİTE & PROTOKOL MİMARİSİ).
/// Appointment ile 1-1 (randevulu), ya da null (walk-in) ilişkisi.
/// </summary>
public class Visit : BaseEntity
{
    public long BranchId   { get; private set; }
    public Branch Branch   { get; private set; } = default!;
    public long CompanyId  { get; private set; }
    public long PatientId  { get; private set; }
    public Patient Patient { get; private set; } = default!;

    /// <summary>Null = walk-in hasta</summary>
    public long? AppointmentId { get; private set; }
    public Appointment? Appointment { get; private set; }

    public bool IsWalkIn { get; private set; }

    public DateOnly  VisitDate   { get; private set; }
    public DateTime  CheckInAt   { get; private set; }
    public DateTime? CheckOutAt  { get; private set; }

    // CalledAt — AddVisitCalledAt migration çalıştırıldıktan sonra açılacak
    // public DateTime? CalledAt { get; private set; }

    public VisitStatus Status { get; private set; } = VisitStatus.Waiting;
    public string?     Notes  { get; private set; }
    public long        CreatedBy { get; private set; }

    public ICollection<Protocol> Protocols { get; private set; } = [];

    private Visit() { }

    public static Visit Create(
        long branchId,
        long companyId,
        long patientId,
        long? appointmentId,
        bool isWalkIn,
        string? notes,
        long createdBy) => new()
    {
        BranchId      = branchId,
        CompanyId     = companyId,
        PatientId     = patientId,
        AppointmentId = appointmentId,
        IsWalkIn      = isWalkIn,
        VisitDate     = DateOnly.FromDateTime(DateTime.UtcNow),
        CheckInAt     = DateTime.UtcNow,
        Status        = VisitStatus.Waiting,
        Notes         = notes,
        CreatedBy     = createdBy,
    };

    public void MarkCalled()
    {
        // CalledAt = DateTime.UtcNow; // AddVisitCalledAt migration sonrası açılacak
        MarkUpdated();
    }

    public void OpenProtocol()
    {
        if (Status == VisitStatus.Waiting)
        {
            Status = VisitStatus.ProtocolOpened;
            MarkUpdated();
        }
    }

    public void CheckOut()
    {
        if (Status is VisitStatus.Waiting or VisitStatus.ProtocolOpened)
        {
            Status      = VisitStatus.Completed;
            CheckOutAt  = DateTime.UtcNow;
            MarkUpdated();
        }
    }

    public void Cancel()
    {
        if (Status is VisitStatus.Completed or VisitStatus.Cancelled)
            throw new InvalidOperationException("Bu vizite iptal edilemez.");
        Status = VisitStatus.Cancelled;
        MarkUpdated();
    }
}
