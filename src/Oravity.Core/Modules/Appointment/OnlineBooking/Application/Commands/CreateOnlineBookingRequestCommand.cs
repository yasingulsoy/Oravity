using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.OnlineBooking.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using AppointmentEntity = Oravity.SharedKernel.Entities.Appointment;

namespace Oravity.Core.Modules.Appointment.OnlineBooking.Application.Commands;

public record CreateOnlineBookingRequestCommand(
    string BranchSlug,
    long DoctorId,
    BookingPatientType PatientType,
    DateOnly RequestedDate,
    TimeOnly RequestedTime,
    int SlotDuration,
    BookingSource Source,
    long? PatientId = null,
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    string? Email = null,
    string? PatientNote = null
) : IRequest<CreateBookingResult>;

public record CreateBookingResult(
    OnlineBookingRequestResponse Request,
    bool AutoApproved,
    string? VerificationCode
);

public class CreateOnlineBookingRequestCommandHandler
    : IRequestHandler<CreateOnlineBookingRequestCommand, CreateBookingResult>
{
    private readonly AppDbContext _db;

    public CreateOnlineBookingRequestCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CreateBookingResult> Handle(
        CreateOnlineBookingRequestCommand request,
        CancellationToken cancellationToken)
    {
        // Slug → BranchId
        var branchSettings = await _db.BranchOnlineBookingSettings
            .FirstOrDefaultAsync(b => b.WidgetSlug == request.BranchSlug, cancellationToken)
            ?? throw new NotFoundException($"Şube bulunamadı: {request.BranchSlug}");

        if (!branchSettings.IsEnabled)
            throw new InvalidOperationException("Bu şube için online randevu aktif değil.");

        // Hekim ayarları
        var doctorSettings = await _db.DoctorOnlineBookingSettings
            .FirstOrDefaultAsync(
                s => s.DoctorId == request.DoctorId && s.BranchId == branchSettings.BranchId,
                cancellationToken)
            ?? throw new NotFoundException($"Hekim online ayarı bulunamadı: {request.DoctorId}");

        if (!doctorSettings.IsOnlineVisible)
            throw new InvalidOperationException("Hekim şu anda online randevuya kapalı.");

        var bookingRequest = OnlineBookingRequest.Create(
            branchId:      branchSettings.BranchId,
            doctorId:      request.DoctorId,
            patientType:   request.PatientType,
            requestedDate: request.RequestedDate,
            requestedTime: request.RequestedTime,
            slotDuration:  request.SlotDuration,
            source:        request.Source,
            patientId:     request.PatientId,
            firstName:     request.FirstName,
            lastName:      request.LastName,
            phone:         request.Phone,
            email:         request.Email,
            patientNote:   request.PatientNote);

        // Telefon doğrulama kodu üret (telefon numarası varsa)
        string? verificationCode = null;
        if (!string.IsNullOrWhiteSpace(request.Phone))
            verificationCode = bookingRequest.GenerateVerificationCode();

        _db.OnlineBookingRequests.Add(bookingRequest);

        // auto_approve=true ise direkt Appointment oluştur
        bool autoApproved = false;
        if (doctorSettings.AutoApprove && (string.IsNullOrWhiteSpace(request.Phone) ||
            bookingRequest.PhoneVerified))
        {
            var startTime = request.RequestedDate
                .ToDateTime(request.RequestedTime)
                .ToUniversalTime();
            var endTime = startTime.AddMinutes(request.SlotDuration);

            var appointment = AppointmentEntity.Create(
                branchId:  branchSettings.BranchId,
                patientId: request.PatientId ?? 0, // yeni hasta ise 0, onay sonrası güncellenir
                doctorId:  request.DoctorId,
                startTime: startTime,
                endTime:   endTime);

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync(cancellationToken);

            bookingRequest.MarkAutoApproved(appointment.Id);
            autoApproved = true;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new CreateBookingResult(
            OnlineBookingMappings.ToResponse(bookingRequest),
            autoApproved,
            verificationCode);
    }
}
