using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.OnlineBooking.Application;
using Oravity.Core.Modules.Notification.Application.Commands;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Appointment.OnlineBooking.Application.Commands;

public record RejectOnlineBookingCommand(
    Guid RequestPublicId,
    string? Reason = null
) : IRequest<OnlineBookingRequestResponse>;

public class RejectOnlineBookingCommandHandler
    : IRequestHandler<RejectOnlineBookingCommand, OnlineBookingRequestResponse>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IMediator _mediator;

    public RejectOnlineBookingCommandHandler(
        AppDbContext db, ICurrentUser user, IMediator mediator)
    {
        _db      = db;
        _user    = user;
        _mediator = mediator;
    }

    public async Task<OnlineBookingRequestResponse> Handle(
        RejectOnlineBookingCommand request,
        CancellationToken cancellationToken)
    {
        var bookingRequest = await _db.OnlineBookingRequests
            .FirstOrDefaultAsync(r => r.PublicId == request.RequestPublicId, cancellationToken)
            ?? throw new NotFoundException($"Talep bulunamadı: {request.RequestPublicId}");

        if (bookingRequest.Status != BookingRequestStatus.Pending)
            throw new InvalidOperationException("Yalnızca bekleyen talepler reddedilebilir.");

        bookingRequest.Reject(_user.UserId, request.Reason);
        await _db.SaveChangesAsync(cancellationToken);

        // Hastaya SMS gönder
        if (!string.IsNullOrWhiteSpace(bookingRequest.Phone))
        {
            var msg = string.IsNullOrWhiteSpace(request.Reason)
                ? "Randevu talebiniz reddedildi. Lütfen kliniğimizi arayınız."
                : $"Randevu talebiniz reddedildi: {request.Reason}";

            await _mediator.Send(new QueueSmsCommand(
                bookingRequest.Phone, msg, "BOOKING_REJECTED"), cancellationToken);
        }

        return OnlineBookingMappings.ToResponse(bookingRequest);
    }
}
