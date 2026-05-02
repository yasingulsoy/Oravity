using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Consent.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Consent.Application.Queries;

// ── Şablon listesi ────────────────────────────────────────────────────────────

public record GetConsentFormTemplatesQuery(bool ActiveOnly = false) : IRequest<IReadOnlyList<ConsentFormTemplateSummary>>;

public class GetConsentFormTemplatesQueryHandler
    : IRequestHandler<GetConsentFormTemplatesQuery, IReadOnlyList<ConsentFormTemplateSummary>>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public GetConsentFormTemplatesQueryHandler(AppDbContext db, ITenantContext tenant)
        => (_db, _tenant) = (db, tenant);

    public async Task<IReadOnlyList<ConsentFormTemplateSummary>> Handle(
        GetConsentFormTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenant.CompanyId
            ?? throw new InvalidOperationException("Şirket bağlamı bulunamadı.");

        var q = _db.ConsentFormTemplates.AsNoTracking()
            .Where(t => t.CompanyId == companyId);

        if (request.ActiveOnly)
            q = q.Where(t => t.IsActive);

        var list = await q.OrderBy(t => t.Name).ToListAsync(cancellationToken);
        return list.Select(ConsentMappings.ToSummary).ToList();
    }
}

// ── Şablon detayı ─────────────────────────────────────────────────────────────

public record GetConsentFormTemplateByIdQuery(Guid PublicId) : IRequest<ConsentFormTemplateResponse>;

public class GetConsentFormTemplateByIdQueryHandler
    : IRequestHandler<GetConsentFormTemplateByIdQuery, ConsentFormTemplateResponse>
{
    private readonly AppDbContext _db;

    public GetConsentFormTemplateByIdQueryHandler(AppDbContext db) => _db = db;

    public async Task<ConsentFormTemplateResponse> Handle(
        GetConsentFormTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _db.ConsentFormTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.PublicId == request.PublicId, cancellationToken)
            ?? throw new NotFoundException("Onam formu şablonu bulunamadı.");

        return ConsentMappings.ToResponse(template);
    }
}

// ── Plan için onam örnekleri ──────────────────────────────────────────────────

public record GetConsentInstancesByPlanQuery(Guid PlanPublicId) : IRequest<IReadOnlyList<ConsentInstanceResponse>>;

public class GetConsentInstancesByPlanQueryHandler
    : IRequestHandler<GetConsentInstancesByPlanQuery, IReadOnlyList<ConsentInstanceResponse>>
{
    private readonly AppDbContext _db;

    public GetConsentInstancesByPlanQueryHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ConsentInstanceResponse>> Handle(
        GetConsentInstancesByPlanQuery request,
        CancellationToken cancellationToken)
    {
        var plan = await _db.TreatmentPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PublicId == request.PlanPublicId, cancellationToken)
            ?? throw new NotFoundException("Tedavi planı bulunamadı.");

        var instances = await _db.ConsentInstances.AsNoTracking()
            .Where(ci => ci.TreatmentPlanId == plan.Id)
            .OrderByDescending(ci => ci.CreatedAt)
            .ToListAsync(cancellationToken);

        var templateIds = instances.Select(ci => ci.FormTemplateId).Distinct().ToList();
        var templates   = await _db.ConsentFormTemplates.AsNoTracking()
            .Where(t => templateIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        return instances
            .Where(ci => templates.ContainsKey(ci.FormTemplateId))
            .Select(ci => ConsentMappings.ToResponse(ci, templates[ci.FormTemplateId], plan.PublicId))
            .ToList();
    }
}

// ── Hasta için tüm onam örnekleri ────────────────────────────────────────────

public record GetPatientConsentInstancesQuery(Guid PatientPublicId) : IRequest<IReadOnlyList<ConsentInstanceResponse>>;

public class GetPatientConsentInstancesQueryHandler
    : IRequestHandler<GetPatientConsentInstancesQuery, IReadOnlyList<ConsentInstanceResponse>>
{
    private readonly AppDbContext _db;

    public GetPatientConsentInstancesQueryHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ConsentInstanceResponse>> Handle(
        GetPatientConsentInstancesQuery request,
        CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PublicId == request.PatientPublicId, cancellationToken)
            ?? throw new NotFoundException("Hasta bulunamadı.");

        var instances = await _db.ConsentInstances.AsNoTracking()
            .Include(ci => ci.TreatmentPlan)
            .Where(ci => ci.PatientId == patient.Id)
            .OrderByDescending(ci => ci.CreatedAt)
            .ToListAsync(cancellationToken);

        var templateIds = instances.Select(ci => ci.FormTemplateId).Distinct().ToList();
        var templates   = await _db.ConsentFormTemplates.AsNoTracking()
            .Where(t => templateIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        return instances
            .Where(ci => templates.ContainsKey(ci.FormTemplateId))
            .Select(ci => ConsentMappings.ToResponse(
                ci,
                templates[ci.FormTemplateId],
                ci.TreatmentPlan?.PublicId))
            .ToList();
    }
}

// ── Token ile onam formu getir (public) ────────────────────────────────────────

public record GetConsentByTokenQuery(string Token) : IRequest<ConsentPublicDto?>;

public record ConsentPublicDto(
    string ConsentCode,
    string Status,
    string FormTemplateName,
    string FormContentHtml,
    string CheckboxesJson,
    bool   ShowDentalChart,
    bool   ShowTreatmentTable,
    bool   RequireDoctorSignature,
    string ItemPublicIdsJson,
    long   PatientId,
    string PatientName,
    DateTime? SignedAt,
    string? SignerName
);

public class GetConsentByTokenQueryHandler
    : IRequestHandler<GetConsentByTokenQuery, ConsentPublicDto?>
{
    private readonly AppDbContext       _db;
    private readonly IEncryptionService _encryption;

    public GetConsentByTokenQueryHandler(AppDbContext db, IEncryptionService encryption)
    {
        _db         = db;
        _encryption = encryption;
    }

    public async Task<ConsentPublicDto?> Handle(
        GetConsentByTokenQuery request,
        CancellationToken cancellationToken)
    {
        var instance = await _db.ConsentInstances
            .Include(ci => ci.FormTemplate)
            .Include(ci => ci.Patient).ThenInclude(p => p!.Branch).ThenInclude(b => b.Company)
            .Include(ci => ci.TreatmentPlan).ThenInclude(p => p!.Doctor)
            .FirstOrDefaultAsync(
                ci => ci.QrToken == request.Token || ci.SmsToken == request.Token,
                cancellationToken);

        if (instance is null) return null;

        // Süresi dolmuş mu kontrol et
        if (instance.Status == ConsentInstanceStatus.Pending && !instance.IsTokenValid(request.Token))
        {
            instance.MarkExpired();
            await _db.SaveChangesAsync(cancellationToken);
        }

        var tpl     = instance.FormTemplate!;
        var patient = instance.Patient!;
        var name    = $"{patient.FirstName} {patient.LastName}";

        // TC Kimlik No çöz (şifreli)
        var tcNo = string.Empty;
        if (!string.IsNullOrWhiteSpace(patient.TcNumberEncrypted))
        {
            try { tcNo = _encryption.Decrypt(patient.TcNumberEncrypted); }
            catch { tcNo = string.Empty; }
        }

        var contentWithVars = ApplyVariables(tpl.ContentHtml, new TemplateVars(
            HastaAdSoyad:   name,
            TCKimlikNo:     tcNo,
            HastaTelefon:   patient.Phone ?? string.Empty,
            HastaYas:       patient.BirthDate.HasValue
                                ? (DateTime.UtcNow.Year - patient.BirthDate.Value.Year).ToString()
                                : string.Empty,
            HastaDogumTarihi: patient.BirthDate?.ToString("dd.MM.yyyy") ?? string.Empty,
            Hekim:          instance.TreatmentPlan?.Doctor?.FullName ?? string.Empty,
            Klinik:         patient.Branch?.Name ?? string.Empty,
            Sirket:         patient.Branch?.Company?.Name ?? string.Empty,
            Tarih:          DateTime.UtcNow.ToString("dd.MM.yyyy"),
            FormNo:         instance.ConsentCode,
            AnneAdi:        patient.MotherName ?? string.Empty,
            BabaAdi:        patient.FatherName ?? string.Empty,
            Adres:          patient.Address ?? string.Empty
        ));

        return new ConsentPublicDto(
            instance.ConsentCode,
            ConsentMappings.StatusLabel(instance.Status),
            tpl.Name,
            contentWithVars,
            tpl.CheckboxesJson,
            tpl.ShowDentalChart,
            tpl.ShowTreatmentTable,
            tpl.RequireDoctorSignature,
            instance.ItemPublicIdsJson,
            patient.Id,
            name,
            instance.SignedAt,
            instance.SignerName
        );
    }

    private record TemplateVars(
        string HastaAdSoyad, string TCKimlikNo, string HastaTelefon,
        string HastaYas, string HastaDogumTarihi,
        string Hekim, string Klinik, string Sirket,
        string Tarih, string FormNo,
        string AnneAdi, string BabaAdi, string Adres);

    private static string ApplyVariables(string html, TemplateVars v)
    {
        var sb = new StringBuilder(html)
            .Replace("{HastaAdSoyad}",    v.HastaAdSoyad)
            .Replace("{TCKimlikNo}",       v.TCKimlikNo)
            .Replace("{HastaTelefon}",     v.HastaTelefon)
            .Replace("{HastaYas}",         v.HastaYas)
            .Replace("{HastaDogumTarihi}", v.HastaDogumTarihi)
            .Replace("{Hekim}",            v.Hekim)
            .Replace("{Klinik}",           v.Klinik)
            .Replace("{Sirket}",           v.Sirket)
            .Replace("{Tarih}",            v.Tarih)
            .Replace("{FormNo}",           v.FormNo)
            .Replace("{AnneAdi}",          v.AnneAdi)
            .Replace("{BabaAdi}",          v.BabaAdi)
            .Replace("{Adres}",            v.Adres);
        return sb.ToString();
    }
}
