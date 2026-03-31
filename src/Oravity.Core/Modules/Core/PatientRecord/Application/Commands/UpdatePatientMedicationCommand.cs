using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.PatientRecord.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientRecord.Application.Commands;

public record UpdatePatientMedicationCommand(
    long PatientId,
    long MedicationId,
    string DrugName,
    string? Dose = null,
    string? Frequency = null,
    string? Reason = null,
    bool IsActive = true
) : IRequest<PatientMedicationResponse>;

public class UpdatePatientMedicationCommandHandler
    : IRequestHandler<UpdatePatientMedicationCommand, PatientMedicationResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public UpdatePatientMedicationCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PatientMedicationResponse> Handle(
        UpdatePatientMedicationCommand request,
        CancellationToken cancellationToken)
    {
        var med = await _db.PatientMedications
            .FirstOrDefaultAsync(
                m => m.Id == request.MedicationId && m.PatientId == request.PatientId,
                cancellationToken)
            ?? throw new NotFoundException($"İlaç bulunamadı: {request.MedicationId}");

        med.Update(request.DrugName, request.Dose, request.Frequency, request.Reason);

        if (request.IsActive) med.Activate();
        else                  med.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);
        return PatientRecordMappings.ToMedicationResponse(med);
    }
}
