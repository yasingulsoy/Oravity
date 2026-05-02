using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Appointment.Application.Queries;

public record GetAppointmentsByDateQuery(
    DateOnly Date,
    long? BranchId = null,
    long? DoctorId = null
) : IRequest<IReadOnlyList<AppointmentResponse>>;

public class GetAppointmentsByDateQueryHandler
    : IRequestHandler<GetAppointmentsByDateQuery, IReadOnlyList<AppointmentResponse>>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetAppointmentsByDateQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<AppointmentResponse>> Handle(
        GetAppointmentsByDateQuery request,
        CancellationToken cancellationToken)
    {
        // UTC gün aralığı
        var dayStart = request.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd   = dayStart.AddDays(1);

        var query = _db.Appointments.AsNoTracking()
            .Where(a => a.StartTime >= dayStart && a.StartTime < dayEnd);

        query = ApplyTenantFilter(query, request.BranchId);

        if (request.DoctorId.HasValue)
            query = query.Where(a => a.DoctorId == request.DoctorId.Value);

        var raw = await query
            .OrderBy(a => a.StartTime)
            .Select(a => new
            {
                a.PublicId, a.BranchId, a.PatientId, a.DoctorId,
                PatientName = a.Patient != null
                    ? a.Patient.FirstName + " " + a.Patient.LastName
                    : null,
                PatientBirthDate = a.Patient != null ? a.Patient.BirthDate : null,
                PatientGender    = a.Patient != null ? a.Patient.Gender    : null,
                DoctorName = a.Doctor.FullName,
                AppointmentTypeName = a.AppointmentType != null ? a.AppointmentType.Name : null,
                a.StartTime, a.EndTime, a.StatusId, a.Notes,
                a.IsUrgent, a.IsEarlierRequest,
                a.RowVersion, a.CreatedAt,
                HasOpenProtocol = _db.Visits
                    .Any(v => v.AppointmentId == a.Id && !v.IsDeleted &&
                              v.Protocols.Any(p => p.Status == SharedKernel.Entities.ProtocolStatus.Open && !p.IsDeleted)),
                IsBeingCalled = _db.Visits
                    .Any(v => v.AppointmentId == a.Id && !v.IsDeleted &&
                              v.CalledAt.HasValue &&
                              v.Status == SharedKernel.Entities.VisitStatus.Waiting),
            })
            .ToListAsync(cancellationToken);

        return raw.Select(a => new AppointmentResponse(
            a.PublicId, a.BranchId, a.PatientId, a.PatientName,
            a.DoctorId, a.DoctorName,
            a.StartTime, a.EndTime, a.StatusId,
            AppointmentMappings.StatusLabel(a.StatusId),
            a.Notes, a.IsUrgent, a.IsEarlierRequest, a.RowVersion, a.CreatedAt,
            a.AppointmentTypeName,
            a.PatientBirthDate, a.PatientGender, a.HasOpenProtocol, a.IsBeingCalled
        )).ToList();
    }

    private IQueryable<SharedKernel.Entities.Appointment> ApplyTenantFilter(
        IQueryable<SharedKernel.Entities.Appointment> query,
        long? requestedBranchId)
    {
        if (_tenant.IsPlatformAdmin)
        {
            if (requestedBranchId.HasValue)
                return query.Where(a => a.BranchId == requestedBranchId.Value);
            return query;
        }

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            return query.Where(a => a.BranchId == _tenant.BranchId.Value);

        if (_tenant.IsCompanyAdmin && _tenant.CompanyId.HasValue)
            return query.Where(a => a.Branch.CompanyId == _tenant.CompanyId.Value);

        return query.Where(_ => false);
    }
}
