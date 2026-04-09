using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Visit.Application.Queries;

/// <summary>Hekimin kendine ait açık (ve bugünkü tamamlanmış) protokolleri.</summary>
public record GetDoctorProtocolsQuery(long? DoctorId = null) : IRequest<IReadOnlyList<DoctorProtocolResponse>>;

public class GetDoctorProtocolsQueryHandler : IRequestHandler<GetDoctorProtocolsQuery, IReadOnlyList<DoctorProtocolResponse>>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetDoctorProtocolsQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<DoctorProtocolResponse>> Handle(GetDoctorProtocolsQuery request, CancellationToken ct)
    {
        var doctorId = request.DoctorId ?? _tenant.UserId;
        var today    = DateTime.UtcNow.Date;

        var protocols = await _db.Protocols
            .AsNoTracking()
            .Where(p => !p.IsDeleted
                        && p.DoctorId == doctorId
                        && ((int)p.Status == (int)ProtocolStatus.Open
                            || (p.StartedAt.HasValue && p.StartedAt.Value.Date == today)))
            .Select(p => new DoctorProtocolResponse(
                p.PublicId,
                p.ProtocolNo,
                p.PatientId,
                p.Patient != null ? $"{p.Patient.FirstName} {p.Patient.LastName}".Trim() : "",
                p.Patient != null ? p.Patient.Phone : null,
                (int)p.ProtocolType,
                VisitLabels.ProtocolType((int)p.ProtocolType),
                (int)p.Status,
                VisitLabels.ProtocolStatus((int)p.Status),
                p.StartedAt))
            .OrderBy(p => p.StartedAt)
            .ToListAsync(ct);

        return protocols;
    }
}
