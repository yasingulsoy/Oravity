using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Treatment.Application.Commands;

/// <summary>
/// Tedavi kalemi tamamlandı olarak işaretlenir.
/// Dövizli kalemlerde yapıldı anındaki TCMB kuru kilitlenir → PriceBaseAmount güncellenir.
/// İzin: treatment_plan:complete
/// </summary>
public record CompleteTreatmentPlanItemCommand(Guid ItemPublicId) : IRequest<TreatmentPlanItemResponse>;

public class CompleteTreatmentPlanItemCommandHandler
    : IRequestHandler<CompleteTreatmentPlanItemCommand, TreatmentPlanItemResponse>
{
    private readonly AppDbContext          _db;
    private readonly ITenantContext        _tenant;
    private readonly IExchangeRateService  _exchangeRates;

    public CompleteTreatmentPlanItemCommandHandler(
        AppDbContext db,
        ITenantContext tenant,
        IExchangeRateService exchangeRates)
    {
        _db            = db;
        _tenant        = tenant;
        _exchangeRates = exchangeRates;
    }

    public async Task<TreatmentPlanItemResponse> Handle(
        CompleteTreatmentPlanItemCommand request,
        CancellationToken cancellationToken)
    {
        var item = await _db.TreatmentPlanItems
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(i => i.PublicId == request.ItemPublicId, cancellationToken)
            ?? throw new NotFoundException($"Tedavi kalemi bulunamadı: {request.ItemPublicId}");

        EnsureTenantAccess(item.Plan);

        // Onam kontrolü: imzalı consent yoksa bypass izni gerekir
        await EnsureConsentAsync(item.PublicId, cancellationToken);

        // Dövizli kalem: yapıldı anındaki kuru kilitle
        decimal? completionRate = null;
        if (item.PriceCurrency != "TRY")
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            completionRate = await _exchangeRates.GetRate(
                item.PriceCurrency,
                today,
                _tenant.CompanyId,
                _tenant.BranchId,
                cancellationToken);
        }

        item.Complete(completionRate: completionRate);
        await _db.SaveChangesAsync(cancellationToken);

        // Outbox: TreatmentItemCompleted event
        var payload = JsonSerializer.Serialize(new
        {
            item.PublicId,
            item.PlanId,
            item.TreatmentId,
            item.DoctorId,
            item.CompletedAt,
            item.FinalPrice
        });
        _db.OutboxMessages.Add(OutboxMessage.Create("TreatmentItemCompleted", payload));
        await _db.SaveChangesAsync(cancellationToken);

        return TreatmentPlanMappings.ToResponse(item);
    }

    private void EnsureTenantAccess(TreatmentPlan plan)
    {
        if (_tenant.IsPlatformAdmin) return;
        if (_tenant.IsBranchLevel && plan.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu tedavi kalemine erişim yetkiniz bulunmuyor.");
    }

    private async Task EnsureConsentAsync(Guid itemPublicId, CancellationToken ct)
    {
        // Platform admin her zaman geçebilir
        if (_tenant.IsPlatformAdmin) return;

        var itemIdStr = itemPublicId.ToString();

        // İmzalı onam var mı?
        // Not: ItemPublicIdsJson jsonb sütunu olduğundan LIKE çalışmaz; string olarak çekip filtrele.
        var signedJsons = await _db.ConsentInstances
            .AsNoTracking()
            .Where(ci => ci.Status == ConsentInstanceStatus.Signed)
            .Select(ci => ci.ItemPublicIdsJson)
            .ToListAsync(ct);
        var hasSignedConsent = signedJsons.Any(json => json.Contains(itemIdStr, StringComparison.OrdinalIgnoreCase));

        if (hasSignedConsent) return;

        // İmzalı onam yok — bypass izni var mı?
        const string bypassPermCode = "treatment_plan.complete_without_consent";

        var hasOverride = await _db.UserPermissionOverrides
            .AnyAsync(o => o.UserId == _tenant.UserId
                           && o.Permission.Code == bypassPermCode
                           && o.IsGranted, ct);
        if (hasOverride) return;

        var hasRolePermission = await _db.UserRoleAssignments
            .Where(a => a.UserId == _tenant.UserId
                        && a.IsActive
                        && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
            .SelectMany(a => a.RoleTemplate.RoleTemplatePermissions)
            .AnyAsync(rtp => rtp.Permission.Code == bypassPermCode, ct);

        if (!hasRolePermission)
            throw new ForbiddenException("Bu tedavi için onam formu alınmadan tamamlanamaz.");
    }
}
