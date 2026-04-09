using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Visit.Application.Commands;

/// <summary>Walk-in (randevusuz) hasta check-in.</summary>
public record CheckInWalkInCommand(long PatientId, string? Notes) : IRequest<VisitResponse>;

public class CheckInWalkInCommandHandler : IRequestHandler<CheckInWalkInCommand, VisitResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICalendarBroadcastService _broadcast;

    public CheckInWalkInCommandHandler(AppDbContext db, ITenantContext tenant, ICalendarBroadcastService broadcast)
    {
        _db        = db;
        _tenant    = tenant;
        _broadcast = broadcast;
    }

    public async Task<VisitResponse> Handle(CheckInWalkInCommand request, CancellationToken ct)
    {
        var branchId = _tenant.BranchId
            ?? throw new ForbiddenException("Şube bağlamı gereklidir.");

        var companyId = _tenant.CompanyId
            ?? throw new ForbiddenException("Şirket bağlamı gereklidir.");

        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Hasta bulunamadı.");

        var visit = SharedKernel.Entities.Visit.Create(
            branchId:      branchId,
            companyId:     companyId,
            patientId:     request.PatientId,
            appointmentId: null,
            isWalkIn:      true,
            notes:         request.Notes,
            createdBy:     _tenant.UserId);

        _db.Visits.Add(visit);
        await _db.SaveChangesAsync(ct);

        var patientName = $"{patient.FirstName} {patient.LastName}".Trim();

        await _broadcast.BroadcastVisitAsync(
            branchId,
            new VisitBroadcastDto(
                visit.PublicId,
                visit.BranchId,
                visit.PatientId,
                patientName,
                true,
                (int)visit.Status),
            CalendarEventType.VisitCheckedIn, ct);

        return new VisitResponse(
            visit.PublicId, visit.PatientId, patientName, visit.BranchId,
            visit.CheckInAt, visit.CheckOutAt, visit.IsWalkIn,
            (int)visit.Status, VisitLabels.VisitStatus((int)visit.Status),
            visit.Notes, []);
    }
}
