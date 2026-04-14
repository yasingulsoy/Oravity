using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Pricing.Application.Commands;

public record UpdateBranchPricingMultiplierCommand(
    long    BranchId,
    decimal PricingMultiplier
) : IRequest<Oravity.Core.Modules.Core.Pricing.Application.Queries.BranchPricingResponse>;

public class UpdateBranchPricingMultiplierCommandHandler
    : IRequestHandler<UpdateBranchPricingMultiplierCommand,
      Oravity.Core.Modules.Core.Pricing.Application.Queries.BranchPricingResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public UpdateBranchPricingMultiplierCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<Oravity.Core.Modules.Core.Pricing.Application.Queries.BranchPricingResponse> Handle(
        UpdateBranchPricingMultiplierCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, cancellationToken)
            ?? throw new ForbiddenException("Şirket bağlamı gereklidir.");

        var branch = await _db.Branches
            .FirstOrDefaultAsync(b => b.Id == request.BranchId && b.CompanyId == companyId, cancellationToken)
            ?? throw new NotFoundException("Şube bulunamadı.");

        branch.SetPricingMultiplier(request.PricingMultiplier);
        await _db.SaveChangesAsync(cancellationToken);

        return new Oravity.Core.Modules.Core.Pricing.Application.Queries.BranchPricingResponse(
            branch.Id, branch.Name, branch.PricingMultiplier);
    }
}
