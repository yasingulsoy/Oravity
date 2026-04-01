using MediatR;
using Oravity.Core.Modules.Appointment.OnlineBooking.Application.Services;
using Oravity.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;

namespace Oravity.Core.Modules.Appointment.OnlineBooking.Application.Queries;

public record GetAvailableSlotsQuery(
    string BranchSlug,
    long DoctorId,
    DateOnly Date
) : IRequest<List<TimeSlotDto>>;

public class GetAvailableSlotsQueryHandler
    : IRequestHandler<GetAvailableSlotsQuery, List<TimeSlotDto>>
{
    private readonly IOnlineAvailabilityService _availability;
    private readonly AppDbContext _db;

    public GetAvailableSlotsQueryHandler(
        IOnlineAvailabilityService availability, AppDbContext db)
    {
        _availability = availability;
        _db           = db;
    }

    public async Task<List<TimeSlotDto>> Handle(
        GetAvailableSlotsQuery request,
        CancellationToken cancellationToken)
    {
        var branchSettings = await _db.BranchOnlineBookingSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.WidgetSlug == request.BranchSlug, cancellationToken)
            ?? throw new NotFoundException($"Şube bulunamadı: {request.BranchSlug}");

        return await _availability.GetAvailableSlots(
            request.DoctorId, branchSettings.BranchId, request.Date, cancellationToken);
    }
}
