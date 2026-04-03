using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Finance.EInvoice.Application;

// ─── Request ──────────────────────────────────────────────────────────────
public record EInvoiceItemInput(
    string  Description,
    decimal Quantity      = 1m,
    string  Unit          = "Adet",
    decimal UnitPrice     = 0m,
    decimal DiscountRate  = 0m,
    decimal TaxRate       = 10m,
    int     SortOrder     = 0);

// ─── Response ─────────────────────────────────────────────────────────────
public record EInvoiceSummary(
    long        Id,
    Guid        PublicId,
    string?     EInvoiceNo,
    EInvoiceType InvoiceType,
    string      ReceiverName,
    decimal     Total,
    string      Currency,
    string?     GibStatus,
    bool        IsCancelled,
    DateOnly    InvoiceDate,
    DateTime    CreatedAt);

public record EInvoiceDetail(
    long        Id,
    Guid        PublicId,
    string?     EInvoiceNo,
    EInvoiceType InvoiceType,
    long?       PaymentId,
    EInvoiceReceiverType ReceiverType,
    string      ReceiverName,
    string?     ReceiverTc,
    string?     ReceiverVkn,
    string?     ReceiverEmail,
    decimal     Subtotal,
    decimal     DiscountAmount,
    decimal     TaxableAmount,
    decimal     TaxRate,
    decimal     TaxAmount,
    decimal     Total,
    string      Currency,
    string?     GibUuid,
    string?     GibStatus,
    DateTime?   SentToGibAt,
    string?     PdfPath,
    DateTime?   SentToReceiverAt,
    bool        IsCancelled,
    string?     CancelReason,
    DateOnly    InvoiceDate,
    DateTime    CreatedAt,
    IReadOnlyList<EInvoiceItemDetail> Items);

public record EInvoiceItemDetail(
    int     SortOrder,
    string  Description,
    decimal Quantity,
    string  Unit,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal DiscountAmount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal Total);

public record EInvoicePagedResult(
    IReadOnlyList<EInvoiceSummary> Items,
    int Total,
    int Page,
    int PageSize);

public record CreateEArchiveResult(
    Guid    PublicId,
    string? EInvoiceNo,
    string? GibStatus,
    string? XmlContent,
    string? PdfPath,
    string  Message);
