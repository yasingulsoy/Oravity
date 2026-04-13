using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.DentalChart.Application;
using Oravity.Core.Modules.Core.DentalChart.Domain.Services;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.DentalChart.Application.Queries;

public record GetPatientDentalChartQuery(Guid PatientPublicId) : IRequest<DentalChartResponse>;

public class GetPatientDentalChartQueryHandler
    : IRequestHandler<GetPatientDentalChartQuery, DentalChartResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IFdiChartService _fdi;

    public GetPatientDentalChartQueryHandler(
        AppDbContext db, ITenantContext tenant, IFdiChartService fdi)
    {
        _db = db;
        _tenant = tenant;
        _fdi = fdi;
    }

    public async Task<DentalChartResponse> Handle(
        GetPatientDentalChartQuery request,
        CancellationToken cancellationToken)
    {
        var patientId = await _db.Patients
            .Where(p => p.PublicId == request.PatientPublicId && !p.IsDeleted)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // Tenant filtresi — şube kullanıcısı yalnızca kendi şubesini görebilir
        var query = _db.ToothRecords
            .AsNoTracking()
            .Where(r => r.PatientId == patientId);

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            query = query.Where(r => r.BranchId == _tenant.BranchId.Value);
        else if (_tenant.IsCompanyAdmin && _tenant.CompanyId.HasValue)
            query = query.Where(r => r.CompanyId == _tenant.CompanyId.Value);

        var records = await query
            .OrderBy(r => r.ToothNumber)
            .ToListAsync(cancellationToken);

        var recordMap = records.ToDictionary(r => r.ToothNumber);
        var allTeeth = _fdi.GetAllToothNumbers();

        // 32 diş: kayıt varsa mevcut, yoksa default Sağlıklı
        var teeth = allTeeth
            .Select(no => recordMap.TryGetValue(no, out var rec)
                ? DentalChartMappings.ToResponse(rec)
                : DentalChartMappings.DefaultHealthy(no))
            .ToList();

        return new DentalChartResponse(
            PatientId:     patientId,
            Teeth:         teeth,
            TotalRecorded: records.Count,
            TotalHealthy:  teeth.Count(t =>
                t.Status == SharedKernel.Entities.ToothStatus.Healthy));
    }
}
