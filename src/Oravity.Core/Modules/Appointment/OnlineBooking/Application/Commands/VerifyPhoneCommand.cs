using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.OnlineBooking.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Appointment.OnlineBooking.Application.Commands;

public record VerifyPhoneCommand(Guid RequestPublicId, string Code) : IRequest<bool>;

public class VerifyPhoneCommandHandler : IRequestHandler<VerifyPhoneCommand, bool>
{
    private readonly AppDbContext _db;

    public VerifyPhoneCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(
        VerifyPhoneCommand request,
        CancellationToken cancellationToken)
    {
        var bookingRequest = await _db.OnlineBookingRequests
            .FirstOrDefaultAsync(r => r.PublicId == request.RequestPublicId, cancellationToken)
            ?? throw new NotFoundException($"Talep bulunamadı: {request.RequestPublicId}");

        var success = bookingRequest.VerifyPhone(request.Code);
        if (success)
            await _db.SaveChangesAsync(cancellationToken);

        return success;
    }
}
