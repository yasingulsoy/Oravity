using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum BookingRequestStatus
{
    Pending          = 1,  // Bekliyor (onay modu)
    AutoApproved     = 2,  // Otomatik Onaylandı
    ReceptionApproved = 3, // Resepsiyon Onayladı
    Rejected         = 4,  // Reddedildi
    Cancelled        = 5   // İptal Edildi (hasta tarafından)
}

public enum BookingSource
{
    Widget = 1,
    Portal = 2,
    Mobile = 3
}

public enum BookingPatientType
{
    New      = 1,  // Yeni hasta
    Existing = 2   // Mevcut hasta
}

/// <summary>
/// Online randevu talebi (SPEC §ONLİNE RANDEVU SİSTEMİ §2.2).
/// Widget/portal üzerinden anonim ya da mevcut hasta tarafından oluşturulur.
/// auto_approve=true ise direkt Appointment oluşturulur (status=2).
/// false ise resepsiyon onayı beklenir (status=1).
/// </summary>
public class OnlineBookingRequest : BaseEntity
{
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public long DoctorId { get; private set; }
    public User Doctor { get; private set; } = default!;

    /// <summary>Mevcut hasta ise dolu; yeni hasta ise null.</summary>
    public long? PatientId { get; private set; }
    public Patient? Patient { get; private set; }

    public BookingPatientType PatientType { get; private set; }

    // ── Yeni hasta form bilgileri ─────────────────────────────────────────
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }

    // ── Randevu zamanı ────────────────────────────────────────────────────
    public DateOnly RequestedDate { get; private set; }
    public TimeOnly RequestedTime { get; private set; }
    public int SlotDuration { get; private set; }

    public string? PatientNote { get; private set; }

    public BookingSource Source { get; private set; } = BookingSource.Widget;
    public BookingRequestStatus Status { get; private set; } = BookingRequestStatus.Pending;

    /// <summary>Onaylandıktan sonra oluşturulan Appointment ID.</summary>
    public long? AppointmentId { get; private set; }

    public string? RejectionReason { get; private set; }

    // ── Telefon doğrulama ─────────────────────────────────────────────────
    public bool PhoneVerified { get; private set; }
    public string? VerificationCode { get; private set; }
    public DateTime? VerificationExpires { get; private set; }

    // ── Review meta ───────────────────────────────────────────────────────
    public long? ReviewedBy { get; private set; }
    public User? Reviewer { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    private OnlineBookingRequest() { }

    public static OnlineBookingRequest Create(
        long branchId, long doctorId, BookingPatientType patientType,
        DateOnly requestedDate, TimeOnly requestedTime, int slotDuration,
        BookingSource source,
        long? patientId = null,
        string? firstName = null, string? lastName = null,
        string? phone = null, string? email = null,
        string? patientNote = null)
    {
        return new OnlineBookingRequest
        {
            BranchId      = branchId,
            DoctorId      = doctorId,
            PatientId     = patientId,
            PatientType   = patientType,
            FirstName     = firstName,
            LastName      = lastName,
            Phone         = phone,
            Email         = email,
            RequestedDate = requestedDate,
            RequestedTime = requestedTime,
            SlotDuration  = slotDuration,
            PatientNote   = patientNote,
            Source        = source,
            Status        = BookingRequestStatus.Pending
        };
    }

    /// <summary>6 haneli doğrulama kodu oluştur (5 dakika geçerli).</summary>
    public string GenerateVerificationCode()
    {
        VerificationCode    = Random.Shared.Next(100000, 999999).ToString();
        VerificationExpires = DateTime.UtcNow.AddMinutes(5);
        return VerificationCode;
    }

    public bool VerifyPhone(string code)
    {
        if (VerificationCode != code || VerificationExpires < DateTime.UtcNow)
            return false;
        PhoneVerified      = true;
        VerificationCode   = null;
        VerificationExpires = null;
        MarkUpdated();
        return true;
    }

    /// <summary>Otomatik onay — direkt randevu oluşturulduğunda çağrılır.</summary>
    public void MarkAutoApproved(long appointmentId)
    {
        Status        = BookingRequestStatus.AutoApproved;
        AppointmentId = appointmentId;
        ReviewedAt    = DateTime.UtcNow;
        MarkUpdated();
    }

    /// <summary>Resepsiyon onayı.</summary>
    public void Approve(long reviewedBy, long appointmentId)
    {
        Status        = BookingRequestStatus.ReceptionApproved;
        AppointmentId = appointmentId;
        ReviewedBy    = reviewedBy;
        ReviewedAt    = DateTime.UtcNow;
        MarkUpdated();
    }

    /// <summary>Reddetme.</summary>
    public void Reject(long reviewedBy, string? reason = null)
    {
        Status           = BookingRequestStatus.Rejected;
        RejectionReason  = reason;
        ReviewedBy       = reviewedBy;
        ReviewedAt       = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Cancel()
    {
        Status = BookingRequestStatus.Cancelled;
        MarkUpdated();
    }
}
