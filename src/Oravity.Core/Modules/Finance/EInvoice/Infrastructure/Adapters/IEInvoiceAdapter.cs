using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Finance.EInvoice.Infrastructure.Adapters;

// ─── DTO'lar ──────────────────────────────────────────────────────────────
public record EInvoiceItemRequest(
    string  Description,
    decimal Quantity,
    string  Unit,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal TaxRate,
    int     SortOrder);

public record EInvoiceRequest(
    Guid            PublicId,
    string          EInvoiceNo,
    EInvoiceType    InvoiceType,
    string          Series,
    int             Sequence,
    DateOnly        InvoiceDate,
    // Klinik (satıcı) bilgisi
    string          SupplierTitle,
    string          SupplierVkn,
    string          SupplierTaxOffice,
    string?         SupplierAddress,
    // Alıcı bilgisi
    EInvoiceReceiverType ReceiverType,
    string          ReceiverName,
    string?         ReceiverTc,
    string?         ReceiverVkn,
    string?         ReceiverTaxOffice,
    string?         ReceiverAddress,
    string?         ReceiverEmail,
    // Tutarlar
    decimal         Subtotal,
    decimal         DiscountAmount,
    decimal         TaxableAmount,
    decimal         TaxRate,
    decimal         TaxAmount,
    decimal         Total,
    string          Currency,
    string          LanguageCode,
    // Kalemler
    IReadOnlyList<EInvoiceItemRequest> Items,
    // Entegratör bağlantı ayarları
    bool            IsTestMode,
    string          ProviderConfig); // JSONB string

public record EInvoiceResult(
    bool    Success,
    string? GibUuid,
    string? Status,      // WAITING | ACCEPTED | REJECTED
    string? ResponseJson,
    string? ErrorMessage,
    string? XmlContent,  // XML_EXPORT senaryosunda doldurulur
    string? PdfPath);

public record EInvoiceStatus(
    string  Uuid,
    string  Status,
    string? ResponseJson);

// ─── Adapter arayüzü ──────────────────────────────────────────────────────
/// <summary>
/// Entegratörden bağımsız e-fatura adapter arayüzü (SPEC §ENTEGRATÖR §4).
/// Aşama 1: XmlExportAdapter (GİB'e göndermez, UBL-TR XML üretir).
/// Aşama 2: ParasutAdapter, LogoAdapter vb.
/// </summary>
public interface IEInvoiceAdapter
{
    Task<EInvoiceResult> SendEArchive(EInvoiceRequest request, CancellationToken ct = default);
    Task<EInvoiceResult> SendEInvoice(EInvoiceRequest request, CancellationToken ct = default);
    Task<EInvoiceResult> SendESMM(EInvoiceRequest request, CancellationToken ct = default);
    Task<EInvoiceStatus> QueryStatus(string uuid, CancellationToken ct = default);
    Task<EInvoiceResult> Cancel(string uuid, string reason, CancellationToken ct = default);
    Task<bool> IsGibRegistered(string vkn, CancellationToken ct = default);
}
