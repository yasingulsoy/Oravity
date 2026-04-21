using MediatR;
using Oravity.Core.Modules.Core.Pricing.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Core.Pricing.Application.Commands;

public record CreatePricingRuleCommand(
    long?     BranchId,
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
    bool      StopProcessing
) : IRequest<PricingRuleResponse>;

public class CreatePricingRuleCommandHandler
    : IRequestHandler<CreatePricingRuleCommand, PricingRuleResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser   _user;

    public CreatePricingRuleCommandHandler(AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db     = db;
        _tenant = tenant;
        _user   = user;
    }

    public async Task<PricingRuleResponse> Handle(
        CreatePricingRuleCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, cancellationToken)
            ?? throw new ForbiddenException("Fiyatlandırma kuralı oluşturmak için şirket bağlamı gereklidir.");

        var rule = PricingRule.Create(
            companyId,
            request.BranchId,
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
            request.StopProcessing,
            _user.IsAuthenticated ? _user.UserId : null);

        _db.PricingRules.Add(rule);
        await _db.SaveChangesAsync(cancellationToken);

        return PricingMappings.ToResponse(rule);
    }
}
