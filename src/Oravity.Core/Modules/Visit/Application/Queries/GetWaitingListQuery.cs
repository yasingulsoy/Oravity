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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

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

        var visits = await query
            .Select(v => new
            {
                v.PublicId,
                v.PatientId,
                PatientName  = v.Patient != null ? $"{v.Patient.FirstName} {v.Patient.LastName}".Trim() : "",
                Phone        = v.Patient != null ? v.Patient.Phone : null,
                v.CheckInAt,
                v.IsWalkIn,
                v.Status,
                AppointmentStart = v.Appointment != null ? (DateTime?)v.Appointment.StartTime : null,
                HasOpenProtocol  = v.Protocols.Any(p => (int)p.Status == (int)ProtocolStatus.Open),
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
            v.AppointmentStart.HasValue
                ? v.AppointmentStart.Value.ToString("HH:mm")
                : null,
            v.HasOpenProtocol,
            (int)(now - v.CheckInAt).TotalMinutes
        )).ToList();
    }
}
