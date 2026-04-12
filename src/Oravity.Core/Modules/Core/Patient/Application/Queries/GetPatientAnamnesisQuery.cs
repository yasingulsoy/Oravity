using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Patient.Application;
using Oravity.Infrastructure.Database;

namespace Oravity.Core.Modules.Core.Patient.Application.Queries;

public record GetPatientAnamnesisQuery(Guid PatientPublicId) : IRequest<PatientAnamnesisResponse?>;

public class GetPatientAnamnesisQueryHandler : IRequestHandler<GetPatientAnamnesisQuery, PatientAnamnesisResponse?>
{
    private readonly AppDbContext _db;

    public GetPatientAnamnesisQueryHandler(AppDbContext db) => _db = db;

    public async Task<PatientAnamnesisResponse?> Handle(GetPatientAnamnesisQuery request, CancellationToken ct)
    {
        var a = await _db.PatientAnamneses
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Patient.PublicId == request.PatientPublicId)
            .OrderByDescending(x => x.FilledAt)
            .FirstOrDefaultAsync(ct);

        return a == null ? null : Map(a);
    }

    internal static PatientAnamnesisResponse Map(SharedKernel.Entities.PatientAnamnesis a) => new(
        a.PublicId,
        a.BloodType, a.IsPregnant, a.IsBreastfeeding,
        a.HasDiabetes, a.HasHypertension, a.HasHeartDisease, a.HasPacemaker,
        a.HasAsthma, a.HasEpilepsy, a.HasKidneyDisease, a.HasLiverDisease,
        a.HasHiv, a.HasHepatitisB, a.HasHepatitisC, a.OtherSystemicDiseases,
        a.LocalAnesthesiaAllergy, a.LocalAnesthesiaAllergyNote,
        a.BleedingTendency, a.OnAnticoagulant, a.AnticoagulantDrug, a.BisphosphonateUse,
        a.HasPenicillinAllergy, a.HasAspirinAllergy, a.HasLatexAllergy, a.OtherAllergies,
        a.PreviousSurgeries,
        a.BrushingFrequency, a.UsesFloss,
        a.SmokingStatus, a.SmokingAmount, a.AlcoholUse,
        a.AdditionalNotes,
        a.HasCriticalAlert,
        a.FilledAt);
}
