using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Laboratory.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Commands;

public record CreateLaboratoryCommand(
    string  Name,
    string? Code,
    string? Phone,
    string? Email,
    string? Website,
    string? Country,
    string? City,
    string? District,
    string? Address,
    string? ContactPerson,
    string? ContactPhone,
    string? WorkingDays,
    string? WorkingHours,
    string? PaymentTerms,
    int     PaymentDays,
    string? Notes
) : IRequest<LaboratoryResponse>;

public class CreateLaboratoryCommandHandler
    : IRequestHandler<CreateLaboratoryCommand, LaboratoryResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public CreateLaboratoryCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<LaboratoryResponse> Handle(
        CreateLaboratoryCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, cancellationToken)
            ?? throw new ForbiddenException("Laboratuvar oluşturmak için şirket bağlamı gereklidir.");

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var codeNorm = request.Code.Trim().ToUpperInvariant();
            var exists = await _db.Laboratories.AsNoTracking()
                .AnyAsync(l => l.CompanyId == companyId && l.Code == codeNorm, cancellationToken);
            if (exists) throw new ConflictException($"'{codeNorm}' kodlu laboratuvar zaten mevcut.");
        }

        var lab = Oravity.SharedKernel.Entities.Laboratory.Create(
            companyId,
            request.Name,
            request.Code,
            request.Phone,
            request.Email,
            request.Website,
            request.Country,
            request.City,
            request.District,
            request.Address,
            request.ContactPerson,
            request.ContactPhone,
            request.WorkingDays,
            request.WorkingHours,
            request.PaymentTerms,
            request.PaymentDays,
            request.Notes);

        _db.Laboratories.Add(lab);
        await _db.SaveChangesAsync(cancellationToken);

        return new LaboratoryResponse(
            lab.PublicId, lab.Name, lab.Code, lab.Phone, lab.Email, lab.Website,
            lab.Country, lab.City, lab.District, lab.Address,
            lab.ContactPerson, lab.ContactPhone,
            lab.WorkingDays, lab.WorkingHours, lab.PaymentTerms, lab.PaymentDays,
            lab.Notes, lab.IsActive, 0, 0, lab.CreatedAt);
    }
}
