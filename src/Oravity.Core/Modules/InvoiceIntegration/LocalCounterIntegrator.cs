using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;

namespace Oravity.Core.Modules.InvoiceIntegration;

/// <summary>
/// Entegratör olmadığında kullanılan yerel sayaç implementasyonu.
/// DB'deki BranchInvoiceSettings sayacını atomik olarak artırır.
/// Fatura gönderimi yapmaz — sadece numara üretir.
/// </summary>
public class LocalCounterIntegrator : IInvoiceIntegrator
{
    private readonly AppDbContext _db;

    public LocalCounterIntegrator(AppDbContext db)
    {
        _db = db;
    }

    public async Task<InvoiceNumberResult> GenerateInvoiceNumberAsync(
        GenerateInvoiceNumberRequest request, CancellationToken ct = default)
    {
        var year = request.IssueDate.Year;

        // Atomik sayaç artırımı — race condition yok
        var counter = await IncrementCounterAsync(request.BranchId, request.InvoiceType, ct);

        // Ayarları oku (prefix için)
        var settings = await _db.BranchInvoiceSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BranchId == request.BranchId, ct);

        string prefix = request.InvoiceType switch
        {
            "EARCHIVE" => settings?.EArchivePrefix ?? string.Empty,
            "EINVOICE" => settings?.EInvoicePrefix ?? string.Empty,
            _          => settings?.NormalPrefix   ?? string.Empty,
        };

        // Format: {PREFIX}{YYYY}{NNNNNNNNN} — 16 karakter (GIB standardı)
        // Prefix yoksa basit format: {YYYY}/{NNNNN}
        var invoiceNo = string.IsNullOrEmpty(prefix)
            ? $"{year}/{counter:D5}"
            : $"{prefix}{year}{counter:D9}";

        return new InvoiceNumberResult(invoiceNo, ExternalUuid: null);
    }

    public Task<SendInvoiceResult> SendInvoiceAsync(
        SendInvoiceRequest request, CancellationToken ct = default)
    {
        // Yerel modda gönderim yok — başarılı simüle et
        return Task.FromResult(new SendInvoiceResult(
            Success: true,
            ErrorMessage: null,
            IntegratorStatus: "LOCAL_ONLY"));
    }

    public Task<InvoiceStatusResult> GetInvoiceStatusAsync(
        string invoiceNo, string? externalUuid, CancellationToken ct = default)
    {
        return Task.FromResult(new InvoiceStatusResult(
            InvoiceNo: invoiceNo,
            ExternalStatus: "LOCAL_ONLY",
            IsAccepted: true,
            IsError: false,
            ErrorMessage: null));
    }

    // ── Atomik sayaç artırımı (PostgreSQL RETURNING) ──────────────────────────

    private async Task<long> IncrementCounterAsync(
        long branchId, string invoiceType, CancellationToken ct)
    {
        var column = invoiceType switch
        {
            "EARCHIVE" => "\"EArchiveCounter\"",
            "EINVOICE" => "\"EInvoiceCounter\"",
            _          => "\"NormalCounter\"",
        };

        // Satır yoksa oluştur
        var exists = await _db.BranchInvoiceSettings
            .AnyAsync(s => s.BranchId == branchId, ct);

        if (!exists)
        {
            var newSettings = Oravity.SharedKernel.Entities.BranchInvoiceSettings.Create(branchId);
            _db.BranchInvoiceSettings.Add(newSettings);
            await _db.SaveChangesAsync(ct);
        }

        // Atomik UPDATE ... RETURNING
        var sql = $"UPDATE branch_invoice_settings SET {column} = {column} + 1 WHERE \"BranchId\" = {{0}} RETURNING {column}";

        var rows = await _db.Database
            .SqlQueryRaw<long>(sql, branchId)
            .ToListAsync(ct);

        if (rows.Count == 0)
            throw new InvalidOperationException(
                $"Şube {branchId} için fatura sayacı güncellenemedi (satır bulunamadı).");

        return rows[0];
    }
}
