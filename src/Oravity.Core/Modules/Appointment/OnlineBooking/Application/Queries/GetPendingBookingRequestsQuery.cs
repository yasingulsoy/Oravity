using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.OnlineBooking.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Appointment.OnlineBooking.Application.Queries;

public record GetPendingBookingRequestsQuery(
    long BranchId,
    int Page = 1,
    int PageSize = 20
) : IRequest<List<OnlineBookingRequestResponse>>;

public class GetPendingBookingRequestsQueryHandler
    : IRequestHandler<GetPendingBookingRequestsQuery, List<OnlineBookingRequestResponse>>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public GetPendingBookingRequestsQueryHandler(AppDbContext db, ICurrentUser user)
    {
        _db   = db;
        _user = user;
    }

    public async Task<List<OnlineBookingRequestResponse>> Handle(
        GetPendingBookingRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.OnlineBookingRequests
            .AsNoTracking()
            .Where(r =>
                r.BranchId == request.BranchId &&
                r.Status == BookingRequestStatus.Pending)
            .OrderBy(r => r.RequestedDate)
            .ThenBy(r => r.RequestedTime);

        var results = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return results.Select(OnlineBookingMappings.ToResponse).ToList();
    }
}
