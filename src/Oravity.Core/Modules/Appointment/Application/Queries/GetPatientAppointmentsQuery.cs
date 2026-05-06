using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Appointment.Application.Queries;

public record GetPatientAppointmentsQuery(
    Guid PatientPublicId,
    int PageSize = 50,
    int Page = 1
) : IRequest<PatientAppointmentsResult>;

public record PatientAppointmentsResult(
    IReadOnlyList<AppointmentResponse> Items,
    int Total
);

public class GetPatientAppointmentsQueryHandler
    : IRequestHandler<GetPatientAppointmentsQuery, PatientAppointmentsResult>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetPatientAppointmentsQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PatientAppointmentsResult> Handle(
        GetPatientAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .Where(p => p.PublicId == request.PatientPublicId && !p.IsDeleted)
            .Select(p => new { p.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (patient is null)
            return new PatientAppointmentsResult([], 0);

        var query = _db.Appointments
            .AsNoTracking()
            .Where(a => a.PatientId == patient.Id);

        query = ApplyTenantFilter(query);

        var total = await query.CountAsync(cancellationToken);

        var raw = await query
            .OrderByDescending(a => a.StartTime)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new
            {
                a.PublicId, a.BranchId, a.PatientId, a.DoctorId,
                PatientName = a.Patient != null
                    ? a.Patient.FirstName + " " + a.Patient.LastName
                    : null,
                PatientBirthDate = a.Patient != null ? a.Patient.BirthDate : null,
                PatientGender    = a.Patient != null ? a.Patient.Gender    : null,
                DoctorName = a.Doctor.FullName,
                BranchName = a.Branch != null ? a.Branch.Name : null,
                AppointmentTypeName = a.AppointmentType != null ? a.AppointmentType.Name : null,
                a.StartTime, a.EndTime, a.StatusId, a.Notes,
                a.IsUrgent, a.IsEarlierRequest, a.RowVersion, a.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var items = raw.Select(a => new AppointmentResponse(
            a.PublicId, a.BranchId, a.PatientId, a.PatientName,
            a.DoctorId, a.DoctorName,
            a.StartTime, a.EndTime, a.StatusId,
            AppointmentMappings.StatusLabel(a.StatusId),
            a.Notes, a.IsUrgent, a.IsEarlierRequest, a.RowVersion, a.CreatedAt,
            a.AppointmentTypeName,
            a.PatientBirthDate, a.PatientGender,
            HasOpenProtocol: false, IsBeingCalled: false, PatientPublicId: null,
            BranchName: a.BranchName
        )).ToList();

        return new PatientAppointmentsResult(items, total);
    }

    private IQueryable<SharedKernel.Entities.Appointment> ApplyTenantFilter(
        IQueryable<SharedKernel.Entities.Appointment> query)
    {
        if (_tenant.IsPlatformAdmin) return query;

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            return query.Where(a => a.BranchId == _tenant.BranchId.Value);

        if (_tenant.IsCompanyAdmin && _tenant.CompanyId.HasValue)
            return query.Where(a => a.Branch.CompanyId == _tenant.CompanyId.Value);

        return query.Where(_ => false);
    }
}
