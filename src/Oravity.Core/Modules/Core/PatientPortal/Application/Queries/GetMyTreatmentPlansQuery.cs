using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.PatientPortal.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientPortal.Application.Queries;

public record GetMyTreatmentPlansQuery(
    bool? CompletedOnly = null
) : IRequest<List<PortalTreatmentPlanItem>>;

public class GetMyTreatmentPlansQueryHandler
    : IRequestHandler<GetMyTreatmentPlansQuery, List<PortalTreatmentPlanItem>>
{
    private readonly AppDbContext _db;
    private readonly ICurrentPortalUser _portalUser;

    public GetMyTreatmentPlansQueryHandler(AppDbContext db, ICurrentPortalUser portalUser)
    {
        _db         = db;
        _portalUser = portalUser;
    }

    public async Task<List<PortalTreatmentPlanItem>> Handle(
        GetMyTreatmentPlansQuery request,
        CancellationToken cancellationToken)
    {
        var patientId = _portalUser.PatientId;

        var query = _db.TreatmentPlans
            .AsNoTracking()
            .Where(p => p.PatientId == patientId);

        if (request.CompletedOnly == true)
            query = query.Where(p => p.Status == TreatmentPlanStatus.Completed);
        else if (request.CompletedOnly == false)
            query = query.Where(p =>
                p.Status != TreatmentPlanStatus.Completed &&
                p.Status != TreatmentPlanStatus.Cancelled);

        var plans = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.PublicId,
                p.Name,
                StatusInt   = (int)p.Status,
                ItemCount   = p.Items.Count,
                TotalAmount = p.Items.Sum(i => i.FinalPrice)
            })
            .ToListAsync(cancellationToken);

        return plans.Select(p => new PortalTreatmentPlanItem(
            p.PublicId,
            p.Name,
            p.StatusInt,
            PatientPortalMappings.TreatmentStatusLabel(p.StatusInt),
            p.TotalAmount,
            p.ItemCount)).ToList();
    }
}
