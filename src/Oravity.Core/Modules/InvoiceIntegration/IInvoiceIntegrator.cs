namespace Oravity.Core.Modules.InvoiceIntegration;

// ── Request / Result tipleri ──────────────────────────────────────────────────

public record GenerateInvoiceNumberRequest(
    long   BranchId,
    string InvoiceType,   // "NORMAL" | "EARCHIVE" | "EINVOICE"
    DateOnly IssueDate
);

public record InvoiceNumberResult(
    string   InvoiceNo,  // Üretilen fatura numarası
    string?  ExternalUuid // Entegratörden gelen UUID (Sovos, DP vb.) — yerel modda null
);

public record SendInvoiceRequest(
    long   InstitutionInvoiceId,
    string InvoiceNo,
    string? ExternalUuid,
    string InvoiceType,
    string ReceiverVkn,
    byte[] UblXml        // UBL-TR XML içeriği
);

public record SendInvoiceResult(
    bool   Success,
    string? ErrorMessage,
    string? IntegratorStatus
);

public record InvoiceStatusResult(
    string  InvoiceNo,
    string? ExternalStatus,
    bool    IsAccepted,
    bool    IsError,
    string? ErrorMessage
);

// ── Soyut arayüz ─────────────────────────────────────────────────────────────

/// <summary>
/// Fatura entegratörü soyut arayüzü.
/// Her entegratör (Sovos, Digital Planet, vb.) bu arayüzü implement eder.
/// Entegratör olmadığında <see cref="LocalCounterIntegrator"/> kullanılır.
/// </summary>
public interface IInvoiceIntegrator
{
    /// <summary>
    /// Fatura numarası üretir. Yerel modda DB sayacından, entegratör modunda
    /// API'den (Sovos: generateInvID) alır.
    /// </summary>
    Task<InvoiceNumberResult> GenerateInvoiceNumberAsync(
        GenerateInvoiceNumberRequest request, CancellationToken ct = default);

    /// <summary>
    /// Faturayı entegratöre gönderir (UBL-TR XML).
    /// Yerel modda her zaman başarılı döner (gönderim yok).
    /// </summary>
    Task<SendInvoiceResult> SendInvoiceAsync(
        SendInvoiceRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gönderilmiş faturanın durumunu entegratörden sorgular.
    /// </summary>
    Task<InvoiceStatusResult> GetInvoiceStatusAsync(
        string invoiceNo, string? externalUuid, CancellationToken ct = default);
}
