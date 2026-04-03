using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Finance.EInvoice.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.EInvoice.Application.Queries;

/// <summary>
/// Tenant-scoped e-fatura listesi; tarih, durum ve alıcı adı filtreleri destekler.
/// </summary>
public record GetEInvoicesQuery(
    EInvoiceType? InvoiceType   = null,
    string?       GibStatus     = null,
    bool?         IsCancelled   = null,
    DateTime?     From          = null,
    DateTime?     To            = null,
    string?       ReceiverName  = null,
    int           Page          = 1,
    int           PageSize      = 50) : IRequest<EInvoicePagedResult>;

public class GetEInvoicesQueryHandler : IRequestHandler<GetEInvoicesQuery, EInvoicePagedResult>
{
    private readonly AppDbContext  _db;
    private readonly ITenantContext _tenant;

    public GetEInvoicesQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<EInvoicePagedResult> Handle(GetEInvoicesQuery request, CancellationToken ct)
    {
        var query = _db.EInvoices
            .Where(e => e.CompanyId == _tenant.CompanyId)
            .AsQueryable();

        if (request.InvoiceType.HasValue)
            query = query.Where(e => e.InvoiceType == request.InvoiceType.Value);

        if (!string.IsNullOrWhiteSpace(request.GibStatus))
            query = query.Where(e => e.GibStatus == request.GibStatus);

        if (request.IsCancelled.HasValue)
            query = query.Where(e => e.IsCancelled == request.IsCancelled.Value);

        if (request.From.HasValue)
            query = query.Where(e => e.CreatedAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(e => e.CreatedAt <= request.To.Value.AddDays(1));

        if (!string.IsNullOrWhiteSpace(request.ReceiverName))
            query = query.Where(e => e.ReceiverName.Contains(request.ReceiverName));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EInvoiceSummary(
                e.Id,
                e.PublicId,
                e.EInvoiceNo,
                e.InvoiceType,
                e.ReceiverName,
                e.Total,
                e.Currency,
                e.GibStatus,
                e.IsCancelled,
                e.InvoiceDate,
                e.CreatedAt))
            .ToListAsync(ct);

        return new EInvoicePagedResult(items, total, request.Page, request.PageSize);
    }
}

/// <summary>
/// Tek bir e-faturanın tüm detaylarını (kalemler dahil) döndürür.
/// </summary>
public record GetEInvoiceDetailQuery(Guid PublicId) : IRequest<EInvoiceDetail>;

public class GetEInvoiceDetailQueryHandler : IRequestHandler<GetEInvoiceDetailQuery, EInvoiceDetail>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public GetEInvoiceDetailQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<EInvoiceDetail> Handle(GetEInvoiceDetailQuery request, CancellationToken ct)
    {
        var e = await _db.EInvoices
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.PublicId == request.PublicId && x.CompanyId == _tenant.CompanyId, ct)
            ?? throw new Oravity.SharedKernel.Exceptions.NotFoundException($"E-fatura bulunamadı: {request.PublicId}");

        return new EInvoiceDetail(
            e.Id, e.PublicId, e.EInvoiceNo, e.InvoiceType, e.PaymentId,
            e.ReceiverType, e.ReceiverName, e.ReceiverTc, e.ReceiverVkn, e.ReceiverEmail,
            e.Subtotal, e.DiscountAmount, e.TaxableAmount, e.TaxRate, e.TaxAmount, e.Total,
            e.Currency, e.GibUuid, e.GibStatus, e.SentToGibAt, e.PdfPath,
            e.SentToReceiverAt, e.IsCancelled, e.CancelReason, e.InvoiceDate, e.CreatedAt,
            e.Items.OrderBy(i => i.SortOrder).Select(i => new EInvoiceItemDetail(
                i.SortOrder, i.Description, i.Quantity, i.Unit,
                i.UnitPrice, i.DiscountRate, i.DiscountAmount,
                i.TaxRate, i.TaxAmount, i.Total)).ToList());
    }
}
