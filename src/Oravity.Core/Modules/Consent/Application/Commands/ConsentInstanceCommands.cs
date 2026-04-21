using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Consent.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using System.Text.Json;

namespace Oravity.Core.Modules.Consent.Application.Commands;

// ── Onam örneği oluştur ────────────────────────────────────────────────────────

public record CreateConsentInstanceCommand(
    Guid   TreatmentPlanPublicId,
    Guid   FormTemplatePublicId,
    /// <summary>Kapsanan tedavi kalemi publicId listesi.</summary>
    List<string> ItemPublicIds,
    /// <summary>qr | sms | both</summary>
    string DeliveryMethod
) : IRequest<ConsentInstanceResponse>;

public class CreateConsentInstanceCommandHandler
    : IRequestHandler<CreateConsentInstanceCommand, ConsentInstanceResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public CreateConsentInstanceCommandHandler(AppDbContext db, ITenantContext tenant)
        => (_db, _tenant) = (db, tenant);

    public async Task<ConsentInstanceResponse> Handle(
        CreateConsentInstanceCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenant.CompanyId
            ?? throw new InvalidOperationException("Şirket bağlamı bulunamadı.");

        var plan = await _db.TreatmentPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PublicId == request.TreatmentPlanPublicId, cancellationToken)
            ?? throw new NotFoundException("Tedavi planı bulunamadı.");

        var template = await _db.ConsentFormTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.PublicId == request.FormTemplatePublicId, cancellationToken)
            ?? throw new NotFoundException("Onam formu şablonu bulunamadı.");

        // Benzersiz consent kodu üret: CF-2026-NNNNN
        var year    = DateTime.UtcNow.Year;
        var lastSeq = await _db.ConsentInstances
            .Where(ci => ci.ConsentCode.StartsWith($"CF-{year}-"))
            .CountAsync(cancellationToken);
        var code = $"CF-{year}-{(lastSeq + 1):D5}";

        var delivery = request.DeliveryMethod.ToLowerInvariant() switch
        {
            "sms"  => "sms",
            "both" => "both",
            _      => "qr",
        };

        var itemJson = JsonSerializer.Serialize(request.ItemPublicIds);

        var instance = ConsentInstance.Create(
            companyId,
            plan.PatientId,
            plan.Id,
            template.Id,
            code,
            itemJson,
            delivery,
            _tenant.UserId);

        _db.ConsentInstances.Add(instance);
        await _db.SaveChangesAsync(cancellationToken);

        return ConsentMappings.ToResponse(instance, template);
    }
}

// ── Onam formunu imzala (public, anonim) ────────────────────────────────────────

public record SignConsentInstanceCommand(
    string Token,
    string? SignerName,
    string? SignatureDataBase64,
    string? CheckboxAnswersJson,
    string? SignerIp,
    string? SignerDevice
) : IRequest<SignConsentResult>;

public record SignConsentResult(bool Success, string Message);

public class SignConsentInstanceCommandHandler
    : IRequestHandler<SignConsentInstanceCommand, SignConsentResult>
{
    private readonly AppDbContext _db;

    public SignConsentInstanceCommandHandler(AppDbContext db) => _db = db;

    public async Task<SignConsentResult> Handle(
        SignConsentInstanceCommand request,
        CancellationToken cancellationToken)
    {
        // Token ile ara (QR veya SMS)
        var instance = await _db.ConsentInstances
            .FirstOrDefaultAsync(
                ci => ci.QrToken == request.Token || ci.SmsToken == request.Token,
                cancellationToken);

        if (instance is null)
            return new SignConsentResult(false, "Geçersiz form bağlantısı.");

        if (instance.Status == ConsentInstanceStatus.Signed)
            return new SignConsentResult(false, "Bu form zaten imzalanmış.");

        if (!instance.IsTokenValid(request.Token))
        {
            instance.MarkExpired();
            await _db.SaveChangesAsync(cancellationToken);
            return new SignConsentResult(false, "Form bağlantısının süresi dolmuş.");
        }

        instance.Sign(
            request.SignerName,
            request.SignatureDataBase64,
            request.CheckboxAnswersJson,
            request.SignerIp,
            request.SignerDevice);

        await _db.SaveChangesAsync(cancellationToken);
        return new SignConsentResult(true, "Onam formu başarıyla imzalandı.");
    }
}
