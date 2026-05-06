using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Commission.Application.Commands;

public record UpdateCommissionTemplateCommand(
    Guid PublicId,
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
    bool IsActive,
    IReadOnlyList<JobStartPriceRequest>? JobStartPrices,
    IReadOnlyList<PriceRangeRequest>? PriceRanges
) : IRequest<CommissionTemplateResponse>;

public class UpdateCommissionTemplateCommandHandler
    : IRequestHandler<UpdateCommissionTemplateCommand, CommissionTemplateResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public UpdateCommissionTemplateCommandHandler(
        AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    public async Task<CommissionTemplateResponse> Handle(
        UpdateCommissionTemplateCommand r, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct)
            ?? throw new ForbiddenException("Şirket bağlamı gereklidir.");

        var template = await _db.DoctorCommissionTemplates
            .Include(t => t.JobStartPrices)
            .Include(t => t.PriceRanges)
            .FirstOrDefaultAsync(t => t.PublicId == r.PublicId && t.CompanyId == companyId, ct)
            ?? throw new NotFoundException($"Şablon bulunamadı: {r.PublicId}");

        var nameNorm = r.Name.Trim();
        var nameClash = await _db.DoctorCommissionTemplates.AsNoTracking()
            .AnyAsync(t => t.CompanyId == companyId && t.Id != template.Id && t.Name == nameNorm, ct);
        if (nameClash)
            throw new ConflictException($"'{nameNorm}' adında bir şablon zaten mevcut.");

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

        template.SetActive(r.IsActive);

        if (_user.IsAuthenticated)
            template.SetUpdatedBy(_user.UserId);

        // İş başı fiyatları: gelen liste varsa tümünü sil+yeniden yaz
        if (r.JobStartPrices != null)
        {
            _db.TemplateJobStartPrices.RemoveRange(template.JobStartPrices);
            foreach (var jp in r.JobStartPrices)
                _db.TemplateJobStartPrices.Add(
                    TemplateJobStartPrice.Create(template.Id, jp.TreatmentId, jp.PriceType, jp.Value));
        }

        // Fiyat bantları: gelen liste varsa tümünü sil+yeniden yaz
        if (r.PriceRanges != null)
        {
            _db.TemplatePriceRanges.RemoveRange(template.PriceRanges);
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
