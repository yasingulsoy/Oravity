using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.PatientPortal.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientPortal.Application.Queries;

public record GetMyAppointmentsQuery(
    bool? FutureOnly = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<List<PortalAppointmentItem>>;

public class GetMyAppointmentsQueryHandler
    : IRequestHandler<GetMyAppointmentsQuery, List<PortalAppointmentItem>>
{
    private readonly AppDbContext _db;
    private readonly ICurrentPortalUser _portalUser;

    public GetMyAppointmentsQueryHandler(AppDbContext db, ICurrentPortalUser portalUser)
    {
        _db         = db;
        _portalUser = portalUser;
    }

    public async Task<List<PortalAppointmentItem>> Handle(
        GetMyAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        var patientId = _portalUser.PatientId;
        var now       = DateTime.UtcNow;

        var query = _db.Appointments
            .AsNoTracking()
            .Where(a => a.PatientId == patientId);

        if (request.FutureOnly == true)
            query = query.Where(a => a.StartTime >= now);
        else if (request.FutureOnly == false)
            query = query.Where(a => a.StartTime < now);

        var appointments = await query
            .OrderByDescending(a => a.StartTime)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new
            {
                a.PublicId,
                a.StartTime,
                a.EndTime,
                DoctorName = a.Doctor.FullName,
                a.StatusId
            })
            .ToListAsync(cancellationToken);

        return appointments.Select(a => new PortalAppointmentItem(
            a.PublicId,
            a.StartTime,
            a.EndTime,
            a.DoctorName,
            a.StatusId,
            PatientPortalMappings.AppointmentStatusLabel(a.StatusId),
            a.StartTime >= now
        )).ToList();
    }
}
