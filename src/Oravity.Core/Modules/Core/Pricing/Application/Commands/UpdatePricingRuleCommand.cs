using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.Pricing.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Core.Pricing.Application.Commands;

public record UpdatePricingRuleCommand(
    Guid      PublicId,
    string    Name,
    string?   Description,
    string    RuleType,
    int       Priority,
    string?   IncludeFilters,
    string?   ExcludeFilters,
    string?   Formula,
    string    OutputCurrency,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    bool      StopProcessing,
    bool      IsActive
) : IRequest<PricingRuleResponse>;

public class UpdatePricingRuleCommandHandler
    : IRequestHandler<UpdatePricingRuleCommand, PricingRuleResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public UpdatePricingRuleCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<PricingRuleResponse> Handle(
        UpdatePricingRuleCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, cancellationToken)
            ?? throw new ForbiddenException("Fiyatlandırma kuralı güncellemek için şirket bağlamı gereklidir.");

        var rule = await _db.PricingRules
            .FirstOrDefaultAsync(r => r.PublicId == request.PublicId
                                   && r.CompanyId == companyId, cancellationToken)
            ?? throw new NotFoundException("Fiyatlandırma kuralı bulunamadı.");

        rule.Update(
            request.Name,
            request.Description,
            request.RuleType,
            request.Priority,
            request.IncludeFilters,
            request.ExcludeFilters,
            request.Formula,
            request.OutputCurrency,
            request.ValidFrom.HasValue ? DateTime.SpecifyKind(request.ValidFrom.Value, DateTimeKind.Utc) : null,
            request.ValidUntil.HasValue ? DateTime.SpecifyKind(request.ValidUntil.Value, DateTimeKind.Utc) : null,
            request.StopProcessing);

        rule.SetActive(request.IsActive);

        await _db.SaveChangesAsync(cancellationToken);
        return PricingMappings.ToResponse(rule);
    }
}
