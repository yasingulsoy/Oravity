using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Finance.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Queries;

public record GetPatientBalanceQuery(long PatientId) : IRequest<PatientBalanceResponse>;

public class GetPatientBalanceQueryHandler
    : IRequestHandler<GetPatientBalanceQuery, PatientBalanceResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetPatientBalanceQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PatientBalanceResponse> Handle(
        GetPatientBalanceQuery request,
        CancellationToken cancellationToken)
    {
        // Toplam tedavi tutarı (tamamlanmış kalemler)
        var treatmentQuery = _db.TreatmentPlanItems
            .AsNoTracking()
            .Where(i =>
                i.Plan.PatientId == request.PatientId &&
                i.Status == TreatmentItemStatus.Completed);

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            treatmentQuery = treatmentQuery.Where(i => i.Plan.BranchId == _tenant.BranchId.Value);
        else if (_tenant.IsCompanyAdmin && _tenant.CompanyId.HasValue)
            treatmentQuery = treatmentQuery.Where(i => i.Plan.Branch.CompanyId == _tenant.CompanyId.Value);

        var totalTreatment = await treatmentQuery
            .SumAsync(i => (decimal?)i.FinalPrice, cancellationToken) ?? 0m;

        // Toplam ödeme (iade edilmemiş)
        var paymentQuery = _db.Payments
            .AsNoTracking()
            .Where(p => p.PatientId == request.PatientId && !p.IsRefunded);

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            paymentQuery = paymentQuery.Where(p => p.BranchId == _tenant.BranchId.Value);
        else if (_tenant.IsCompanyAdmin && _tenant.CompanyId.HasValue)
            paymentQuery = paymentQuery.Where(p => p.Branch.CompanyId == _tenant.CompanyId.Value);

        var totalPaid = await paymentQuery
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        // Toplam dağıtılan (iade edilmemiş, hasta ödemesine bağlı allocation'lar)
        var totalAllocated = await _db.PaymentAllocations
            .AsNoTracking()
            .Where(a =>
                a.PaymentId.HasValue &&
                !a.IsRefunded &&
                a.Payment!.PatientId == request.PatientId)
            .SumAsync(a => (decimal?)a.AllocatedAmount, cancellationToken) ?? 0m;

        // Bakiye = Ödenen − Tedavi Tutarı
        // Pozitif = ALACAK (hasta fazla ödedi), Negatif = BORÇ
        var balance = totalPaid - totalTreatment;
        var balanceLabel = balance switch
        {
            > 0 => $"+{balance:N2} TL (Alacak)",
            < 0 => $"{balance:N2} TL (Borç)",
            _   => "0,00 TL (Dengede)"
        };

        return new PatientBalanceResponse(
            request.PatientId,
            totalTreatment,
            totalPaid,
            totalAllocated,
            balance,
            balanceLabel);
    }
}
