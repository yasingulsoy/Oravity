using MediatR;

namespace Oravity.SharedKernel.Events;

/// <summary>
/// Outbox pattern event'leri — JSON payload'dan deserialize edilerek
/// MediatR üzerinden publish edilir.
/// </summary>

public record AppointmentCreatedEvent(
    long AppointmentId,
    Guid PublicId,
    long PatientId,
    long DoctorId,
    long BranchId,
    DateTime StartTime,
    DateTime EndTime
) : INotification;

public record AppointmentCompletedEvent(
    long AppointmentId,
    Guid PublicId,
    long PatientId,
    long DoctorId,
    long BranchId,
    long CompanyId,
    DateTime CompletedAt
) : INotification;

public record PaymentReceivedEvent(
    long PaymentId,
    Guid PublicId,
    long PatientId,
    long BranchId,
    decimal Amount,
    string Currency,
    string Method
) : INotification;

public record TreatmentItemCompletedEvent(
    long ItemId,
    Guid PublicId,
    long PlanId,
    long TreatmentId,
    long? DoctorId,
    DateTime? CompletedAt,
    decimal FinalPrice
) : INotification;
