using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.PatientInvoice.Application.Queries;

public record GetPatientInvoicesQuery(
    PatientInvoiceStatus? Status = null,
    long? PatientId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedPatientInvoiceResult>;

public record PagedPatientInvoiceResult(
    IReadOnlyList<PatientInvoiceResponse> Items,
    int Total,
    int Page,
    int PageSize
);

public class GetPatientInvoicesQueryHandler
    : IRequestHandler<GetPatientInvoicesQuery, PagedPatientInvoiceResult>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetPatientInvoicesQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PagedPatientInvoiceResult> Handle(
        GetPatientInvoicesQuery r, CancellationToken ct)
    {
        var q = _db.PatientInvoices.AsNoTracking().AsQueryable();

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            q = q.Where(i => i.BranchId == _tenant.BranchId.Value);

        if (r.Status.HasValue)    q = q.Where(i => i.Status == r.Status.Value);
        if (r.PatientId.HasValue) q = q.Where(i => i.PatientId == r.PatientId.Value);
        if (r.From.HasValue)      q = q.Where(i => i.InvoiceDate >= r.From.Value);
        if (r.To.HasValue)        q = q.Where(i => i.InvoiceDate <= r.To.Value);

        var total = await q.CountAsync(ct);
        var pageSize = Math.Clamp(r.PageSize, 1, 200);
        var page = Math.Max(1, r.Page);

        var raw = await q
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new
            {
                Invoice = i,
                PatientName = (i.Patient.FirstName ?? "") + " " + (i.Patient.LastName ?? "")
            })
            .ToListAsync(ct);

        var items = raw
            .Select(x => PatientInvoiceMappings.ToResponse(x.Invoice, x.PatientName.Trim()))
            .ToList();

        return new PagedPatientInvoiceResult(items, total, page, pageSize);
    }
}

public record GetPatientInvoiceByIdQuery(Guid PublicId) : IRequest<PatientInvoiceResponse?>;

public class GetPatientInvoiceByIdQueryHandler
    : IRequestHandler<GetPatientInvoiceByIdQuery, PatientInvoiceResponse?>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetPatientInvoiceByIdQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PatientInvoiceResponse?> Handle(
        GetPatientInvoiceByIdQuery r, CancellationToken ct)
    {
        var q = _db.PatientInvoices.AsNoTracking()
            .Where(i => i.PublicId == r.PublicId);

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            q = q.Where(i => i.BranchId == _tenant.BranchId.Value);

        var row = await q
            .Select(i => new
            {
                Invoice = i,
                PatientName = (i.Patient.FirstName ?? "") + " " + (i.Patient.LastName ?? "")
            })
            .FirstOrDefaultAsync(ct);

        return row == null
            ? null
            : PatientInvoiceMappings.ToResponse(row.Invoice, row.PatientName.Trim());
    }
}
