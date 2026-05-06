using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Commission.Application.Commands;

public record CreateCommissionTemplateCommand(
    string Name,
    CommissionWorkingStyle WorkingStyle,
    CommissionPaymentType PaymentType,
    decimal FixedFee,
    decimal PrimRate,
    bool InstitutionPayOnInvoice,
    JobStartCalculation? JobStartCalculation,
    bool ClinicTargetEnabled,
    decimal? ClinicTargetBonusRate,
    bool DoctorTargetEnabled,
    decimal? DoctorTargetBonusRate,
    bool DeductTreatmentPlanCommission,
    bool DeductLabCost,
    bool DeductTreatmentCost,
    bool RequireLabApproval,
    bool KdvEnabled,
    decimal? KdvRate,
    string? KdvAppliedPaymentTypes,
    bool ExtraExpenseEnabled,
    decimal? ExtraExpenseRate,
    bool WithholdingTaxEnabled,
    decimal? WithholdingTaxRate,
    IReadOnlyList<JobStartPriceRequest>? JobStartPrices,
    IReadOnlyList<PriceRangeRequest>? PriceRanges
) : IRequest<CommissionTemplateResponse>;

public class CreateCommissionTemplateCommandHandler
    : IRequestHandler<CreateCommissionTemplateCommand, CommissionTemplateResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public CreateCommissionTemplateCommandHandler(
        AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    public async Task<CommissionTemplateResponse> Handle(
        CreateCommissionTemplateCommand r, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct)
            ?? throw new ForbiddenException("Şablon oluşturmak için şirket bağlamı gereklidir.");

        var nameNorm = r.Name.Trim();
        var exists = await _db.DoctorCommissionTemplates.AsNoTracking()
            .AnyAsync(t => t.CompanyId == companyId && t.Name == nameNorm, ct);
        if (exists)
            throw new ConflictException($"'{nameNorm}' adında bir şablon zaten mevcut.");

        var template = DoctorCommissionTemplate.Create(
            companyId, nameNorm, r.WorkingStyle, r.PaymentType,
            r.FixedFee, r.PrimRate, r.InstitutionPayOnInvoice);

        template.UpdateBasics(
            nameNorm, r.WorkingStyle, r.PaymentType, r.FixedFee, r.PrimRate,
            r.InstitutionPayOnInvoice, r.JobStartCalculation);

        template.UpdateTargets(
            r.ClinicTargetEnabled, r.ClinicTargetBonusRate,
            r.DoctorTargetEnabled, r.DoctorTargetBonusRate);

        template.UpdateDeductions(
            r.DeductTreatmentPlanCommission, r.DeductLabCost, r.DeductTreatmentCost,
            r.RequireLabApproval,
            r.KdvEnabled, r.KdvRate, r.KdvAppliedPaymentTypes,
            r.ExtraExpenseEnabled, r.ExtraExpenseRate,
            r.WithholdingTaxEnabled, r.WithholdingTaxRate);

        if (_user.IsAuthenticated)
            template.SetCreatedBy(_user.UserId, _user.TenantId);

        _db.DoctorCommissionTemplates.Add(template);
        await _db.SaveChangesAsync(ct);

        if (r.JobStartPrices?.Count > 0)
        {
            foreach (var jp in r.JobStartPrices)
                _db.TemplateJobStartPrices.Add(
                    TemplateJobStartPrice.Create(template.Id, jp.TreatmentId, jp.PriceType, jp.Value));
        }

        if (r.PriceRanges?.Count > 0)
        {
            foreach (var pr in r.PriceRanges)
                _db.TemplatePriceRanges.Add(
                    TemplatePriceRange.Create(template.Id, pr.MinAmount, pr.MaxAmount, pr.Rate));
        }

        await _db.SaveChangesAsync(ct);

        var reloaded = await _db.DoctorCommissionTemplates
            .Include(t => t.JobStartPrices)
            .Include(t => t.PriceRanges)
            .FirstAsync(t => t.Id == template.Id, ct);

        return CommissionMappings.ToResponse(reloaded);
    }
}
