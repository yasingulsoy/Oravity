using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.PatientRecord.Application;
using Oravity.Infrastructure.Database;

namespace Oravity.Core.Modules.Core.PatientRecord.Application.Queries;

public record GetPatientMedicationsQuery(
    long PatientId,
    bool? ActiveOnly = true
) : IRequest<IReadOnlyList<PatientMedicationResponse>>;

public class GetPatientMedicationsQueryHandler
    : IRequestHandler<GetPatientMedicationsQuery, IReadOnlyList<PatientMedicationResponse>>
{
    private readonly AppDbContext _db;

    public GetPatientMedicationsQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PatientMedicationResponse>> Handle(
        GetPatientMedicationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.PatientMedications
            .AsNoTracking()
            .Where(m => m.PatientId == request.PatientId);

        if (request.ActiveOnly == true)
            query = query.Where(m => m.IsActive);

        return await query
            .OrderByDescending(m => m.AddedAt)
            .Select(m => PatientRecordMappings.ToMedicationResponse(m))
            .ToListAsync(cancellationToken);
    }
}
