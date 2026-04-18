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

    public GetInstitutionInvoicesQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
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
                PatientName = (i.Patient.FirstName ?? "") + " " + (i.Patient.LastName ?? ""),
                InstitutionName = i.Institution.Name
            })
            .ToListAsync(ct);

        var items = raw
            .Select(x => InstitutionInvoiceMappings.ToResponse(x.Invoice, x.PatientName.Trim(), x.InstitutionName))
            .ToList();

        return new PagedInvoiceResult(items, total, page, pageSize);
    }
}

public record GetInstitutionInvoiceByIdQuery(Guid PublicId) : IRequest<InstitutionInvoiceResponse?>;

public class GetInstitutionInvoiceByIdQueryHandler
    : IRequestHandler<GetInstitutionInvoiceByIdQuery, InstitutionInvoiceResponse?>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetInstitutionInvoiceByIdQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
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
                PatientName = (i.Patient.FirstName ?? "") + " " + (i.Patient.LastName ?? ""),
                InstitutionName = i.Institution.Name
            })
            .FirstOrDefaultAsync(ct);

        return row == null
            ? null
            : InstitutionInvoiceMappings.ToResponse(row.Invoice, row.PatientName.Trim(), row.InstitutionName);
    }
}
