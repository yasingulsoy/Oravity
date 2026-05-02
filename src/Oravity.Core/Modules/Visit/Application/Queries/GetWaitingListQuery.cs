using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Visit.Application.Queries;

/// <summary>Şubedeki güncel bekleme listesi (status: Bekliyor veya Protokol Açıldı).</summary>
public record GetWaitingListQuery(long? BranchId = null) : IRequest<IReadOnlyList<WaitingListItemResponse>>;

public class GetWaitingListQueryHandler : IRequestHandler<GetWaitingListQuery, IReadOnlyList<WaitingListItemResponse>>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetWaitingListQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<WaitingListItemResponse>> Handle(GetWaitingListQuery request, CancellationToken ct)
    {
        var branchId = request.BranchId ?? _tenant.BranchId;
        var today    = DateOnly.FromDateTime(DateTime.UtcNow);

        var query = _db.Visits
            .AsNoTracking()
            .Where(v => !v.IsDeleted
                        && v.VisitDate == today
                        && ((int)v.Status == (int)VisitStatus.Waiting
                            || (int)v.Status == (int)VisitStatus.ProtocolOpened));

        if (branchId.HasValue)
            query = query.Where(v => v.BranchId == branchId.Value);
        else if (!_tenant.IsCompanyAdmin && !_tenant.IsPlatformAdmin)
            query = query.Where(v => v.BranchId == _tenant.BranchId);

        // Protokol tip renklerini bir kez çek
        var typeColors = await _db.ProtocolTypes
            .AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => new { t.Name, t.Color }, ct);

        var visits = await query
            .Select(v => new
            {
                v.PublicId,
                v.PatientId,
                PatientName  = v.Patient != null ? $"{v.Patient.FirstName} {v.Patient.LastName}".Trim() : "",
                Phone        = v.Patient != null ? v.Patient.Phone : null,
                PatientBirthDate = v.Patient != null ? v.Patient.BirthDate : null,
                PatientGender    = v.Patient != null ? v.Patient.Gender    : null,
                v.CheckInAt,
                v.IsWalkIn,
                v.Status,
                BranchName = v.Branch != null ? v.Branch.Name : null,
                AppointmentStart            = v.Appointment != null ? (DateTime?)v.Appointment.StartTime : null,
                HasOpenProtocol             = v.Protocols.Any(p => (int)p.Status == (int)ProtocolStatus.Open && !p.IsDeleted),
                AppointmentDoctorId         = v.Appointment != null ? (long?)v.Appointment.DoctorId : null,
                AppointmentSpecializationId = v.Appointment != null ? v.Appointment.SpecializationId : null,
                v.CalledAt,
                Protocols = v.Protocols
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.CreatedAt)
                    .Select(p => new
                    {
                        p.PublicId,
                        p.ProtocolNo,
                        p.ProtocolType,
                        p.Status,
                        p.Diagnosis,
                        p.StartedAt,
                        DoctorName = p.Doctor != null ? p.Doctor.FullName : "",
                    })
                    .ToList(),
            })
            .OrderBy(v => v.CheckInAt)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;

        return visits.Select(v => new WaitingListItemResponse(
            v.PublicId,
            v.PatientId,
            v.PatientName,
            v.Phone,
            v.CheckInAt,
            v.IsWalkIn,
            (int)v.Status,
            VisitLabels.VisitStatus((int)v.Status),
            v.AppointmentStart.HasValue ? v.AppointmentStart.Value.ToString("HH:mm") : null,
            v.HasOpenProtocol,
            (int)(now - v.CheckInAt).TotalMinutes,
            v.AppointmentDoctorId,
            v.AppointmentSpecializationId,
            v.PatientBirthDate,
            v.PatientGender,
            IsBeingCalled: v.CalledAt.HasValue && v.Status == VisitStatus.Waiting,
            BranchName: v.BranchName,
            v.Protocols.Select(p =>
            {
                var typeId = (int)p.ProtocolType;
                typeColors.TryGetValue(typeId, out var tc);
                return new WaitingProtocolItem(
                    p.PublicId,
                    p.ProtocolNo,
                    typeId,
                    tc?.Name ?? VisitLabels.ProtocolType(typeId),
                    tc?.Color ?? "#6366f1",
                    (int)p.Status,
                    VisitLabels.ProtocolStatus((int)p.Status),
                    p.DoctorName,
                    p.Diagnosis,
                    p.StartedAt);
            }).ToList()
        )).ToList();
    }
}
