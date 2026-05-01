using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.InstitutionInvoice.Application.Queries;

public record GetInstitutionInvoicesQuery(
    InstitutionInvoiceStatus? Status = null,
    long? InstitutionId = null,
    long? PatientId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedInvoiceResult>;

public record PagedInvoiceResult(
    IReadOnlyList<InstitutionInvoiceResponse> Items,
    int Total,
    int Page,
    int PageSize
);

public class GetInstitutionInvoicesQueryHandler
    : IRequestHandler<GetInstitutionInvoicesQuery, PagedInvoiceResult>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IEncryptionService _encryption;

    public GetInstitutionInvoicesQueryHandler(AppDbContext db, ITenantContext tenant, IEncryptionService encryption)
    {
        _db = db;
        _tenant = tenant;
        _encryption = encryption;
    }

    public async Task<PagedInvoiceResult> Handle(
        GetInstitutionInvoicesQuery r, CancellationToken ct)
    {
        var q = _db.InstitutionInvoices.AsNoTracking().AsQueryable();

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            q = q.Where(i => i.BranchId == _tenant.BranchId.Value);

        if (r.Status.HasValue)         q = q.Where(i => i.Status == r.Status.Value);
        if (r.InstitutionId.HasValue)  q = q.Where(i => i.InstitutionId == r.InstitutionId.Value);
        if (r.PatientId.HasValue)      q = q.Where(i => i.PatientId == r.PatientId.Value);
        if (r.From.HasValue)           q = q.Where(i => i.InvoiceDate >= r.From.Value);
        if (r.To.HasValue)             q = q.Where(i => i.InvoiceDate <= r.To.Value);

        var total = await q.CountAsync(ct);
        var pageSize = Math.Clamp(r.PageSize, 1, 200);
        var page = Math.Max(1, r.Page);

        var raw = await q
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new
            {
                Invoice = i,
                PatientFirstName = i.Patient.FirstName ?? "",
                PatientLastName  = i.Patient.LastName ?? "",
                PatientTcEnc     = i.Patient.TcNumberEncrypted,
                InstitutionName      = i.Institution.Name,
                InstitutionTaxNumber = i.Institution.TaxNumber,
                InstitutionTaxOffice = i.Institution.TaxOffice,
                InstitutionAddress   = i.Institution.Address,
                InstitutionCity      = i.Institution.City,
            })
            .ToListAsync(ct);

        var items = raw.Select(x =>
        {
            string? tc = null;
            if (!string.IsNullOrEmpty(x.PatientTcEnc))
                try { tc = _encryption.Decrypt(x.PatientTcEnc); } catch { /* şifre çözülemezse null bırak */ }

            return InstitutionInvoiceMappings.ToResponse(
                x.Invoice,
                $"{x.PatientFirstName} {x.PatientLastName}".Trim(),
                x.InstitutionName,
                tc,
                x.InstitutionTaxNumber,
                x.InstitutionTaxOffice,
                x.InstitutionAddress,
                x.InstitutionCity);
        }).ToList();

        return new PagedInvoiceResult(items, total, page, pageSize);
    }
}

public record GetInstitutionInvoiceByIdQuery(Guid PublicId) : IRequest<InstitutionInvoiceResponse?>;

public class GetInstitutionInvoiceByIdQueryHandler
    : IRequestHandler<GetInstitutionInvoiceByIdQuery, InstitutionInvoiceResponse?>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IEncryptionService _encryption;

    public GetInstitutionInvoiceByIdQueryHandler(AppDbContext db, ITenantContext tenant, IEncryptionService encryption)
    {
        _db = db;
        _tenant = tenant;
        _encryption = encryption;
    }

    public async Task<InstitutionInvoiceResponse?> Handle(
        GetInstitutionInvoiceByIdQuery r, CancellationToken ct)
    {
        var q = _db.InstitutionInvoices.AsNoTracking()
            .Where(i => i.PublicId == r.PublicId);

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            q = q.Where(i => i.BranchId == _tenant.BranchId.Value);

        var row = await q
            .Select(i => new
            {
                Invoice = i,
                PatientFirstName = i.Patient.FirstName ?? "",
                PatientLastName  = i.Patient.LastName ?? "",
                PatientTcEnc     = i.Patient.TcNumberEncrypted,
                InstitutionName      = i.Institution.Name,
                InstitutionTaxNumber = i.Institution.TaxNumber,
                InstitutionTaxOffice = i.Institution.TaxOffice,
                InstitutionAddress   = i.Institution.Address,
                InstitutionCity      = i.Institution.City,
            })
            .FirstOrDefaultAsync(ct);

        if (row == null) return null;

        string? tc = null;
        if (!string.IsNullOrEmpty(row.PatientTcEnc))
            try { tc = _encryption.Decrypt(row.PatientTcEnc); } catch { /* şifre çözülemezse null bırak */ }

        return InstitutionInvoiceMappings.ToResponse(
            row.Invoice,
            $"{row.PatientFirstName} {row.PatientLastName}".Trim(),
            row.InstitutionName,
            tc,
            row.InstitutionTaxNumber,
            row.InstitutionTaxOffice,
            row.InstitutionAddress,
            row.InstitutionCity);
    }
}
