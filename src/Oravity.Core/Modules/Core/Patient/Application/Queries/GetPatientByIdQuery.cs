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
            .Include(p => p.CitizenshipType)
            .Include(p => p.ReferralSource)
            .Include(p => p.AgreementInstitution)
            .Include(p => p.InsuranceInstitution)
            .Where(p => p.PublicId == request.PublicId);

        query = ApplyTenantFilter(query);

        var patient = await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Hasta bulunamadı: {request.PublicId}");

        var canViewContact = await CanViewContactAsync(cancellationToken);
        return PatientMappings.ToResponse(patient, canViewContact);
    }

    private async Task<bool> CanViewContactAsync(CancellationToken ct)
    {
        if (_tenantContext.IsPlatformAdmin) return true;

        var hasRole = await _db.UserRoleAssignments
            .Where(a => a.UserId == _tenantContext.UserId
                        && a.IsActive
                        && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
            .SelectMany(a => a.RoleTemplate.RoleTemplatePermissions)
            .AnyAsync(rtp => rtp.Permission.Code == "patient.view_contact", ct);

        if (hasRole) return true;

        return await _db.UserPermissionOverrides
            .AnyAsync(o => o.UserId == _tenantContext.UserId
                           && o.Permission.Code == "patient.view_contact"
                           && o.IsGranted, ct);
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
