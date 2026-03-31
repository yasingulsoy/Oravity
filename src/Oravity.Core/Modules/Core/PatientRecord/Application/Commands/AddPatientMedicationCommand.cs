using MediatR;
using Oravity.Core.Modules.Core.PatientRecord.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientRecord.Application.Commands;

public record AddPatientMedicationCommand(
    long PatientId,
    string DrugName,
    string? Dose = null,
    string? Frequency = null,
    string? Reason = null
) : IRequest<PatientMedicationResponse>;

public class AddPatientMedicationCommandHandler
    : IRequestHandler<AddPatientMedicationCommand, PatientMedicationResponse>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public AddPatientMedicationCommandHandler(AppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<PatientMedicationResponse> Handle(
        AddPatientMedicationCommand request,
        CancellationToken cancellationToken)
    {
        var medication = PatientMedication.Create(
            request.PatientId,
            request.DrugName,
            _user.UserId,
            request.Dose,
            request.Frequency,
            request.Reason);

        _db.PatientMedications.Add(medication);
        await _db.SaveChangesAsync(cancellationToken);

        return PatientRecordMappings.ToMedicationResponse(medication);
    }
}
