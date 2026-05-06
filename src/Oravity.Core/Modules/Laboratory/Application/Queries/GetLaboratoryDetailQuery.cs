using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Laboratory.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Queries;

public record GetLaboratoryDetailQuery(Guid PublicId)
    : IRequest<LaboratoryDetailResponse>;

public record LaboratoryDetailResponse(
    LaboratoryResponse                            Laboratory,
    IReadOnlyList<BranchAssignmentResponse>       BranchAssignments,
    IReadOnlyList<LaboratoryPriceItemResponse>    PriceItems
);

public class GetLaboratoryDetailQueryHandler
    : IRequestHandler<GetLaboratoryDetailQuery, LaboratoryDetailResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public GetLaboratoryDetailQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<LaboratoryDetailResponse> Handle(
        GetLaboratoryDetailQuery request,
        CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct)
            ?? throw new ForbiddenException("Şirket bağlamı gerekli.");

        var lab = await _db.Laboratories.AsNoTracking()
            .FirstOrDefaultAsync(l => l.PublicId == request.PublicId
                                       && l.CompanyId == companyId, ct)
            ?? throw new NotFoundException("Laboratuvar bulunamadı.");

        var assignments = await _db.LaboratoryBranchAssignments.AsNoTracking()
            .Include(a => a.Branch)
            .Where(a => a.LaboratoryId == lab.Id)
            .OrderBy(a => a.Priority)
            .Select(a => new BranchAssignmentResponse(
                a.PublicId, a.Branch.PublicId, a.Branch.Name,
                a.Priority, a.IsActive))
            .ToListAsync(ct);

        var prices = await _db.LaboratoryPriceItems.AsNoTracking()
            .Where(p => p.LaboratoryId == lab.Id)
            .OrderBy(p => p.Category).ThenBy(p => p.ItemName)
            .Select(p => new LaboratoryPriceItemResponse(
                p.PublicId, p.ItemName, p.ItemCode, p.Description,
                p.Price, p.Currency, p.PricingType, p.EstimatedDeliveryDays, p.Category,
                p.ValidFrom, p.ValidUntil, p.IsActive))
            .ToListAsync(ct);

        var activeAssignments = assignments.Where(a => a.IsActive).ToList();
        var workCount = await _db.LaboratoryWorks.AsNoTracking()
            .CountAsync(w => w.LaboratoryId == lab.Id, ct);

        var labDto = new LaboratoryResponse(
            lab.PublicId, lab.Name, lab.Code, lab.Phone, lab.Email, lab.Website,
            lab.Country, lab.City, lab.District, lab.Address,
            lab.ContactPerson, lab.ContactPhone,
            lab.WorkingDays, lab.WorkingHours, lab.PaymentTerms, lab.PaymentDays,
            lab.Notes, lab.IsActive, activeAssignments.Count, workCount, lab.CreatedAt,
            activeAssignments.Select(a => a.BranchName).ToList());

        return new LaboratoryDetailResponse(labDto, assignments, prices);
    }
}
