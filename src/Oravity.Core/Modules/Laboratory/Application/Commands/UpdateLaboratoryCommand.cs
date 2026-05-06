using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Laboratory.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Commands;

public record UpdateLaboratoryCommand(
    Guid    PublicId,
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
    string? Notes,
    bool    IsActive
) : IRequest<LaboratoryResponse>;

public class UpdateLaboratoryCommandHandler
    : IRequestHandler<UpdateLaboratoryCommand, LaboratoryResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public UpdateLaboratoryCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<LaboratoryResponse> Handle(
        UpdateLaboratoryCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, cancellationToken)
            ?? throw new ForbiddenException("Şirket bağlamı gerekli.");

        var lab = await _db.Laboratories
            .FirstOrDefaultAsync(l => l.PublicId == request.PublicId
                                       && l.CompanyId == companyId, cancellationToken)
            ?? throw new NotFoundException("Laboratuvar bulunamadı.");

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var codeNorm = request.Code.Trim().ToUpperInvariant();
            var clash = await _db.Laboratories.AsNoTracking()
                .AnyAsync(l => l.CompanyId == companyId
                                && l.Code == codeNorm
                                && l.Id != lab.Id, cancellationToken);
            if (clash) throw new ConflictException($"'{codeNorm}' kodu başka bir laboratuvarda kullanılıyor.");
        }

        lab.Update(
            request.Name, request.Code,
            request.Phone, request.Email, request.Website,
            request.Country, request.City, request.District, request.Address,
            request.ContactPerson, request.ContactPhone,
            request.WorkingDays, request.WorkingHours,
            request.PaymentTerms, request.PaymentDays, request.Notes);

        lab.SetActive(request.IsActive);

        await _db.SaveChangesAsync(cancellationToken);

        var branchCount = await _db.LaboratoryBranchAssignments.AsNoTracking()
            .CountAsync(a => a.LaboratoryId == lab.Id && a.IsActive, cancellationToken);
        var workCount = await _db.LaboratoryWorks.AsNoTracking()
            .CountAsync(w => w.LaboratoryId == lab.Id
                              && w.Status != LaboratoryWorkStatus.Approved
                              && w.Status != LaboratoryWorkStatus.Cancelled
                              && w.Status != LaboratoryWorkStatus.Rejected, cancellationToken);

        return new LaboratoryResponse(
            lab.PublicId, lab.Name, lab.Code, lab.Phone, lab.Email, lab.Website,
            lab.Country, lab.City, lab.District, lab.Address,
            lab.ContactPerson, lab.ContactPhone,
            lab.WorkingDays, lab.WorkingHours, lab.PaymentTerms, lab.PaymentDays,
            lab.Notes, lab.IsActive, branchCount, workCount, lab.CreatedAt, []);
    }
}
