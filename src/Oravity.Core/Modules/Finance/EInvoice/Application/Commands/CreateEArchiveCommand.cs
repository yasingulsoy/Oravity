using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oravity.Core.Modules.Finance.EInvoice.Application;
using Oravity.Core.Modules.Finance.EInvoice.Infrastructure.Adapters;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using EInvoiceEntity = Oravity.SharedKernel.Entities.EInvoice;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Finance.EInvoice.Application.Commands;

/// <summary>
/// Hasta ödemesinden e-arşiv fatura oluşturur (SPEC §E-FATURA §3).
/// Adapter pattern: XML_EXPORT (Aşama 1) veya PARASUT/LOGO (Aşama 2).
/// </summary>
public record CreateEArchiveCommand(
    long   PaymentId,
    string? ReceiverName    = null,
    /// <summary>TC açık olarak gönderilir — şifreli Patient.TcNumberEncrypted kullanılmaz.</summary>
    string? ReceiverTc      = null,
    string? ReceiverAddress = null,
    string? ReceiverEmail   = null,
    IReadOnlyList<EInvoiceItemInput>? Items = null) : IRequest<CreateEArchiveResult>;

public class CreateEArchiveCommandHandler : IRequestHandler<CreateEArchiveCommand, CreateEArchiveResult>
{
    private readonly AppDbContext           _db;
    private readonly EInvoiceAdapterFactory _factory;
    private readonly ICurrentUser           _user;
    private readonly ITenantContext         _tenant;
    private readonly ILogger<CreateEArchiveCommandHandler> _logger;

    public CreateEArchiveCommandHandler(
        AppDbContext           db,
        EInvoiceAdapterFactory factory,
        ICurrentUser           user,
        ITenantContext         tenant,
        ILogger<CreateEArchiveCommandHandler> logger)
    {
        _db      = db;
        _factory = factory;
        _user    = user;
        _tenant  = tenant;
        _logger  = logger;
    }

    public async Task<CreateEArchiveResult> Handle(CreateEArchiveCommand request, CancellationToken ct)
    {
        // 1. Ödeme ve hasta bilgisi
        var payment = await _db.Payments
            .Include(p => p.Patient)
            .Include(p => p.Branch)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, ct)
            ?? throw new NotFoundException($"Ödeme bulunamadı: {request.PaymentId}");

        if (payment.Branch.CompanyId != _tenant.CompanyId)
            throw new ForbiddenException("Bu ödemeye erişim yetkiniz yok.");

        // 2. Entegratör ayarları
        var integration = await _db.EInvoiceIntegrations
            .FirstOrDefaultAsync(i => i.CompanyId == payment.Branch.CompanyId && i.IsActive, ct)
            ?? throw new NotFoundException(
                $"Şirket için aktif e-fatura entegrasyonu bulunamadı. " +
                "Lütfen ayarlardan bir entegratör ekleyin.");

        // 3. Sıra numarası — şirket bazlı seri için MAX+1
        const string defaultSeries = "GBS";
        var lastSeq = await _db.EInvoices
            .Where(e => e.CompanyId == payment.Branch.CompanyId && e.Series == defaultSeries)
            .MaxAsync(e => (int?)e.Sequence, ct) ?? 0;
        var sequence = lastSeq + 1;

        // 4. Alıcı bilgisi
        var patient       = payment.Patient;
        var receiverName  = request.ReceiverName  ?? $"{patient.FirstName} {patient.LastName}".Trim();
        var receiverEmail = request.ReceiverEmail ?? patient.Email;

        // 5. Kalemler — verilmezse ödeme tutarından tek satır üretilir
        var itemInputs = (request.Items is { Count: > 0 })
            ? request.Items
            : (IReadOnlyList<EInvoiceItemInput>)new List<EInvoiceItemInput>
            {
                new EInvoiceItemInput("Diş Hekimliği Hizmet Bedeli", UnitPrice: payment.Amount)
            };

        var subtotal = itemInputs.Sum(i => i.UnitPrice * i.Quantity);

        // 6. EInvoice entity
        var einvoice = EInvoiceEntity.Create(
            companyId:       payment.Branch.CompanyId,
            branchId:        payment.BranchId,
            invoiceType:     EInvoiceType.EArchive,
            receiverType:    EInvoiceReceiverType.Individual,
            receiverName:    receiverName,
            subtotal:        subtotal,
            taxRate:         10m,
            series:          defaultSeries,
            sequence:        sequence,
            createdBy:       _user.UserId,
            paymentId:       payment.Id,
            receiverTc:      request.ReceiverTc,
            receiverEmail:   receiverEmail,
            receiverAddress: request.ReceiverAddress);

        _db.EInvoices.Add(einvoice);
        await _db.SaveChangesAsync(ct);

        // 7. Kalemler
        var sortOrder = 0;
        foreach (var item in itemInputs)
        {
            _db.EInvoiceItems.Add(EInvoiceItem.Create(
                einvoice.Id, item.Description, item.UnitPrice,
                item.Quantity, item.DiscountRate, item.TaxRate, item.Unit, sortOrder++));
        }
        await _db.SaveChangesAsync(ct);

        // 8. Adapter
        await _db.Entry(einvoice).Collection(e => e.Items).LoadAsync(ct);
        var adapterRequest = BuildAdapterRequest(einvoice, itemInputs, integration);
        var adapter        = _factory.Create(integration.Provider);
        var result         = await adapter.SendEArchive(adapterRequest, ct);

        // 9. GİB yanıtını kaydet
        if (result.Success)
        {
            einvoice.MarkSentToGib(
                result.GibUuid    ?? $"LOCAL-{einvoice.PublicId}",
                result.Status     ?? "LOCAL_EXPORT",
                result.ResponseJson);

            if (receiverEmail is not null)
                einvoice.MarkSentToReceiver();
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "E-arşiv oluşturuldu: EInvoiceNo={No} GibStatus={Status}",
            einvoice.EInvoiceNo, result.Status);

        return new CreateEArchiveResult(
            einvoice.PublicId,
            einvoice.EInvoiceNo,
            result.Status,
            result.XmlContent,
            result.PdfPath,
            result.Success ? "E-arşiv başarıyla oluşturuldu." : $"Hata: {result.ErrorMessage}");
    }

    private static EInvoiceRequest BuildAdapterRequest(
        EInvoiceEntity einvoice,
        IReadOnlyList<EInvoiceItemInput> items,
        EInvoiceIntegration integration)
    {
        return new EInvoiceRequest(
            PublicId:          einvoice.PublicId,
            EInvoiceNo:        einvoice.EInvoiceNo!,
            InvoiceType:       einvoice.InvoiceType,
            Series:            einvoice.Series,
            Sequence:          einvoice.Sequence!.Value,
            InvoiceDate:       einvoice.InvoiceDate,
            SupplierTitle:     integration.CompanyTitle,
            SupplierVkn:       integration.Vkn,
            SupplierTaxOffice: integration.TaxOffice,
            SupplierAddress:   integration.Address,
            ReceiverType:      einvoice.ReceiverType,
            ReceiverName:      einvoice.ReceiverName,
            ReceiverTc:        einvoice.ReceiverTc,
            ReceiverVkn:       einvoice.ReceiverVkn,
            ReceiverTaxOffice: einvoice.ReceiverTaxOffice,
            ReceiverAddress:   einvoice.ReceiverAddress,
            ReceiverEmail:     einvoice.ReceiverEmail,
            Subtotal:          einvoice.Subtotal,
            DiscountAmount:    einvoice.DiscountAmount,
            TaxableAmount:     einvoice.TaxableAmount,
            TaxRate:           einvoice.TaxRate,
            TaxAmount:         einvoice.TaxAmount,
            Total:             einvoice.Total,
            Currency:          einvoice.Currency,
            LanguageCode:      einvoice.LanguageCode,
            Items: items.Select((i, idx) => new EInvoiceItemRequest(
                i.Description, i.Quantity, i.Unit, i.UnitPrice,
                i.DiscountRate, i.TaxRate, idx)).ToList(),
            IsTestMode:     integration.IsTestMode,
            ProviderConfig: integration.Config);
    }
}
