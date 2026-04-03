using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oravity.Core.Modules.Finance.EInvoice.Infrastructure.Adapters;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.EInvoice.Application.Commands;

/// <summary>
/// E-fatura / e-arşiv iptal işlemi.
/// XML_EXPORT provider'da yerel kayıt; gerçek entegratörde GİB iptal isteği gönderilir.
/// </summary>
public record CancelEInvoiceCommand(Guid PublicId, string Reason) : IRequest<Unit>;

public class CancelEInvoiceCommandHandler : IRequestHandler<CancelEInvoiceCommand, Unit>
{
    private readonly AppDbContext           _db;
    private readonly EInvoiceAdapterFactory _factory;
    private readonly ITenantContext         _tenant;
    private readonly ILogger<CancelEInvoiceCommandHandler> _logger;

    public CancelEInvoiceCommandHandler(
        AppDbContext           db,
        EInvoiceAdapterFactory factory,
        ITenantContext         tenant,
        ILogger<CancelEInvoiceCommandHandler> logger)
    {
        _db      = db;
        _factory = factory;
        _tenant  = tenant;
        _logger  = logger;
    }

    public async Task<Unit> Handle(CancelEInvoiceCommand request, CancellationToken ct)
    {
        var einvoice = await _db.EInvoices
            .FirstOrDefaultAsync(e => e.PublicId == request.PublicId, ct)
            ?? throw new NotFoundException($"E-fatura bulunamadı: {request.PublicId}");

        if (einvoice.CompanyId != _tenant.CompanyId)
            throw new ForbiddenException("Bu faturaya erişim yetkiniz yok.");

        if (einvoice.IsCancelled)
            throw new ConflictException("Bu fatura zaten iptal edilmiş.");

        // Entegratör üzerinden GİB iptal isteği (varsa GibUuid)
        if (einvoice.GibUuid is not null)
        {
            var integration = await _db.EInvoiceIntegrations
                .FirstOrDefaultAsync(i => i.CompanyId == einvoice.CompanyId && i.IsActive, ct);

            if (integration is not null)
            {
                var adapter = _factory.Create(integration.Provider);
                await adapter.Cancel(einvoice.GibUuid, request.Reason, ct);
            }
        }

        einvoice.Cancel(request.Reason);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "E-fatura iptal edildi: EInvoiceNo={No} Reason={Reason}",
            einvoice.EInvoiceNo, request.Reason);

        return Unit.Value;
    }
}
