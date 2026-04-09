using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Visit.Application.Commands;

/// <summary>Randevusu olan hastayı check-in yapar → visits kaydı oluşturur.</summary>
public record CheckInPatientCommand(Guid AppointmentPublicId, string? Notes) : IRequest<VisitResponse>;

public class CheckInPatientCommandHandler : IRequestHandler<CheckInPatientCommand, VisitResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICalendarBroadcastService _broadcast;

    public CheckInPatientCommandHandler(AppDbContext db, ITenantContext tenant, ICalendarBroadcastService broadcast)
    {
        _db        = db;
        _tenant    = tenant;
        _broadcast = broadcast;
    }

    public async Task<VisitResponse> Handle(CheckInPatientCommand request, CancellationToken ct)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.PublicId == request.AppointmentPublicId, ct)
            ?? throw new NotFoundException("Randevu bulunamadı.");

        if (await _db.Visits.AnyAsync(v => v.AppointmentId == appointment.Id && !v.IsDeleted, ct))
            throw new ConflictException("Bu randevu için zaten check-in yapılmış.");

        var companyId = _tenant.CompanyId
            ?? throw new ForbiddenException("Şirket bağlamı gereklidir.");

        var visit = SharedKernel.Entities.Visit.Create(
            branchId:      appointment.BranchId,
            companyId:     companyId,
            patientId:     appointment.PatientId!.Value,
            appointmentId: appointment.Id,
            isWalkIn:      false,
            notes:         request.Notes,
            createdBy:     _tenant.UserId);

        _db.Visits.Add(visit);

        // Randevu durumunu "Geldi" olarak güncelle
        appointment.SetStatus(AppointmentStatus.WellKnownIds.Arrived);

        await _db.SaveChangesAsync(ct);

        var patientName = appointment.Patient is { } p
            ? $"{p.FirstName} {p.LastName}".Trim()
            : "";

        await _broadcast.BroadcastVisitAsync(
            appointment.BranchId,
            new VisitBroadcastDto(
                visit.PublicId,
                visit.BranchId,
                visit.PatientId,
                patientName,
                false,
                (int)visit.Status),
            CalendarEventType.VisitCheckedIn, ct);

        return MapToResponse(visit, patientName);
    }

    private static VisitResponse MapToResponse(SharedKernel.Entities.Visit v, string patientName) =>
        new(v.PublicId, v.PatientId, patientName, v.BranchId,
            v.CheckInAt, v.CheckOutAt, v.IsWalkIn,
            (int)v.Status, VisitLabels.VisitStatus((int)v.Status),
            v.Notes, []);
}
