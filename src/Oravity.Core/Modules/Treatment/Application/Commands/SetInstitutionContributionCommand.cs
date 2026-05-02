using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Treatment.Application.Commands;

/// <summary>
/// Provizyon kurumunun TZH onayına göre kurum katkı tutarını kalem bazında girer.
/// PatientAmount otomatik hesaplanır: FinalPrice - ContributionAmount.
/// İzin: treatment_plan:edit
/// </summary>
public record SetInstitutionContributionCommand(
    Guid     ItemPublicId,
    decimal? ContributionAmount,
    long?    InstitutionId = null
) : IRequest<TreatmentPlanItemResponse>;

public class SetInstitutionContributionCommandHandler
    : IRequestHandler<SetInstitutionContributionCommand, TreatmentPlanItemResponse>
{
    private readonly AppDbContext _db;

    public SetInstitutionContributionCommandHandler(AppDbContext db) => _db = db;

    public async Task<TreatmentPlanItemResponse> Handle(
        SetInstitutionContributionCommand request,
        CancellationToken cancellationToken)
    {
        var item = await _db.TreatmentPlanItems
            .Include(i => i.Treatment)
            .Include(i => i.Doctor)
            .Include(i => i.ApprovedBy)
            .Include(i => i.Plan).ThenInclude(p => p.Doctor)
            .FirstOrDefaultAsync(i => i.PublicId == request.ItemPublicId, cancellationToken)
            ?? throw new NotFoundException("Tedavi kalemi bulunamadı.");

        await TreatmentItemFinancialGuard.AssertContributionCanBeChangedAsync(item.Id, _db, cancellationToken);

        item.SetInstitutionContribution(request.ContributionAmount, request.InstitutionId);
        await _db.SaveChangesAsync(cancellationToken);

        return TreatmentPlanMappings.ToResponse(item);
    }
}
