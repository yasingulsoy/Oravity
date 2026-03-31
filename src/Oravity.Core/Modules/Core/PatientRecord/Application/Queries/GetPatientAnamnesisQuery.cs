using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.PatientRecord.Application;
using Oravity.Infrastructure.Database;

namespace Oravity.Core.Modules.Core.PatientRecord.Application.Queries;

public record GetPatientAnamnesisQuery(long PatientId) : IRequest<PatientAnamnesisResponse?>;

public class GetPatientAnamnesisQueryHandler
    : IRequestHandler<GetPatientAnamnesisQuery, PatientAnamnesisResponse?>
{
    private readonly AppDbContext _db;

    public GetPatientAnamnesisQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PatientAnamnesisResponse?> Handle(
        GetPatientAnamnesisQuery request,
        CancellationToken cancellationToken)
    {
        var anamnesis = await _db.PatientAnamneses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.PatientId == request.PatientId, cancellationToken);

        return anamnesis is null ? null : PatientRecordMappings.ToResponse(anamnesis);
    }
}
