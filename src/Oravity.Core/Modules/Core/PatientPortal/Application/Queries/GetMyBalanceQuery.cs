using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.PatientPortal.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientPortal.Application.Queries;

public record GetMyBalanceQuery : IRequest<PortalBalanceResponse>;

public class GetMyBalanceQueryHandler
    : IRequestHandler<GetMyBalanceQuery, PortalBalanceResponse>
{
    private readonly AppDbContext _db;
    private readonly ICurrentPortalUser _portalUser;

    public GetMyBalanceQueryHandler(AppDbContext db, ICurrentPortalUser portalUser)
    {
        _db         = db;
        _portalUser = portalUser;
    }

    public async Task<PortalBalanceResponse> Handle(
        GetMyBalanceQuery request,
        CancellationToken cancellationToken)
    {
        var patientId = _portalUser.PatientId;

        // Toplam tedavi tutarı (tamamlanan kalemlerin final_price'ları)
        var totalTreatment = await _db.TreatmentPlanItems
            .AsNoTracking()
            .Where(i => i.Plan.PatientId == patientId &&
                        i.Status == TreatmentItemStatus.Completed)
            .SumAsync(i => i.FinalPrice, cancellationToken);

        // Ödemeler
        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

        var totalPaid = payments.Sum(p => p.Amount);

        var paymentItems = payments.Select(p => new PortalPaymentItem(
            p.PublicId,
            p.Amount,
            p.Currency,
            (int)p.Method,
            PatientPortalMappings.PaymentMethodLabel((int)p.Method),
            p.PaymentDate,
            p.Notes)).ToList();

        return new PortalBalanceResponse(
            totalTreatment,
            totalPaid,
            totalTreatment - totalPaid,
            paymentItems);
    }
}
