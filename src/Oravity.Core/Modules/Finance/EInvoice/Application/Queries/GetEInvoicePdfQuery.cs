using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.EInvoice.Application.Queries;

/// <summary>PDF yolu + fatura numarası döner. PDF yoksa null.</summary>
public record GetEInvoicePdfQuery(Guid PublicId) : IRequest<EInvoicePdfResult?>;

public record EInvoicePdfResult(string PdfPath, string? EInvoiceNo);

public class GetEInvoicePdfQueryHandler : IRequestHandler<GetEInvoicePdfQuery, EInvoicePdfResult?>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public GetEInvoicePdfQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<EInvoicePdfResult?> Handle(
        GetEInvoicePdfQuery request, CancellationToken cancellationToken)
    {
        var einvoice = await _db.EInvoices
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.PublicId == request.PublicId && e.CompanyId == _tenant.CompanyId,
                cancellationToken)
            ?? throw new NotFoundException($"E-fatura bulunamadı: {request.PublicId}");

        if (string.IsNullOrEmpty(einvoice.PdfPath))
            return null;

        return new EInvoicePdfResult(einvoice.PdfPath, einvoice.EInvoiceNo);
    }
}
