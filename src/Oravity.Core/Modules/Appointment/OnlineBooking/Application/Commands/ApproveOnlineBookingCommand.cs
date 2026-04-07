using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.OnlineBooking.Application;
using Oravity.Core.Modules.Notification.Application.Commands;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using AppointmentEntity = Oravity.SharedKernel.Entities.Appointment;

namespace Oravity.Core.Modules.Appointment.OnlineBooking.Application.Commands;

public record ApproveOnlineBookingCommand(Guid RequestPublicId) : IRequest<OnlineBookingRequestResponse>;

public class ApproveOnlineBookingCommandHandler
    : IRequestHandler<ApproveOnlineBookingCommand, OnlineBookingRequestResponse>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IMediator _mediator;

    public ApproveOnlineBookingCommandHandler(
        AppDbContext db, ICurrentUser user, IMediator mediator)
    {
        _db      = db;
        _user    = user;
        _mediator = mediator;
    }

    public async Task<OnlineBookingRequestResponse> Handle(
        ApproveOnlineBookingCommand request,
        CancellationToken cancellationToken)
    {
        var bookingRequest = await _db.OnlineBookingRequests
            .FirstOrDefaultAsync(r => r.PublicId == request.RequestPublicId, cancellationToken)
            ?? throw new NotFoundException($"Talep bulunamadı: {request.RequestPublicId}");

        if (bookingRequest.Status != BookingRequestStatus.Pending)
            throw new InvalidOperationException("Yalnızca bekleyen talepler onaylanabilir.");

        var startTime = bookingRequest.RequestedDate
            .ToDateTime(bookingRequest.RequestedTime)
            .ToUniversalTime();
        var endTime = startTime.AddMinutes(bookingRequest.SlotDuration);

        // Appointment oluştur
        var appointment = AppointmentEntity.Create(
            branchId:  bookingRequest.BranchId,
            patientId: bookingRequest.PatientId ?? 0,
            doctorId:  bookingRequest.DoctorId,
            statusId:  AppointmentStatus.WellKnownIds.Confirmed,
            startTime: startTime,
            endTime:   endTime);

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync(cancellationToken);

        bookingRequest.Approve(_user.UserId, appointment.Id);
        await _db.SaveChangesAsync(cancellationToken);

        // Hastaya SMS gönder (telefon varsa)
        if (!string.IsNullOrWhiteSpace(bookingRequest.Phone))
        {
            await _mediator.Send(new QueueSmsCommand(
                bookingRequest.Phone,
                $"Randevunuz onaylandı: {bookingRequest.RequestedDate:dd MMMM}, {bookingRequest.RequestedTime:HH:mm}",
                "BOOKING_APPROVED"), cancellationToken);
        }

        return OnlineBookingMappings.ToResponse(bookingRequest);
    }
}
