using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Treatment.Application.Queries;

public record GetTreatmentByIdQuery(Guid PublicId) : IRequest<TreatmentResponse>;

public class GetTreatmentByIdQueryHandler
    : IRequestHandler<GetTreatmentByIdQuery, TreatmentResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public GetTreatmentByIdQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<TreatmentResponse> Handle(
        GetTreatmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Treatments
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.PublicId == request.PublicId);

        if (!_tenant.IsPlatformAdmin)
        {
            if (_tenant.CompanyId.HasValue)
                query = query.Where(t => t.CompanyId == _tenant.CompanyId.Value);
            else
                throw new ForbiddenException("Erişim için şirket bağlamı gereklidir.");
        }

        var treatment = await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Tedavi bulunamadı.");

        return TreatmentCatalogMappings.ToResponse(treatment);
    }
}
