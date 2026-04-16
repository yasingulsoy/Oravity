using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Pricing.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Pricing.Application.Commands;

public record DeletePricingRuleCommand(Guid PublicId) : IRequest;

public class DeletePricingRuleCommandHandler : IRequestHandler<DeletePricingRuleCommand>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public DeletePricingRuleCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task Handle(DeletePricingRuleCommand request, CancellationToken cancellationToken)
    {
        var companyId = await TenantCompanyResolver.ResolveCompanyIdAsync(_tenant, _db, cancellationToken)
            ?? throw new ForbiddenException("Şirket bağlamı gereklidir.");

        var rule = await _db.PricingRules
            .FirstOrDefaultAsync(r => r.PublicId == request.PublicId
                                   && r.CompanyId == companyId, cancellationToken)
            ?? throw new NotFoundException("Kural bulunamadı.");

        _db.PricingRules.Remove(rule);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
