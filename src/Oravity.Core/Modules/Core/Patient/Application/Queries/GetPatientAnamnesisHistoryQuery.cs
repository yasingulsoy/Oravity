using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Patient.Application;
using Oravity.Infrastructure.Database;

namespace Oravity.Core.Modules.Core.Patient.Application.Queries;

public record GetPatientAnamnesisHistoryQuery(Guid PatientPublicId, int Limit = 50)
    : IRequest<IReadOnlyList<AnamnesisHistoryItem>>;

public class GetPatientAnamnesisHistoryQueryHandler
    : IRequestHandler<GetPatientAnamnesisHistoryQuery, IReadOnlyList<AnamnesisHistoryItem>>
{
    private readonly AppDbContext _db;

    public GetPatientAnamnesisHistoryQueryHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AnamnesisHistoryItem>> Handle(
        GetPatientAnamnesisHistoryQuery request, CancellationToken ct)
    {
        return await _db.PatientAnamneses
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Patient.PublicId == request.PatientPublicId)
            .OrderByDescending(x => x.FilledAt)
            .Take(request.Limit)
            .Select(x => new AnamnesisHistoryItem(
                x.PublicId,
                x.FilledAt,
                x.FilledByUser != null ? x.FilledByUser.FullName : "",
                x.LocalAnesthesiaAllergy || x.HasPenicillinAllergy || x.OnAnticoagulant ||
                x.BleedingTendency || x.HasPacemaker || x.BisphosphonateUse ||
                x.HasHiv || x.HasHepatitisB || x.HasHepatitisC,
                x.BloodType,
                x.SmokingStatus,
                x.AlcoholUse))
            .ToListAsync(ct);
    }
}
