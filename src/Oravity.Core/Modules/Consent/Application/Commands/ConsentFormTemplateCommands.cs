using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Consent.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Consent.Application.Commands;

// ── Şablon oluştur ────────────────────────────────────────────────────────────

public record CreateConsentFormTemplateCommand(
    string  Code,
    string  Name,
    string  Language,
    string  Version,
    string  ContentHtml,
    string  CheckboxesJson,
    bool    AppliesToAllTreatments,
    string? TreatmentCategoryIdsJson,
    bool    ShowDentalChart,
    bool    ShowTreatmentTable,
    bool    RequireDoctorSignature
) : IRequest<ConsentFormTemplateResponse>;

public class CreateConsentFormTemplateCommandHandler
    : IRequestHandler<CreateConsentFormTemplateCommand, ConsentFormTemplateResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public CreateConsentFormTemplateCommandHandler(AppDbContext db, ITenantContext tenant)
        => (_db, _tenant) = (db, tenant);

    public async Task<ConsentFormTemplateResponse> Handle(
        CreateConsentFormTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenant.CompanyId
            ?? throw new InvalidOperationException("Şirket bağlamı bulunamadı.");

        var exists = await _db.ConsentFormTemplates
            .AnyAsync(t => t.CompanyId == companyId && t.Code == request.Code.ToUpperInvariant(), cancellationToken);
        if (exists)
            throw new InvalidOperationException($"'{request.Code}' kodu zaten kullanılıyor.");

        var template = ConsentFormTemplate.Create(
            companyId,
            request.Code,
            request.Name,
            request.Language,
            request.Version,
            request.ContentHtml,
            request.CheckboxesJson,
            request.AppliesToAllTreatments,
            request.TreatmentCategoryIdsJson,
            request.ShowDentalChart,
            request.ShowTreatmentTable,
            request.RequireDoctorSignature,
            _tenant.UserId);

        _db.ConsentFormTemplates.Add(template);
        await _db.SaveChangesAsync(cancellationToken);

        return ConsentMappings.ToResponse(template);
    }
}

// ── Şablon güncelle ────────────────────────────────────────────────────────────

public record UpdateConsentFormTemplateCommand(
    Guid    PublicId,
    string  Name,
    string  Language,
    string  Version,
    string  ContentHtml,
    string  CheckboxesJson,
    bool    AppliesToAllTreatments,
    string? TreatmentCategoryIdsJson,
    bool    ShowDentalChart,
    bool    ShowTreatmentTable,
    bool    RequireDoctorSignature
) : IRequest<ConsentFormTemplateResponse>;

public class UpdateConsentFormTemplateCommandHandler
    : IRequestHandler<UpdateConsentFormTemplateCommand, ConsentFormTemplateResponse>
{
    private readonly AppDbContext _db;

    public UpdateConsentFormTemplateCommandHandler(AppDbContext db) => _db = db;

    public async Task<ConsentFormTemplateResponse> Handle(
        UpdateConsentFormTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _db.ConsentFormTemplates
            .FirstOrDefaultAsync(t => t.PublicId == request.PublicId, cancellationToken)
            ?? throw new NotFoundException("Onam formu şablonu bulunamadı.");

        template.Update(
            request.Name,
            request.Language,
            request.Version,
            request.ContentHtml,
            request.CheckboxesJson,
            request.AppliesToAllTreatments,
            request.TreatmentCategoryIdsJson,
            request.ShowDentalChart,
            request.ShowTreatmentTable,
            request.RequireDoctorSignature);

        await _db.SaveChangesAsync(cancellationToken);
        return ConsentMappings.ToResponse(template);
    }
}

// ── Şablon sil (soft-delete) ──────────────────────────────────────────────────

public record DeleteConsentFormTemplateCommand(Guid PublicId) : IRequest;

public class DeleteConsentFormTemplateCommandHandler
    : IRequestHandler<DeleteConsentFormTemplateCommand>
{
    private readonly AppDbContext _db;

    public DeleteConsentFormTemplateCommandHandler(AppDbContext db) => _db = db;

    public async Task Handle(DeleteConsentFormTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _db.ConsentFormTemplates
            .FirstOrDefaultAsync(t => t.PublicId == request.PublicId, cancellationToken)
            ?? throw new NotFoundException("Onam formu şablonu bulunamadı.");

        template.SoftDelete();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
