using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Patient.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Patient.Application.Queries;

public record GetPatientByIdQuery(Guid PublicId) : IRequest<PatientResponse>;

public class GetPatientByIdQueryHandler : IRequestHandler<GetPatientByIdQuery, PatientResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetPatientByIdQueryHandler(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<PatientResponse> Handle(
        GetPatientByIdQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Patients.AsNoTracking()
            .Where(p => p.PublicId == request.PublicId);

        query = ApplyTenantFilter(query);

        var patient = await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Hasta bulunamadı: {request.PublicId}");

        return PatientMappings.ToResponse(patient);
    }

    private IQueryable<SharedKernel.Entities.Patient> ApplyTenantFilter(
        IQueryable<SharedKernel.Entities.Patient> query)
    {
        if (_tenantContext.IsPlatformAdmin) return query;

        if (_tenantContext.IsBranchLevel && _tenantContext.BranchId.HasValue)
            return query.Where(p => p.BranchId == _tenantContext.BranchId.Value);

        if (_tenantContext.IsCompanyAdmin && _tenantContext.CompanyId.HasValue)
            return query.Where(p => p.Branch.CompanyId == _tenantContext.CompanyId.Value);

        return query.Where(_ => false);
    }
}
