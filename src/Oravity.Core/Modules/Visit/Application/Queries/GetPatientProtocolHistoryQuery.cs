using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Infrastructure.Database;

namespace Oravity.Core.Modules.Visit.Application.Queries;

public record ProtocolHistoryItem(
    Guid      PublicId,
    string    ProtocolNo,
    DateTime  CreatedAt,
    string    BranchName,
    int       ProtocolType,
    string    ProtocolTypeName,
    int       Status,
    string    StatusName,
    string    DoctorName,
    string?   ChiefComplaint,
    string?   Diagnosis
);

public record GetPatientProtocolHistoryQuery(Guid PatientPublicId, int Limit = 20)
    : IRequest<IReadOnlyList<ProtocolHistoryItem>>;

public class GetPatientProtocolHistoryQueryHandler
    : IRequestHandler<GetPatientProtocolHistoryQuery, IReadOnlyList<ProtocolHistoryItem>>
{
    private readonly AppDbContext _db;

    public GetPatientProtocolHistoryQueryHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProtocolHistoryItem>> Handle(
        GetPatientProtocolHistoryQuery request, CancellationToken ct)
    {
        return await _db.Protocols
            .AsNoTracking()
            .Include(p => p.Doctor)
            .Include(p => p.Branch)
            .Where(p => !p.IsDeleted && p.Patient.PublicId == request.PatientPublicId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(request.Limit)
            .Select(p => new ProtocolHistoryItem(
                p.PublicId,
                p.ProtocolNo,
                p.CreatedAt,
                p.Branch.Name,
                (int)p.ProtocolType,
                VisitLabels.ProtocolType((int)p.ProtocolType),
                (int)p.Status,
                VisitLabels.ProtocolStatus((int)p.Status),
                p.Doctor != null ? p.Doctor.FullName : "",
                p.ChiefComplaint,
                p.Diagnosis))
            .ToListAsync(ct);
    }
}
