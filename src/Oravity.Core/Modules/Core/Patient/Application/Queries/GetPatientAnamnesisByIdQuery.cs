using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Patient.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Core.Patient.Application.Queries;

public record GetPatientAnamnesisByIdQuery(Guid PatientPublicId, Guid AnamnesisPublicId)
    : IRequest<PatientAnamnesisResponse>;

public class GetPatientAnamnesisByIdQueryHandler
    : IRequestHandler<GetPatientAnamnesisByIdQuery, PatientAnamnesisResponse>
{
    private readonly AppDbContext _db;

    public GetPatientAnamnesisByIdQueryHandler(AppDbContext db) => _db = db;

    public async Task<PatientAnamnesisResponse> Handle(
        GetPatientAnamnesisByIdQuery request, CancellationToken ct)
    {
        var a = await _db.PatientAnamneses
            .AsNoTracking()
            .Include(x => x.FilledByUser)
            .FirstOrDefaultAsync(x =>
                !x.IsDeleted &&
                x.PublicId == request.AnamnesisPublicId &&
                x.Patient.PublicId == request.PatientPublicId, ct)
            ?? throw new NotFoundException("Anamnez kaydı bulunamadı.");

        return GetPatientAnamnesisQueryHandler.Map(a);
    }
}
