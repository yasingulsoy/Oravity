using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Appointment.OnlineBooking.Application;

public record OnlineBookingRequestResponse(
    Guid PublicId,
    long BranchId,
    long DoctorId,
    long? PatientId,
    BookingPatientType PatientType,
    string? FirstName,
    string? LastName,
    string? Phone,
    string? Email,
    DateOnly RequestedDate,
    TimeOnly RequestedTime,
    int SlotDuration,
    string? PatientNote,
    BookingSource Source,
    BookingRequestStatus Status,
    string StatusLabel,
    long? AppointmentId,
    string? RejectionReason,
    bool PhoneVerified,
    long? ReviewedBy,
    DateTime? ReviewedAt,
    DateTime CreatedAt
);

public static class OnlineBookingMappings
{
    public static OnlineBookingRequestResponse ToResponse(OnlineBookingRequest r) => new(
        r.PublicId, r.BranchId, r.DoctorId, r.PatientId,
        r.PatientType, r.FirstName, r.LastName, r.Phone, r.Email,
        r.RequestedDate, r.RequestedTime, r.SlotDuration,
        r.PatientNote, r.Source, r.Status, StatusLabel(r.Status),
        r.AppointmentId, r.RejectionReason, r.PhoneVerified,
        r.ReviewedBy, r.ReviewedAt, r.CreatedAt);

    public static string StatusLabel(BookingRequestStatus s) => s switch
    {
        BookingRequestStatus.Pending           => "Bekliyor",
        BookingRequestStatus.AutoApproved      => "Otomatik Onaylandı",
        BookingRequestStatus.ReceptionApproved => "Resepsiyon Onayladı",
        BookingRequestStatus.Rejected          => "Reddedildi",
        BookingRequestStatus.Cancelled         => "İptal Edildi",
        _                                      => s.ToString()
    };
}
