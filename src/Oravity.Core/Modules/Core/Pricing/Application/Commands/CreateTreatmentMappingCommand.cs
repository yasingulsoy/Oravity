using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Pricing.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Pricing.Application.Commands;

public record CreateTreatmentMappingCommand(
    Guid    TreatmentPublicId,
    long    ReferenceListId,
    string  ReferenceCode,
    string? MappingQuality,
    string? Notes
) : IRequest<TreatmentMappingResponse>;

public class CreateTreatmentMappingCommandHandler
    : IRequestHandler<CreateTreatmentMappingCommand, TreatmentMappingResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public CreateTreatmentMappingCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<TreatmentMappingResponse> Handle(
        CreateTreatmentMappingCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenant.CompanyId
            ?? throw new ForbiddenException("Eşleştirme oluşturmak için şirket bağlamı gereklidir.");

        var treatment = await _db.Treatments
            .FirstOrDefaultAsync(t => t.PublicId == request.TreatmentPublicId
                                   && t.CompanyId == companyId, cancellationToken)
            ?? throw new NotFoundException("Tedavi bulunamadı.");

        var refList = await _db.ReferencePriceLists
            .FirstOrDefaultAsync(l => l.Id == request.ReferenceListId, cancellationToken)
            ?? throw new NotFoundException("Referans fiyat listesi bulunamadı.");

        var existing = await _db.TreatmentMappings
            .AnyAsync(m => m.InternalTreatmentId == treatment.Id
                        && m.ReferenceListId == refList.Id, cancellationToken);
        if (existing)
            throw new ConflictException("Bu tedavi için bu referans listesinde zaten bir eşleştirme var.");

        var mapping = TreatmentMapping.Create(
            treatment.Id,
            refList.Id,
            request.ReferenceCode,
            request.MappingQuality,
            request.Notes);

        _db.TreatmentMappings.Add(mapping);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(mapping).Reference(m => m.InternalTreatment).LoadAsync(cancellationToken);
        await _db.Entry(mapping).Reference(m => m.ReferenceList).LoadAsync(cancellationToken);

        return PricingMappings.ToResponse(mapping);
    }
}
