using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Oravity.Core.Services;

/// <summary>
/// İmzalanan onam formunun yasal geçerli PDF çıktısını üretir.
/// Form içeriği (HTML → düz metin), checkbox cevapları, imza resmi ve zaman damgasını içerir.
/// </summary>
public class ConsentPdfService
{
    private readonly AppDbContext _db;

    public ConsentPdfService(AppDbContext db) => _db = db;

    public async Task<byte[]> GenerateAsync(Guid instancePublicId, CancellationToken ct = default)
    {
        var instance = await _db.ConsentInstances
            .AsNoTracking()
            .Include(ci => ci.Patient)
            .Include(ci => ci.FormTemplate)
            .Include(ci => ci.TreatmentPlan).ThenInclude(p => p!.Branch).ThenInclude(b => b.Company)
            .Include(ci => ci.TreatmentPlan).ThenInclude(p => p!.Doctor)
            .FirstOrDefaultAsync(ci => ci.PublicId == instancePublicId, ct)
            ?? throw new NotFoundException("Onam formu bulunamadı.");

        if (instance.Status != ConsentInstanceStatus.Signed)
            throw new InvalidOperationException("Yalnızca imzalanan onam formları PDF olarak indirilebilir.");

        QuestPDF.Settings.License = LicenseType.Community;

        var checkboxDefs  = ParseCheckboxDefs(instance.FormTemplate?.CheckboxesJson ?? "[]");
        var checkboxAnswers = ParseCheckboxAnswers(instance.CheckboxAnswersJson ?? "[]");

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.4f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

                page.Header().Element(c => RenderHeader(c, instance));
                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Element(c => RenderPatientInfo(c, instance));
                    col.Item().Element(c => RenderFormContent(c, instance.FormTemplate?.ContentHtml ?? ""));
                    if (checkboxDefs.Count > 0)
                        col.Item().Element(c => RenderCheckboxes(c, checkboxDefs, checkboxAnswers));
                    col.Item().Element(c => RenderSignatureBlock(c, instance));
                });
                page.Footer().Element(RenderFooter);
            });
        });

        return doc.GeneratePdf();
    }

    // ── Header ────────────────────────────────────────────────────────────────

    static void RenderHeader(IContainer container, ConsentInstance ci)
    {
        var branch  = ci.TreatmentPlan?.Branch;
        var company = branch?.Company;

        container.BorderBottom(1).BorderColor("#d1d5db").PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(company?.Name ?? "—").FontSize(13).Bold().FontColor("#1d4ed8");
                col.Item().Text(branch?.Name  ?? "—").FontSize(8).FontColor("#6b7280");
            });

            row.RelativeItem().AlignCenter().Column(col =>
            {
                col.Item().AlignCenter().Text("HASTA ONAM FORMU").FontSize(15).Bold().FontColor("#111827");
                col.Item().AlignCenter().Text($"Form No: {ci.ConsentCode}").FontSize(8).FontColor("#6b7280").Italic();
            });

            row.ConstantItem(80).AlignRight().Column(col =>
            {
                col.Item().AlignRight().Text("Tarih").FontSize(7).FontColor("#9ca3af");
                col.Item().AlignRight().Text(
                    (ci.SignedAt ?? ci.CreatedAt).ToString("dd.MM.yyyy")).FontSize(8).Bold();
            });
        });
    }

    // ── Hasta Bilgileri ───────────────────────────────────────────────────────

    static void RenderPatientInfo(IContainer container, ConsentInstance ci)
    {
        var p      = ci.Patient;
        var doctor = ci.TreatmentPlan?.Doctor;

        container.Background("#f8fafc").Border(1).BorderColor("#e2e8f0").Padding(10).Column(col =>
        {
            col.Item().Text("Hasta Bilgileri").FontSize(8).Bold().FontColor("#475569");
            col.Spacing(4);
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    InfoRow(left, "Ad Soyad",  p is not null ? $"{p.FirstName} {p.LastName}" : "—");
                    InfoRow(left, "Doğum Tarihi", p?.BirthDate?.ToString("dd.MM.yyyy") ?? "—");
                    InfoRow(left, "Cinsiyet",   p?.Gender ?? "—");
                });
                row.RelativeItem().Column(right =>
                {
                    InfoRow(right, "Telefon",  p?.Phone ?? "—");
                    InfoRow(right, "Hekim",    doctor?.FullName ?? "—");
                    InfoRow(right, "Form Şablonu", ci.FormTemplate?.Name ?? "—");
                });
            });
        });
    }

    static void InfoRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(90).Text(label + " :").FontSize(8).FontColor("#64748b").SemiBold();
            r.RelativeItem().Text(value).FontSize(8);
        });
    }

    // ── Form İçeriği ──────────────────────────────────────────────────────────

    static void RenderFormContent(IContainer container, string html)
    {
        var plain = HtmlToPlainText(html);
        if (string.IsNullOrWhiteSpace(plain)) return;

        container.Column(col =>
        {
            col.Item().Text("Onam Metni").FontSize(8).Bold().FontColor("#475569");
            col.Spacing(4);
            col.Item().Border(1).BorderColor("#e2e8f0").Padding(10)
                .Text(plain).FontSize(8.5f).LineHeight(1.5f);
        });
    }

    // ── Checkbox'lar ─────────────────────────────────────────────────────────

    static void RenderCheckboxes(IContainer container,
        List<CheckboxDef> defs, Dictionary<string, bool> answers)
    {
        container.Column(col =>
        {
            col.Item().Text("Onay Maddeleri").FontSize(8).Bold().FontColor("#475569");
            col.Spacing(3);
            col.Item().Border(1).BorderColor("#e2e8f0").Padding(10).Column(inner =>
            {
                inner.Spacing(5);
                foreach (var def in defs)
                {
                    var checked_ = answers.TryGetValue(def.Id, out var v) && v;
                    inner.Item().Row(row =>
                    {
                        // Checkbox kutusu simgesi
                        row.ConstantItem(14).AlignTop().Text(checked_ ? "☑" : "☐")
                            .FontSize(10)
                            .FontColor(checked_ ? "#16a34a" : "#dc2626");
                        row.RelativeItem().PaddingLeft(4)
                            .Text(def.Label).FontSize(8.5f).LineHeight(1.4f);
                    });
                }
            });
        });
    }

    // ── İmza Bloğu ───────────────────────────────────────────────────────────

    static void RenderSignatureBlock(IContainer container, ConsentInstance ci)
    {
        var hasDoctorSig = !string.IsNullOrWhiteSpace(ci.DoctorSignatureDataBase64);
        var requiresDoctor = ci.FormTemplate?.RequireDoctorSignature ?? false;

        container.Border(1).BorderColor("#d1d5db").Padding(12).Column(col =>
        {
            col.Item().Text("İmza Bilgileri").FontSize(8).Bold().FontColor("#475569");
            col.Spacing(6);

            col.Item().Row(row =>
            {
                // Sol: imzalayan bilgileri
                row.RelativeItem().Column(left =>
                {
                    left.Spacing(4);
                    InfoRow(left, "İmzalayan",   ci.SignerName   ?? "—");
                    InfoRow(left, "İmzalandı",   ci.SignedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—");
                    InfoRow(left, "IP Adresi",   ci.SignerIp     ?? "—");
                    InfoRow(left, "Cihaz",       ci.SignerDevice ?? "—");
                });

                // Hasta imzası
                row.ConstantItem(160).Border(1).BorderColor("#e2e8f0")
                    .Padding(4).Column(sigCol =>
                {
                    sigCol.Item().AlignCenter().Text("Hasta / Vasi İmzası").FontSize(7).FontColor("#9ca3af");
                    sigCol.Item().AlignCenter().Height(70).Element(img =>
                        RenderSignatureImage(img, ci.SignatureDataBase64));
                });
            });

            // Doktor imzası satırı — şablon gerektiriyorsa veya imza verisi varsa göster
            if (requiresDoctor || hasDoctorSig)
            {
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text("Hekim İmzası").FontSize(8).SemiBold().FontColor("#475569");
                        left.Item().Text(ci.TreatmentPlan?.Doctor?.FullName ?? "—").FontSize(8);
                    });

                    row.ConstantItem(160).Border(1).BorderColor("#e2e8f0")
                        .Padding(4).Column(sigCol =>
                    {
                        sigCol.Item().AlignCenter().Text("Hekim İmzası").FontSize(7).FontColor("#9ca3af");
                        sigCol.Item().AlignCenter().Height(70).Element(img =>
                            RenderSignatureImage(img, ci.DoctorSignatureDataBase64));
                    });
                });
            }

            // Doğrulama notu
            col.Item().PaddingTop(6).Text(
                $"Bu belge {ci.SignedAt?.ToString("dd.MM.yyyy HH:mm:ss")} UTC tarihinde dijital olarak imzalanmıştır. " +
                $"Form No: {ci.ConsentCode}")
                .FontSize(6.5f).FontColor("#94a3b8").Italic();
        });
    }

    static void RenderSignatureImage(IContainer container, string? dataUri)
    {
        if (!string.IsNullOrWhiteSpace(dataUri))
        {
            try
            {
                var b64 = dataUri.Contains(',') ? dataUri[(dataUri.IndexOf(',') + 1)..] : dataUri;
                var bytes = Convert.FromBase64String(b64);
                container.Image(bytes).FitArea();
            }
            catch
            {
                container.AlignCenter().AlignMiddle()
                    .Text("İmza gösterilemiyor").FontSize(7).FontColor("#9ca3af").Italic();
            }
        }
        else
        {
            container.AlignCenter().AlignMiddle()
                .Text("İmza yok").FontSize(7).FontColor("#9ca3af").Italic();
        }
    }

    // ── Footer ────────────────────────────────────────────────────────────────

    static void RenderFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor("#e2e8f0").PaddingTop(5).Row(row =>
        {
            row.RelativeItem().Text("Bu belge yasal geçerli dijital onam kaydıdır. Aslına uygunluğu sistem üzerinden doğrulanabilir.")
                .FontSize(6.5f).FontColor("#94a3b8").Italic();
            row.ConstantItem(60).AlignRight()
                .Text(t =>
                {
                    t.Span("Sayfa ").FontSize(7).FontColor("#94a3b8");
                    t.CurrentPageNumber().FontSize(7).FontColor("#94a3b8");
                    t.Span(" / ").FontSize(7).FontColor("#94a3b8");
                    t.TotalPages().FontSize(7).FontColor("#94a3b8");
                });
        });
    }

    // ── HTML → Düz Metin ─────────────────────────────────────────────────────

    static string HtmlToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";

        // Blok elementleri newline'a çevir
        html = Regex.Replace(html, @"<(br|p|div|h[1-6]|li|tr)[^>]*>", "\n", RegexOptions.IgnoreCase);
        // Tüm HTML tag'larını kaldır
        html = Regex.Replace(html, @"<[^>]+>", "");
        // HTML entity decode
        html = System.Net.WebUtility.HtmlDecode(html);
        // Fazladan boşluk / satır temizle
        html = Regex.Replace(html, @"[ \t]+", " ");
        html = Regex.Replace(html, @"\n{3,}", "\n\n");
        return html.Trim();
    }

    // ── JSON Parse yardımcıları ───────────────────────────────────────────────

    record CheckboxDef(string Id, string Label);

    static List<CheckboxDef> ParseCheckboxDefs(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateArray()
                .Select(el => new CheckboxDef(
                    el.TryGetProperty("id",    out var id)    ? id.GetString()    ?? "" : "",
                    el.TryGetProperty("label", out var label) ? label.GetString() ?? "" : ""))
                .Where(d => !string.IsNullOrWhiteSpace(d.Id))
                .ToList();
        }
        catch { return []; }
    }

    static Dictionary<string, bool> ParseCheckboxAnswers(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateArray()
                .ToDictionary(
                    el => el.TryGetProperty("id",      out var id)  ? id.GetString()    ?? "" : "",
                    el => el.TryGetProperty("checked", out var chk) && chk.GetBoolean());
        }
        catch { return []; }
    }
}
