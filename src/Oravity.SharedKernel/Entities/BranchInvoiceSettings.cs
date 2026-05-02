namespace Oravity.SharedKernel.Entities;

public enum InvoiceIntegratorType
{
    None         = 0, // Yerel sayaç (entegratör yok)
    Sovos        = 1,
    DigitalPlanet = 2,
    Custom       = 99
}

public enum EInvoiceDocumentType
{
    Normal   = 0, // Entegratörsüz, basit sayaç
    EArchive = 1, // E-Arşiv
    EInvoice = 2  // E-Fatura
}

/// <summary>
/// Şube bazlı e-fatura entegratör ayarları ve yerel fatura sayaçları.
/// Her şube için tek kayıt (one-to-one ile Branch).
/// </summary>
public class BranchInvoiceSettings : BaseEntities.BaseEntity
{
    public long BranchId { get; private set; }

    // ── Entegratör tipi ──────────────────────────────────────────────────
    public InvoiceIntegratorType IntegratorType { get; private set; } = InvoiceIntegratorType.None;

    // Firma VKN (fatura gönderici)
    public string? CompanyVkn { get; private set; }

    // Entegratör kimlik bilgileri (şifrelenmiş saklanmalı — TODO Faz 2)
    public string? IntegratorEndpoint { get; private set; }
    public string? IntegratorCompanyCode { get; private set; }
    public string? IntegratorUsername { get; private set; }
    public string? IntegratorPassword { get; private set; } // AES encrypt — Faz 2

    // ── Fatura sayaçları (yerel mod veya entegratör fallback) ────────────
    public string? NormalPrefix   { get; private set; }
    public long    NormalCounter  { get; private set; } = 0;

    public string? EArchivePrefix   { get; private set; }
    public long    EArchiveCounter  { get; private set; } = 0;

    public string? EInvoicePrefix   { get; private set; }
    public long    EInvoiceCounter  { get; private set; } = 0;

    private BranchInvoiceSettings() { }

    public static BranchInvoiceSettings Create(long branchId) => new()
    {
        BranchId = branchId,
    };

    public void UpdateIntegrator(
        InvoiceIntegratorType type,
        string? companyVkn,
        string? endpoint,
        string? companyCode,
        string? username,
        string? password)
    {
        IntegratorType       = type;
        CompanyVkn           = companyVkn?.Trim();
        IntegratorEndpoint   = endpoint?.Trim();
        IntegratorCompanyCode = companyCode?.Trim();
        IntegratorUsername   = username?.Trim();
        if (password != null) IntegratorPassword = password; // null → değiştirme
        MarkUpdated();
    }

    public void UpdatePrefixes(
        string? normalPrefix,   long? normalCounter,
        string? eArchivePrefix, long? eArchiveCounter,
        string? eInvoicePrefix, long? eInvoiceCounter)
    {
        NormalPrefix   = normalPrefix?.Trim().ToUpperInvariant();
        EArchivePrefix = eArchivePrefix?.Trim().ToUpperInvariant();
        EInvoicePrefix = eInvoicePrefix?.Trim().ToUpperInvariant();

        if (normalCounter.HasValue)   NormalCounter   = normalCounter.Value;
        if (eArchiveCounter.HasValue) EArchiveCounter = eArchiveCounter.Value;
        if (eInvoiceCounter.HasValue) EInvoiceCounter = eInvoiceCounter.Value;
        MarkUpdated();
    }

}
