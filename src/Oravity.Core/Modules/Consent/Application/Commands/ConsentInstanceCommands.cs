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
    /// <summary>Null ise tedaviden bağımsız standalone onam formu oluşturulur.</summary>
    Guid?  TreatmentPlanPublicId,
    Guid   FormTemplatePublicId,
    /// <summary>Kapsanan tedavi kalemi publicId listesi. Standalone onam için boş olabilir.</summary>
    List<string> ItemPublicIds,
    /// <summary>qr | sms | both</summary>
    string DeliveryMethod,
    /// <summary>Standalone onam için hasta publicId'si (plan yoksa zorunlu).</summary>
    Guid?  PatientPublicId = null
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

        // Plan-bağlı onam veya standalone onam
        long? planId = null;
        long  patientId;

        if (request.TreatmentPlanPublicId.HasValue)
        {
            var plan = await _db.TreatmentPlans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PublicId == request.TreatmentPlanPublicId.Value, cancellationToken)
                ?? throw new NotFoundException("Tedavi planı bulunamadı.");
            planId    = plan.Id;
            patientId = plan.PatientId;

            // İmzalı onam çakışması — plan varsa kontrol yap
            // Not: ItemPublicIdsJson jsonb kolonu olduğu için LIKE kullanamayız; in-memory kontrol yapıyoruz.
            if (request.ItemPublicIds.Count > 0)
            {
                var signedJsons = await _db.ConsentInstances
                    .AsNoTracking()
                    .Where(ci => ci.TreatmentPlanId == planId && ci.Status == ConsentInstanceStatus.Signed)
                    .Select(ci => ci.ItemPublicIdsJson)
                    .ToListAsync(cancellationToken);

                foreach (var itemId in request.ItemPublicIds)
                {
                    if (signedJsons.Any(json => json.Contains(itemId)))
                        throw new ConflictException(
                            $"Bu tedavi kalemi için zaten imzalı bir onam formu bulunmaktadır ({itemId}). " +
                            "İmzalanmış kalemlere yeni onam formu oluşturulamaz.");
                }
            }
        }
        else if (request.PatientPublicId.HasValue)
        {
            // Standalone: hasta publicId'sinden patientId çöz
            var patient = await _db.Patients.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PublicId == request.PatientPublicId.Value, cancellationToken)
                ?? throw new NotFoundException("Hasta bulunamadı.");
            patientId = patient.Id;
        }
        else
        {
            throw new InvalidOperationException("Onam formu için tedavi planı veya hasta belirtilmelidir.");
        }

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
            patientId,
            planId,
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
    string? DoctorSignatureDataBase64,
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
            request.DoctorSignatureDataBase64,
            request.CheckboxAnswersJson,
            request.SignerIp,
            request.SignerDevice);

        await _db.SaveChangesAsync(cancellationToken);
        return new SignConsentResult(true, "Onam formu başarıyla imzalandı.");
    }
}

// ── Onam örneğini iptal et ─────────────────────────────────────────────────────

public record CancelConsentInstanceCommand(Guid InstancePublicId) : IRequest<ConsentInstanceResponse>;

public class CancelConsentInstanceCommandHandler
    : IRequestHandler<CancelConsentInstanceCommand, ConsentInstanceResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public CancelConsentInstanceCommandHandler(AppDbContext db, ITenantContext tenant)
        => (_db, _tenant) = (db, tenant);

    public async Task<ConsentInstanceResponse> Handle(
        CancelConsentInstanceCommand request,
        CancellationToken cancellationToken)
    {
        var instance = await _db.ConsentInstances
            .Include(ci => ci.FormTemplate)
            .FirstOrDefaultAsync(ci => ci.PublicId == request.InstancePublicId, cancellationToken)
            ?? throw new NotFoundException("Onam formu bulunamadı.");

        if (instance.Status == ConsentInstanceStatus.Cancelled)
            throw new InvalidOperationException("Bu onam formu zaten iptal edilmiş.");

        instance.Cancel();
        await _db.SaveChangesAsync(cancellationToken);

        return ConsentMappings.ToResponse(instance, instance.FormTemplate!);
    }
}
