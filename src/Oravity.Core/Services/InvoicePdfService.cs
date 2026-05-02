using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text.Json;

namespace Oravity.Core.Services;

public class InvoicePdfService
{
    static readonly CultureInfo TR = new("tr-TR");

    private readonly AppDbContext _db;

    public InvoicePdfService(AppDbContext db) => _db = db;

    // ── PatientInvoice PDF ────────────────────────────────────────────────────

    public async Task<byte[]> GeneratePatientInvoicePdfAsync(Guid publicId, CancellationToken ct = default)
    {
        var invoice = await _db.PatientInvoices
            .AsNoTracking()
            .Include(i => i.Branch).ThenInclude(b => b.Company)
            .Include(i => i.Patient)
            .FirstOrDefaultAsync(i => i.PublicId == publicId, ct)
            ?? throw new NotFoundException($"Hasta faturası bulunamadı: {publicId}");

        var settings = await _db.BranchInvoiceSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BranchId == invoice.BranchId, ct);

        var lineItems = await LoadTreatmentLineItemsAsync(invoice.TreatmentItemIdsJson, ct);

        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Element(c => InvoiceHeader(c,
                        invoice.Branch.Company.Name,
                        invoice.Branch.Name,
                        settings?.CompanyVkn,
                        invoice.InvoiceType == "EINVOICE" ? "E-FATURA" : "E-ARŞİV FATURA"));

                    col.Item().Element(c => InfoRow(c,
                        sellerItems: [
                            ("VKN", settings?.CompanyVkn ?? "—"),
                            ("Şube", invoice.Branch.Name),
                            ("Fatura Tipi", invoice.InvoiceType == "EINVOICE" ? "e-Fatura" : "e-Arşiv"),
                        ],
                        invoiceItems: [
                            ("Fatura No", invoice.InvoiceNo),
                            ("Fatura Tarihi", invoice.InvoiceDate.ToString("dd.MM.yyyy")),
                            ("Vade Tarihi", invoice.DueDate.ToString("dd.MM.yyyy")),
                        ]));

                    col.Item().Element(c => RecipientBox(c,
                        invoice.RecipientName,
                        invoice.RecipientType == InvoiceRecipientType.IndividualTc
                            ? ("TC Kimlik No", invoice.RecipientTcNo ?? "—")
                            : ("VKN", invoice.RecipientVkn ?? "—"),
                        invoice.RecipientTaxOffice));

                    if (lineItems.Count > 0)
                        col.Item().Element(c => LineItemsTable(c, lineItems, invoice.Currency));

                    col.Item().Element(c => PatientTotals(c,
                        invoice.Amount, invoice.KdvRate, invoice.KdvAmount, invoice.TotalAmount, invoice.Currency));

                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                        col.Item().Element(c => NotesBox(c, invoice.Notes));

                    col.Item().Element(SimulationFooter);
                });
            });
        }).GeneratePdf();
    }

    // ── InstitutionInvoice PDF ────────────────────────────────────────────────

    public async Task<byte[]> GenerateInstitutionInvoicePdfAsync(Guid publicId, CancellationToken ct = default)
    {
        var invoice = await _db.InstitutionInvoices
            .AsNoTracking()
            .Include(i => i.Branch).ThenInclude(b => b.Company)
            .Include(i => i.Institution)
            .Include(i => i.Patient)
            .FirstOrDefaultAsync(i => i.PublicId == publicId, ct)
            ?? throw new NotFoundException($"Kurum faturası bulunamadı: {publicId}");

        var settings = await _db.BranchInvoiceSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BranchId == invoice.BranchId, ct);

        var lineItems = await LoadTreatmentLineItemsAsync(invoice.TreatmentItemIdsJson, ct);

        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Element(c => InvoiceHeader(c,
                        invoice.Branch.Company.Name,
                        invoice.Branch.Name,
                        settings?.CompanyVkn,
                        "E-FATURA"));

                    col.Item().Element(c => InfoRow(c,
                        sellerItems: [
                            ("VKN", settings?.CompanyVkn ?? "—"),
                            ("Şube", invoice.Branch.Name),
                            ("Fatura Tipi", "e-Fatura"),
                        ],
                        invoiceItems: [
                            ("Fatura No", invoice.InvoiceNo),
                            ("Fatura Tarihi", invoice.InvoiceDate.ToString("dd.MM.yyyy")),
                            ("Vade Tarihi", invoice.DueDate.ToString("dd.MM.yyyy")),
                        ]));

                    col.Item().Element(c => RecipientBox(c,
                        invoice.Institution.Name,
                        ("VKN", invoice.Institution.TaxNumber ?? "—"),
                        invoice.Institution.TaxOffice,
                        patientName: $"{invoice.Patient.FirstName} {invoice.Patient.LastName}".Trim()));

                    if (lineItems.Count > 0)
                        col.Item().Element(c => LineItemsTable(c, lineItems, invoice.Currency));

                    col.Item().Element(c => InstitutionTotals(c,
                        invoice.Amount, invoice.KdvRate, invoice.KdvAmount,
                        invoice.WithholdingApplies, invoice.WithholdingAmount,
                        invoice.WithholdingCode,
                        invoice.NetPayableAmount, invoice.Currency));

                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                        col.Item().Element(c => NotesBox(c, invoice.Notes));

                    col.Item().Element(SimulationFooter);
                });
            });
        }).GeneratePdf();
    }

    // ── Shared components ─────────────────────────────────────────────────────

    static void InvoiceHeader(IContainer c, string companyName, string branchName, string? vkn, string invoiceTypeLabel)
    {
        c.BorderBottom(1).BorderColor("#1a56db").PaddingBottom(10).Row(row =>
        {
            row.RelativeItem(2).Column(col =>
            {
                col.Item().Text(companyName).FontSize(14).Bold().FontColor("#1a56db");
                col.Item().Text(branchName).FontSize(9).FontColor("#6b7280");
                if (vkn != null)
                    col.Item().Text($"VKN: {vkn}").FontSize(8).FontColor("#6b7280");
            });

            row.RelativeItem(2).AlignCenter().Column(col =>
            {
                col.Item().AlignCenter()
                    .Background("#1a56db")
                    .Padding(6)
                    .Text(invoiceTypeLabel)
                    .FontSize(13).Bold().FontColor(Colors.White);
            });

            row.RelativeItem(1); // boşluk
        });
    }

    static void InfoRow(IContainer c, (string Label, string Value)[] sellerItems, (string Label, string Value)[] invoiceItems)
    {
        c.Row(row =>
        {
            row.RelativeItem().Border(0.5f).BorderColor("#e5e7eb").Padding(8).Column(col =>
            {
                col.Item().Text("SATICI BİLGİLERİ").FontSize(8).Bold().FontColor("#374151");
                col.Item().PaddingTop(4);
                foreach (var (label, value) in sellerItems)
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text(label + ":").FontSize(8).FontColor("#6b7280");
                        r.RelativeItem(2).Text(value).FontSize(8);
                    });
            });

            row.ConstantItem(12); // gutter

            row.RelativeItem().Border(0.5f).BorderColor("#e5e7eb").Padding(8).Column(col =>
            {
                col.Item().Text("FATURA BİLGİLERİ").FontSize(8).Bold().FontColor("#374151");
                col.Item().PaddingTop(4);
                foreach (var (label, value) in invoiceItems)
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text(label + ":").FontSize(8).FontColor("#6b7280");
                        r.RelativeItem(2).Text(value).FontSize(8).Bold();
                    });
            });
        });
    }

    static void RecipientBox(IContainer c, string name, (string Label, string Value) idField,
        string? taxOffice, string? patientName = null)
    {
        c.Border(0.5f).BorderColor("#e5e7eb").Background("#f9fafb").Padding(10).Column(col =>
        {
            col.Item().Text("ALICI BİLGİLERİ").FontSize(8).Bold().FontColor("#374151");
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Text(name).FontSize(10).Bold();
                    inner.Item().Text($"{idField.Label}: {idField.Value}").FontSize(8).FontColor("#6b7280");
                    if (!string.IsNullOrWhiteSpace(taxOffice))
                        inner.Item().Text($"Vergi Dairesi: {taxOffice}").FontSize(8).FontColor("#6b7280");
                });
                if (!string.IsNullOrWhiteSpace(patientName))
                    row.RelativeItem().AlignRight().Column(inner =>
                    {
                        inner.Item().Text("Hasta").FontSize(8).FontColor("#6b7280");
                        inner.Item().Text(patientName).FontSize(9).Bold();
                    });
            });
        });
    }

    static void LineItemsTable(IContainer c, List<InvoiceLineItem> items, string currency)
    {
        c.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(4); // tedavi adı
                cols.RelativeColumn(1); // diş no
                cols.RelativeColumn(1); // kdv %
                cols.RelativeColumn(2); // tutar
            });

            static IContainer HeaderCell(IContainer cell) =>
                cell.Background("#1a56db").Padding(5);

            static IContainer DataCell(IContainer cell, int row) =>
                cell.Background(row % 2 == 0 ? "#f9fafb" : Colors.White).Padding(5);

            table.Header(h =>
            {
                h.Cell().Element(HeaderCell).Text("Hizmet Adı").FontSize(8).Bold().FontColor(Colors.White);
                h.Cell().Element(HeaderCell).AlignCenter().Text("Diş No").FontSize(8).Bold().FontColor(Colors.White);
                h.Cell().Element(HeaderCell).AlignCenter().Text("KDV %").FontSize(8).Bold().FontColor(Colors.White);
                h.Cell().Element(HeaderCell).AlignRight().Text("Tutar (KDV Dahil)").FontSize(8).Bold().FontColor(Colors.White);
            });

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var rowIndex = i;

                table.Cell().Element(cell => DataCell(cell, rowIndex)).Text(item.Name).FontSize(8);
                table.Cell().Element(cell => DataCell(cell, rowIndex)).AlignCenter().Text(item.ToothNumber ?? "—").FontSize(8);
                table.Cell().Element(cell => DataCell(cell, rowIndex)).AlignCenter().Text($"%{item.KdvRate:0}").FontSize(8);
                table.Cell().Element(cell => DataCell(cell, rowIndex)).AlignRight()
                    .Text(Fmt(item.Amount, currency)).FontSize(8);
            }
        });
    }

    static void PatientTotals(IContainer c, decimal matrah, decimal kdvRate, decimal kdvAmount, decimal total, string currency)
    {
        c.AlignRight().Width(250).Column(col =>
        {
            col.Item().BorderBottom(0.5f).BorderColor("#e5e7eb").PaddingBottom(4).Row(row =>
            {
                row.RelativeItem().Text("Matrah (KDV Hariç)").FontSize(9);
                row.ConstantItem(120).AlignRight().Text(Fmt(matrah, currency)).FontSize(9);
            });
            col.Item().PaddingTop(4).PaddingBottom(4).BorderBottom(0.5f).BorderColor("#e5e7eb").Row(row =>
            {
                row.RelativeItem().Text($"KDV (%{kdvRate * 100:0})").FontSize(9);
                row.ConstantItem(120).AlignRight().Text(Fmt(kdvAmount, currency)).FontSize(9);
            });
            col.Item().PaddingTop(6).Background("#1a56db").Padding(6).Row(row =>
            {
                row.RelativeItem().Text("TOPLAM (KDV Dahil)").FontSize(10).Bold().FontColor(Colors.White);
                row.ConstantItem(120).AlignRight().Text(Fmt(total, currency)).FontSize(10).Bold().FontColor(Colors.White);
            });
        });
    }

    static void InstitutionTotals(IContainer c, decimal matrah, decimal kdvRate, decimal kdvAmount,
        bool withholdingApplies, decimal withholdingAmount, string? withholdingCode,
        decimal netPayable, string currency)
    {
        c.AlignRight().Width(280).Column(col =>
        {
            col.Item().BorderBottom(0.5f).BorderColor("#e5e7eb").PaddingBottom(4).Row(row =>
            {
                row.RelativeItem().Text("Matrah (KDV Hariç)").FontSize(9);
                row.ConstantItem(140).AlignRight().Text(Fmt(matrah, currency)).FontSize(9);
            });

            var kdvLabel = $"KDV (%{kdvRate * 100:0})" +
                (withholdingApplies ? "  [Tevkifat Uygulanır]" : "");
            col.Item().PaddingTop(4).PaddingBottom(4).BorderBottom(0.5f).BorderColor("#e5e7eb").Row(row =>
            {
                row.RelativeItem().Text(kdvLabel).FontSize(9);
                row.ConstantItem(140).AlignRight().Text(Fmt(kdvAmount, currency)).FontSize(9);
            });

            if (withholdingApplies && withholdingAmount > 0)
            {
                var label = string.IsNullOrWhiteSpace(withholdingCode)
                    ? "Tevkifat (-)".ToString()
                    : $"Tevkifat {withholdingCode} (-)";
                col.Item().PaddingTop(2).PaddingBottom(4).BorderBottom(0.5f).BorderColor("#e5e7eb").Row(row =>
                {
                    row.RelativeItem().Text(label).FontSize(9).FontColor("#dc2626");
                    row.ConstantItem(140).AlignRight().Text($"−{Fmt(withholdingAmount, currency)}").FontSize(9).FontColor("#dc2626");
                });
            }

            col.Item().PaddingTop(6).Background("#1a56db").Padding(6).Row(row =>
            {
                row.RelativeItem().Text("NET ÖDENECEk TUTAR").FontSize(10).Bold().FontColor(Colors.White);
                row.ConstantItem(140).AlignRight().Text(Fmt(netPayable, currency)).FontSize(10).Bold().FontColor(Colors.White);
            });
        });
    }

    static void NotesBox(IContainer c, string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return;
        c.Border(0.5f).BorderColor("#fbbf24").Background("#fefce8").Padding(8).Column(col =>
        {
            col.Item().Text("Not:").FontSize(8).Bold().FontColor("#92400e");
            col.Item().Text(notes).FontSize(8).FontColor("#78350f");
        });
    }

    static void SimulationFooter(IContainer c)
    {
        c.BorderTop(0.5f).BorderColor("#e5e7eb").PaddingTop(6).Column(col =>
        {
            col.Item().AlignCenter()
                .Text("Bu belge simülasyon amaçlı üretilmiştir — gerçek GİB entegrasyonu aktif değildir.")
                .FontSize(7).Italic().FontColor("#9ca3af");
            col.Item().AlignCenter()
                .Text($"Üretim zamanı: {DateTime.Now:dd.MM.yyyy HH:mm:ss}")
                .FontSize(7).FontColor("#d1d5db");
        });
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────────

    private async Task<List<InvoiceLineItem>> LoadTreatmentLineItemsAsync(string? treatmentItemIdsJson, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(treatmentItemIdsJson)) return [];

        List<long>? ids;
        try { ids = JsonSerializer.Deserialize<List<long>>(treatmentItemIdsJson); }
        catch { return []; }

        if (ids == null || ids.Count == 0) return [];

        var items = await _db.TreatmentPlanItems
            .AsNoTracking()
            .Include(i => i.Treatment)
            .Where(i => ids.Contains(i.Id))
            .ToListAsync(ct);

        return items.Select(i => new InvoiceLineItem(
            i.Treatment?.Name ?? "Tedavi",
            i.ToothNumber,
            i.KdvRate,
            i.TotalAmount
        )).ToList();
    }

    static string Fmt(decimal amount, string currency)
    {
        var n = amount.ToString("N2", TR);
        return currency switch
        {
            "TRY" => $"₺{n}",
            "EUR" => $"€{n}",
            "USD" => $"${n}",
            "GBP" => $"£{n}",
            "CHF" => $"Fr {n}",
            _     => $"{n} {currency}",
        };
    }
}

record InvoiceLineItem(string Name, string? ToothNumber, decimal KdvRate, decimal Amount);
