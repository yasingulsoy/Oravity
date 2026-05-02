using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net.Http.Json;
using System.Text;

namespace Oravity.Core.Services;

// ── Görüntüleme için düzleştirilmiş kalem ──────────────────────────────────
record PdfLineItem(
    string? ToothNumber,
    string  TreatmentName,
    decimal ListPrice,    // indirim öncesi
    decimal FinalPrice,   // indirim sonrası
    decimal DiscountRate,
    string  Currency,
    decimal TryEquivalent // her zaman TRY karşılığı
);

public class TreatmentPlanPdfService
{
    static readonly HashSet<string> SupportedCurrencies = ["TRY", "USD", "EUR", "CHF", "GBP"];

    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public TreatmentPlanPdfService(AppDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<byte[]> GenerateAsync(Guid planPublicId, string? displayCurrency = null, CancellationToken ct = default)
    {
        var targetCurrency = displayCurrency?.ToUpperInvariant() is { } c && SupportedCurrencies.Contains(c) ? c : null;

        // ── Veri yükle ─────────────────────────────────────────────────────
        var plan = await _db.TreatmentPlans
            .AsNoTracking()
            .Include(p => p.Patient).ThenInclude(p => p.AgreementInstitution)
            .Include(p => p.Doctor)
            .Include(p => p.Branch).ThenInclude(b => b.Company)
            .Include(p => p.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Treatment)
            .FirstOrDefaultAsync(p => p.PublicId == planPublicId, ct)
            ?? throw new NotFoundException($"Tedavi planı bulunamadı: {planPublicId}");

        // ── Döviz kurları: 1 yabancı = X TRY ──────────────────────────────
        var fxRates = await FetchFxRatesAsync(ct);  // {"USD": 44.87, "EUR": 52.79, ...}

        // ── Görüntüleme kalemleri ──────────────────────────────────────────
        var lines = plan.Items.Select(item =>
        {
            var origCur     = item.PriceCurrency;
            // Liste fiyatı: referans listedeki ham fiyat (kampanya/kural öncesi, KDV dahil).
            // Yoksa (eski kalem) UnitPrice'a düş — PDF'de fark gösterilmez.
            var rawList     = item.ListPrice ?? item.UnitPrice;
            var tryList     = ToTry(rawList,          origCur, fxRates);
            // TotalAmount = KDV dahil, hastanın ödediği tutar
            var tryFinal    = ToTry(item.TotalAmount, origCur, fxRates);

            string displayCur;
            decimal displayList, displayFinal;

            if (targetCurrency is null)
            {
                // Orijinal para biriminde göster
                displayCur   = origCur;
                displayList  = rawList;
                displayFinal = item.TotalAmount;
            }
            else
            {
                // Seçilen dövize çevir
                displayCur   = targetCurrency;
                displayList  = FromTry(tryList,  targetCurrency, fxRates);
                displayFinal = FromTry(tryFinal, targetCurrency, fxRates);
            }

            return new PdfLineItem(
                item.ToothNumber,
                item.Treatment?.Name ?? "—",
                displayList,
                displayFinal,
                item.DiscountRate,
                displayCur,
                tryFinal
            );
        }).ToList();

        var treatedTeeth = plan.Items
            .Where(i => !string.IsNullOrWhiteSpace(i.ToothNumber))
            .Select(i => i.ToothNumber!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Diş numarası → simge kodu (ilk eşleşen simgeli tedavi)
        var toothSymbols = plan.Items
            .Where(i => !string.IsNullOrWhiteSpace(i.ToothNumber)
                     && !string.IsNullOrWhiteSpace(i.Treatment?.ChartSymbolCode))
            .GroupBy(i => i.ToothNumber!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Treatment!.ChartSymbolCode!,
                          StringComparer.OrdinalIgnoreCase);

        // ── PDF ────────────────────────────────────────────────────────────
        QuestPDF.Settings.License = LicenseType.Community;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(8));

                page.Header().Element(c => Header(c, plan.Branch.Company.Name, plan.Branch.Name, targetCurrency));
                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Element(c => PatientInfo(c, plan, fxRates));
                    col.Item().Element(c => DentalChart(c, treatedTeeth, toothSymbols));
                    col.Item().Element(c => TreatmentTable(c, lines, fxRates, targetCurrency));
                    if (lines.Any(l => l.ListPrice > l.FinalPrice))
                        col.Item().Element(c => PriceSummaryBox(c, lines, fxRates));
                });
                page.Footer().Element(c => Footer(c));
            });
        });

        return doc.GeneratePdf();
    }

    // ── Kur yardımcıları ───────────────────────────────────────────────────

    static decimal ToTry(decimal amount, string fromCurrency, Dictionary<string, decimal> fxRates)
    {
        if (fromCurrency == "TRY") return amount;
        return fxRates.TryGetValue(fromCurrency, out var r) ? Math.Round(amount * r, 2) : amount;
    }

    static decimal FromTry(decimal tryAmount, string toCurrency, Dictionary<string, decimal> fxRates)
    {
        if (toCurrency == "TRY") return tryAmount;
        return fxRates.TryGetValue(toCurrency, out var r) && r > 0
            ? Math.Round(tryAmount / r, 2)
            : tryAmount;
    }

    static readonly System.Globalization.CultureInfo TR = new("tr-TR");
    static readonly System.Globalization.CultureInfo EN = System.Globalization.CultureInfo.InvariantCulture;

    static string Fmt(decimal amount, string currency, bool noSymbol = false)
    {
        var n = currency == "TRY"
            ? amount.ToString("N2", TR)
            : amount.ToString("N2", EN);
        if (noSymbol) return n;
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

    // ── Header ─────────────────────────────────────────────────────────────

    static void Header(IContainer container, string companyName, string branchName, string? currency)
    {
        container.BorderBottom(1).BorderColor("#dddddd").PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(companyName).FontSize(14).Bold().FontColor("#1a56db");
                col.Item().Text(branchName).FontSize(8).FontColor("#6b7280");
            });

            row.RelativeItem().AlignCenter().Column(col =>
            {
                col.Item().AlignCenter().Text("Tedavi Planı").FontSize(18).Bold().FontColor("#111827");
                if (currency is not null && currency != "TRY")
                    col.Item().AlignCenter().Text($"Fiyatlar {currency} cinsinden").FontSize(7).FontColor("#6b7280").Italic();
            });

            row.RelativeItem().AlignRight().Text("").FontSize(8);
        });
    }

    // ── Hasta Bilgileri ────────────────────────────────────────────────────

    static void PatientInfo(IContainer container, SharedKernel.Entities.TreatmentPlan plan, Dictionary<string, decimal> fxRates)
    {
        var patient     = plan.Patient;
        var doctor      = plan.Doctor;
        var institution = patient.AgreementInstitution;

        container.Background("#f9fafb").Border(1).BorderColor("#e5e7eb").Padding(8).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Row(r => { r.ConstantItem(80).Text("Hasta Adı :").SemiBold(); r.RelativeItem().Text($"{patient.FirstName} {patient.LastName}"); });
                col.Item().Row(r => { r.ConstantItem(80).Text("Hasta ID :").SemiBold();  r.RelativeItem().Text(patient.PublicId.ToString()[..8].ToUpper()).FontFamily("Courier New"); });
                col.Item().Row(r => { r.ConstantItem(80).Text("Kurum :").SemiBold();     r.RelativeItem().Text(institution?.Name ?? "—"); });
            });

            row.RelativeItem().Column(col =>
            {
                col.Item().Row(r => { r.ConstantItem(80).Text("Departman :").SemiBold(); r.RelativeItem().Text("Diş Hastalıkları ve Tedavisi"); });
                col.Item().Row(r => { r.ConstantItem(80).Text("Hekim :").SemiBold();     r.RelativeItem().Text(doctor.FullName); });
                col.Item().Row(r => { r.ConstantItem(80).Text("Tarih :").SemiBold();     r.RelativeItem().Text(plan.CreatedAt.ToString("dd.MM.yyyy")); });
            });

            // Döviz kurları: 1 EUR = ₺X
            var relevantCurrencies = plan.Items
                .Select(i => i.PriceCurrency)
                .Where(c => c != "TRY")
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            if (relevantCurrencies.Count > 0)
            {
                row.ConstantItem(100).Column(col =>
                {
                    col.Item().Text("Güncel Kur").FontSize(7).FontColor("#6b7280").SemiBold();
                    foreach (var code in relevantCurrencies)
                    {
                        if (!fxRates.TryGetValue(code, out var tryPerOne) || tryPerOne == 0) continue;
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"1 {code} =").FontSize(7);
                            r.ConstantItem(52).AlignRight().Text(Fmt(tryPerOne, "TRY")).FontFamily("Courier New").FontSize(7);
                        });
                    }
                });
            }
            else
            {
                row.ConstantItem(100);
            }
        });
    }

    // ── Diş Şeması ─────────────────────────────────────────────────────────

    static void DentalChart(IContainer container, HashSet<string> treatedTeeth, Dictionary<string, string> toothSymbols)
    {
        var svg = BuildDentalChartSvg(treatedTeeth, toothSymbols);

        container.Border(1).BorderColor("#e5e7eb").Padding(8).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(20).AlignMiddle().Text("R").Bold().FontColor("#dc2626");
                row.RelativeItem().AlignCenter().Text("Üst Çene").FontSize(7).FontColor("#6b7280");
                row.ConstantItem(20).AlignRight().AlignMiddle().Text("L").Bold().FontColor("#dc2626");
            });
            col.Item().Svg(svg);
            col.Item().Row(row =>
            {
                row.ConstantItem(20).AlignMiddle().Text("R").Bold().FontColor("#dc2626");
                row.RelativeItem().AlignCenter().Text("Alt Çene").FontSize(7).FontColor("#6b7280");
                row.ConstantItem(20).AlignRight().AlignMiddle().Text("L").Bold().FontColor("#dc2626");
            });
        });
    }

    static string BuildDentalChartSvg(HashSet<string> treatedTeeth, Dictionary<string, string> toothSymbols)
    {
        var upperTeeth = new[] { "18","17","16","15","14","13","12","11","21","22","23","24","25","26","27","28" };
        var lowerTeeth = new[] { "48","47","46","45","44","43","42","41","31","32","33","34","35","36","37","38" };

        const int toothW = 28, toothH = 36, gap = 2, centerGap = 6;
        const int totalW = 16 * toothW + 15 * gap + centerGap + 40;
        const int totalH = 2 * toothH + 30;
        const int startX = 20;

        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {totalW} {totalH}\" width=\"{totalW}\" height=\"{totalH}\">");

        void DrawTooth(int index, int row, string toothNum)
        {
            var x = startX + index * (toothW + gap) + (index >= 8 ? centerGap : 0);
            var y = row == 0 ? 10 : toothH + 20;

            var treated   = treatedTeeth.Contains(toothNum);
            var fill      = treated ? "#dbeafe" : "#ffffff";
            var stroke    = treated ? "#1d4ed8" : "#9ca3af";
            var textFill  = treated ? "#1d4ed8" : "#374151";

            // Diş gövdesi
            sb.AppendLine($"<rect x=\"{x}\" y=\"{y}\" width=\"{toothW}\" height=\"{toothH - 8}\" rx=\"4\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"1\"/>");
            sb.AppendLine($"<rect x=\"{x + 8}\" y=\"{y + toothH - 10}\" width=\"{toothW - 16}\" height=\"10\" rx=\"2\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"1\"/>");

            // Tedavi simgesi — nested <svg> ile güvenli konumlandırma (viewBox=36×44)
            if (toothSymbols.TryGetValue(toothNum, out var symCode))
            {
                var overlay = GetSymbolSvg(symCode);
                if (overlay is not null)
                    sb.AppendLine($"<svg x=\"{x}\" y=\"{y}\" width=\"{toothW}\" height=\"{toothH - 8}\" viewBox=\"0 0 36 44\" preserveAspectRatio=\"none\">{overlay}</svg>");
            }

            var numY = row == 0 ? y - 2 : y + toothH + 8;
            sb.AppendLine($"<text x=\"{x + toothW / 2}\" y=\"{numY}\" text-anchor=\"middle\" font-family=\"Arial\" font-size=\"7\" fill=\"{textFill}\">{toothNum}</text>");
        }

        for (int i = 0; i < upperTeeth.Length; i++) DrawTooth(i, 0, upperTeeth[i]);

        var midY = toothH + 16;
        sb.AppendLine($"<line x1=\"{startX}\" y1=\"{midY}\" x2=\"{startX + 16 * (toothW + gap) + centerGap}\" y2=\"{midY}\" stroke=\"#d1d5db\" stroke-width=\"1\" stroke-dasharray=\"4,3\"/>");

        for (int i = 0; i < lowerTeeth.Length; i++) DrawTooth(i, 1, lowerTeeth[i]);

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    /// <summary>
    /// Sembol kodunu 36×44 viewBox'ta çizilmiş SVG element string'ine dönüştürür.
    /// Frontend'deki dentalSymbols.tsx ile koordinat sistemi aynıdır.
    /// </summary>
    static string? GetSymbolSvg(string code) => code switch
    {
        // ── Cerrahi ──────────────────────────────────────────────────────────
        "extraction" =>
            """<line x1="8" y1="6" x2="28" y2="38" stroke="#ef4444" stroke-width="3" stroke-linecap="round"/>""" +
            """<line x1="28" y1="6" x2="8" y2="38" stroke="#ef4444" stroke-width="3" stroke-linecap="round"/>""",

        "implant" =>
            """<line x1="18" y1="6" x2="18" y2="38" stroke="#7c3aed" stroke-width="2.5" stroke-linecap="round"/>""" +
            """<line x1="12" y1="11" x2="24" y2="11" stroke="#7c3aed" stroke-width="1.3"/>""" +
            """<line x1="12" y1="17" x2="24" y2="17" stroke="#7c3aed" stroke-width="1.3"/>""" +
            """<line x1="12" y1="23" x2="24" y2="23" stroke="#7c3aed" stroke-width="1.3"/>""" +
            """<line x1="12" y1="29" x2="24" y2="29" stroke="#7c3aed" stroke-width="1.3"/>""" +
            """<rect x="11" y="4" width="14" height="5" rx="2" fill="#7c3aed" opacity="0.9"/>""",

        "sinus-lift" =>
            """<path d="M4,10 Q10,2 18,6 Q26,2 32,10" fill="none" stroke="#0284c7" stroke-width="2" stroke-linecap="round"/>""" +
            """<path d="M4,16 Q10,8 18,12 Q26,8 32,16" fill="none" stroke="#0284c7" stroke-width="1.5" stroke-linecap="round" opacity="0.6"/>""",

        "graft" =>
            """<rect x="8" y="28" width="20" height="12" rx="2" fill="none" stroke="#b45309" stroke-width="1.8" stroke-dasharray="3,2"/>""" +
            """<text x="18" y="37" text-anchor="middle" font-size="7" fill="#b45309" font-family="Arial" font-weight="bold">G</text>""",

        "broken" =>
            """<path d="M21,2 L17,14 L24,18 L14,42" fill="none" stroke="#ea580c" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>""",

        "root" =>
            """<line x1="8" y1="14" x2="28" y2="14" stroke="#78716c" stroke-width="2" stroke-linecap="round"/>""" +
            """<line x1="13" y1="14" x2="11" y2="40" stroke="#78716c" stroke-width="2.2" stroke-linecap="round"/>""" +
            """<line x1="23" y1="14" x2="25" y2="40" stroke="#78716c" stroke-width="2.2" stroke-linecap="round"/>""",

        // ── Protetik ─────────────────────────────────────────────────────────
        "crown" =>
            """<path d="M6,38 L6,16 L12,24 L18,8 L24,24 L30,16 L30,38 Z" fill="none" stroke="#92400e" stroke-width="2" stroke-linejoin="round"/>""",

        "bridge" =>
            """<rect x="0" y="1" width="36" height="6" fill="#0e7490" opacity="0.7" rx="1"/>""" +
            """<line x1="0" y1="7" x2="36" y2="7" stroke="#0e7490" stroke-width="1"/>""",

        "veneer" =>
            """<polygon points="0,0 36,0 28,12 8,12" fill="#fbcfe8" stroke="#db2777" stroke-width="1.8" opacity="0.85"/>""",

        "inlay" =>
            """<rect x="10" y="14" width="16" height="16" rx="2" fill="#a7f3d0" stroke="#059669" stroke-width="1.8"/>""",

        "protez" =>
            """<rect x="3" y="4" width="30" height="36" rx="4" fill="none" stroke="#6d28d9" stroke-width="1.8" stroke-dasharray="4,3"/>""",

        // ── Endodonti ────────────────────────────────────────────────────────
        "root-canal" =>
            """<line x1="15" y1="8" x2="13" y2="40" stroke="#c2410c" stroke-width="2.2" stroke-linecap="round"/>""" +
            """<line x1="21" y1="8" x2="23" y2="40" stroke="#c2410c" stroke-width="2.2" stroke-linecap="round"/>""",

        "apikal" =>
            """<circle cx="18" cy="20" r="9" fill="#fda4af" stroke="#e11d48" stroke-width="1.5"/>""" +
            """<line x1="18" y1="15" x2="18" y2="22" stroke="#881337" stroke-width="2.5" stroke-linecap="round"/>""" +
            """<circle cx="18" cy="25.5" r="1.5" fill="#881337"/>""",

        // ── Dolgu ────────────────────────────────────────────────────────────
        "filling-o" =>
            """<rect x="8" y="12" width="20" height="20" fill="#93c5fd" stroke="#1d4ed8" stroke-width="1.5"/>""",

        "filling-m" =>
            """<polygon points="0,0 8,12 8,32 0,44" fill="#93c5fd" stroke="#1d4ed8" stroke-width="1.5"/>""",

        "filling-d" =>
            """<polygon points="28,12 36,0 36,44 28,32" fill="#93c5fd" stroke="#1d4ed8" stroke-width="1.5"/>""",

        "filling-v" =>
            """<polygon points="0,0 36,0 28,12 8,12" fill="#93c5fd" stroke="#1d4ed8" stroke-width="1.5"/>""",

        "filling-l" =>
            """<polygon points="8,32 28,32 36,44 0,44" fill="#93c5fd" stroke="#1d4ed8" stroke-width="1.5"/>""",

        "filling-mo" =>
            """<polygon points="0,0 8,12 8,32 0,44" fill="#93c5fd" stroke="#1d4ed8" stroke-width="1.5"/>""" +
            """<rect x="8" y="12" width="20" height="20" fill="#93c5fd" stroke="#1d4ed8" stroke-width="1.5"/>""",

        "filling-do" =>
            """<rect x="8" y="12" width="20" height="20" fill="#93c5fd" stroke="#1d4ed8" stroke-width="1.5"/>""" +
            """<polygon points="28,12 36,0 36,44 28,32" fill="#93c5fd" stroke="#1d4ed8" stroke-width="1.5"/>""",

        "filling-mod" =>
            """<polygon points="0,0 8,12 8,32 0,44" fill="#93c5fd" stroke="#1d4ed8" stroke-width="1.5"/>""" +
            """<rect x="8" y="12" width="20" height="20" fill="#93c5fd" stroke="#1d4ed8" stroke-width="1.5"/>""" +
            """<polygon points="28,12 36,0 36,44 28,32" fill="#93c5fd" stroke="#1d4ed8" stroke-width="1.5"/>""",

        "decay" =>
            """<circle cx="14" cy="18" r="4" fill="#a16207" opacity="0.7"/>""" +
            """<circle cx="22" cy="23" r="3" fill="#a16207" opacity="0.6"/>""" +
            """<circle cx="17" cy="28" r="2.5" fill="#a16207" opacity="0.5"/>""",

        // ── Periodontoloji ───────────────────────────────────────────────────
        "perio" =>
            """<path d="M4,14 Q9,8 14,14 Q19,8 24,14 Q29,8 34,14" fill="none" stroke="#16a34a" stroke-width="2" stroke-linecap="round"/>""" +
            """<line x1="6" y1="14" x2="6" y2="32" stroke="#16a34a" stroke-width="1.2" stroke-dasharray="2,2"/>""" +
            """<line x1="30" y1="14" x2="30" y2="32" stroke="#16a34a" stroke-width="1.2" stroke-dasharray="2,2"/>""",

        "perio-adv" =>
            """<path d="M4,20 Q9,12 14,20 Q19,12 24,20 Q29,12 34,20" fill="none" stroke="#15803d" stroke-width="2.2" stroke-linecap="round"/>""" +
            """<line x1="6" y1="20" x2="6" y2="40" stroke="#15803d" stroke-width="1.5" stroke-dasharray="2,2"/>""" +
            """<line x1="30" y1="20" x2="30" y2="40" stroke="#15803d" stroke-width="1.5" stroke-dasharray="2,2"/>""",

        // ── Ortodonti ────────────────────────────────────────────────────────
        "braket" =>
            """<rect x="11" y="16" width="14" height="12" rx="1" fill="#bae6fd" stroke="#0369a1" stroke-width="1.8"/>""" +
            """<line x1="4" y1="22" x2="11" y2="22" stroke="#0369a1" stroke-width="1.5"/>""" +
            """<line x1="25" y1="22" x2="32" y2="22" stroke="#0369a1" stroke-width="1.5"/>""",

        "bant" =>
            """<rect x="2" y="12" width="32" height="20" rx="2" fill="none" stroke="#075985" stroke-width="2"/>""" +
            """<line x1="2" y1="18" x2="34" y2="18" stroke="#075985" stroke-width="1"/>""" +
            """<line x1="2" y1="26" x2="34" y2="26" stroke="#075985" stroke-width="1"/>""",

        // ── Genel ────────────────────────────────────────────────────────────
        "missing" =>
            """<rect x="2" y="2" width="32" height="40" rx="4" fill="#f9fafb" stroke="#9ca3af" stroke-width="1.8" stroke-dasharray="5,3"/>""" +
            """<circle cx="18" cy="22" r="3.5" fill="#d1d5db"/>""",

        "impacted" =>
            """<path d="M18,6 L18,30 M10,22 L18,32 L26,22" fill="none" stroke="#15803d" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>""",

        _ => null
    };

    // ── Tedavi Tablosu ─────────────────────────────────────────────────────

    static void TreatmentTable(IContainer container, List<PdfLineItem> lines, Dictionary<string, decimal> fxRates, string? targetCurrency)
    {
        // TL karşılığı sütunu: herhangi bir yabancı para birimi varsa göster
        var allTry     = lines.All(l => l.Currency == "TRY");
        var showTryCol = !allTry;
        // Liste Fiyatı sütunu: en az bir kalemde gerçek fark varsa göster
        var showListCol = lines.Any(l => l.ListPrice > l.FinalPrice);

        container.Table(table =>
        {
            // Diş | İşlem | [Liste Fiyatı] | [İndirim%] | Tedavi Fiyatı [| TL Karşılığı]
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(22);   // Diş
                cols.RelativeColumn(4);    // İşlem
                if (showListCol) cols.RelativeColumn(2); // Liste Fiyatı
                if (showListCol) cols.ConstantColumn(46); // İndirim%
                cols.RelativeColumn(2);    // Tedavi Fiyatı
                if (showTryCol) cols.RelativeColumn(2);  // TL Karşılığı
            });

            static IContainer HdrCell(IContainer c) =>
                c.Background("#1e40af").Padding(4).AlignCenter();

            table.Header(h =>
            {
                h.Cell().Element(HdrCell).Text(t => t.Span("Diş").FontSize(7.5f).Bold().FontColor("#ffffff"));
                h.Cell().Element(HdrCell).Text(t => t.Span("İşlem").FontSize(7.5f).Bold().FontColor("#ffffff"));
                if (showListCol)
                {
                    h.Cell().Element(HdrCell).Text(t => t.Span("Liste Fiyatı").FontSize(7.5f).Bold().FontColor("#ffffff"));
                    h.Cell().Element(HdrCell).Text(t => t.Span("İndirim %").FontSize(7.5f).Bold().FontColor("#ffffff"));
                }
                h.Cell().Element(HdrCell).Text(t => t.Span("Tedavi Fiyatı").FontSize(7.5f).Bold().FontColor("#ffffff"));
                if (showTryCol)
                    h.Cell().Element(HdrCell).Text(t => t.Span("TL Karşılığı").FontSize(7.5f).Bold().FontColor("#ffffff"));
            });

            bool even = false;
            foreach (var line in lines)
            {
                even = !even;
                var bg = even ? "#ffffff" : "#f9fafb";
                IContainer DC(IContainer c) => c.Background(bg).BorderBottom(1).BorderColor("#f3f4f6").Padding(4);

                table.Cell().Element(DC).AlignCenter().Text(line.ToothNumber ?? "—").FontSize(7.5f);
                table.Cell().Element(DC).Text(line.TreatmentName).FontSize(7.5f);
                if (showListCol)
                {
                    var hasDiscount = line.ListPrice > line.FinalPrice;
                    // Gerçek fark yoksa (eski kalem, listPrice kaydedilmemiş) — göster
                    table.Cell().Element(DC).AlignRight().Text(t =>
                    {
                        if (hasDiscount)
                            t.Span(Fmt(line.ListPrice, line.Currency))
                             .FontFamily("Courier New").FontSize(7.5f)
                             .Strikethrough().FontColor("#9ca3af");
                        else
                            t.Span("—").FontSize(7.5f).FontColor("#9ca3af");
                    });
                    table.Cell().Element(DC).AlignCenter()
                        .Text(hasDiscount && line.DiscountRate > 0 ? $"%{line.DiscountRate:0}" : "—")
                        .FontSize(7.5f).FontColor(hasDiscount && line.DiscountRate > 0 ? "#111827" : "#9ca3af");
                }
                table.Cell().Element(DC).AlignRight().Text(Fmt(line.FinalPrice, line.Currency)).FontFamily("Courier New").FontSize(7.5f);
                if (showTryCol)
                    table.Cell().Element(DC).AlignRight()
                        .Text(line.Currency == "TRY" ? "—" : Fmt(line.TryEquivalent, "TRY"))
                        .FontFamily("Courier New").FontSize(7.5f)
                        .FontColor(line.Currency == "TRY" ? "#9ca3af" : "#111827");
            }

            // ── Para birimi bazında ara toplamlar ──────────────────────────
            var groups = lines
                .GroupBy(l => l.Currency)
                .OrderBy(g => g.Key == "TRY" ? 1 : 0)
                .ToList();

            foreach (var group in groups)
            {
                var cur        = group.Key;
                // Sadece gerçek indirimli kalemler liste toplamına dahil, diğerleri finalPrice ile
                var listTotal  = group.Sum(l => l.ListPrice > l.FinalPrice ? l.ListPrice : l.FinalPrice);
                var finalTotal = group.Sum(l => l.FinalPrice);
                var tryTotal   = group.Sum(l => l.TryEquivalent);
                var savedTotal = listTotal - finalTotal;
                var avgPct     = listTotal > 0 && savedTotal > 0 ? Math.Round(savedTotal / listTotal * 100) : 0;
                var isForeign  = cur != "TRY";

                IContainer TC(IContainer c) => c.Background("#e0e7ff").Padding(4);

                table.Cell().ColumnSpan(2u).Element(TC).AlignRight()
                    .Text($"Toplam ({cur})").FontSize(8).Bold().FontColor("#1e3a8a");
                if (showListCol)
                {
                    table.Cell().Element(TC).AlignRight().Text(t =>
                    {
                        if (savedTotal > 0)
                            t.Span(Fmt(listTotal, cur)).FontFamily("Courier New").FontSize(8).Bold()
                             .Strikethrough().FontColor("#6b7280");
                        else
                            t.Span("—").FontSize(8).FontColor("#9ca3af");
                    });
                    table.Cell().Element(TC).AlignCenter()
                        .Text(avgPct > 0 ? $"%{avgPct}" : "—").FontSize(8).Bold()
                        .FontColor(avgPct > 0 ? "#1e3a8a" : "#9ca3af");
                }
                table.Cell().Element(TC).AlignRight()
                    .Text(Fmt(finalTotal, cur)).FontFamily("Courier New").FontSize(8).Bold().FontColor("#1e3a8a");
                if (showTryCol)
                    table.Cell().Element(TC).AlignRight()
                        .Text(isForeign ? Fmt(tryTotal, "TRY") : "—")
                        .FontFamily("Courier New").FontSize(8).Bold()
                        .FontColor(isForeign ? "#1e3a8a" : "#9ca3af");
            }

            // ── Genel TRY toplamı (birden fazla para birimi varsa) ─────────
            if (groups.Count > 1 || (groups.Count == 1 && groups[0].Key != "TRY"))
            {
                var grandTryTotal = lines.Sum(l => l.TryEquivalent);
                var grandList     = lines.Sum(l => l.TryEquivalent > 0
                    ? ToTry(l.ListPrice, l.Currency, fxRates) : l.ListPrice);
                var grandPct      = grandList > 0
                    ? Math.Round((1 - grandTryTotal / grandList) * 100) : 0;

                uint colCount = (showListCol ? 2u : 0u) + (showTryCol ? 1u : 0u) + 3u; // Diş+İşlem+Tedavi + opsiyoneller
                IContainer GC(IContainer c) => c.Background("#1e40af").Padding(4);

                table.Cell().ColumnSpan(colCount - 1u).Element(GC).AlignRight()
                    .Text("GENEL TOPLAM (TL)").FontSize(8.5f).Bold().FontColor("#ffffff");
                table.Cell().Element(GC).AlignRight()
                    .Text(Fmt(grandTryTotal, "TRY")).FontFamily("Courier New").FontSize(8.5f).Bold().FontColor("#ffffff");
            }
        });
    }

    // ── Fiyat özeti kutusu ─────────────────────────────────────────────────

    static void PriceSummaryBox(IContainer container, List<PdfLineItem> lines, Dictionary<string, decimal> fxRates)
    {
        var groups = lines
            .Where(l => l.ListPrice > l.FinalPrice)
            .GroupBy(l => l.Currency)
            .ToList();

        // RelativeItem inside AutoItem → DocumentLayoutException.
        // Çözüm: sabit genişlik (Width) + iç satırlar için Table kullan.
        container.AlignRight()
            .Width(260)
            .Border(1).BorderColor("#1e40af")
            .Background("#f0f4ff").Padding(10)
            .Column(col =>
            {
                col.Spacing(5);
                col.Item().Text("Fiyat Özeti").FontSize(8.5f).Bold().FontColor("#1e3a8a");

                foreach (var g in groups)
                {
                    var cur        = g.Key;
                    var listTotal  = g.Sum(l => l.ListPrice);
                    var finalTotal = g.Sum(l => l.FinalPrice);
                    var saved      = listTotal - finalTotal;

                    // 3:2 kolonlu tablo — sol etiket, sağ rakam
                    col.Item().Table(tbl =>
                    {
                        tbl.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); });

                        tbl.Cell().Text($"Liste fiyatı ({cur})").FontSize(7.5f).FontColor("#6b7280");
                        tbl.Cell().AlignRight().Text(t =>
                            t.Span(Fmt(listTotal, cur))
                             .FontFamily("Courier New").FontSize(7.5f)
                             .Strikethrough().FontColor("#9ca3af"));

                        tbl.Cell().Text($"Tasarruf ({cur})").FontSize(7.5f).FontColor("#059669");
                        tbl.Cell().AlignRight()
                            .Text($"−{Fmt(saved, cur)}")
                            .FontFamily("Courier New").FontSize(7.5f).FontColor("#059669");

                        tbl.Cell().PaddingTop(3).BorderTop(1).BorderColor("#c7d2fe")
                            .Text($"Planın tutarı ({cur})").FontSize(8).Bold().FontColor("#1e3a8a");
                        tbl.Cell().PaddingTop(3).BorderTop(1).BorderColor("#c7d2fe").AlignRight()
                            .Text(Fmt(finalTotal, cur))
                            .FontFamily("Courier New").FontSize(8).Bold().FontColor("#1e3a8a");
                    });
                }

                // Birden fazla para birimi varsa TRY genel toplamı
                if (groups.Count > 1 || (groups.Count == 1 && groups[0].Key != "TRY"))
                {
                    var grandFinal = lines.Sum(l => l.TryEquivalent);
                    col.Item().Background("#1e40af").Padding(4).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); });
                        tbl.Cell().Text("Genel Toplam (TL)").FontSize(8).Bold().FontColor("#ffffff");
                        tbl.Cell().AlignRight()
                            .Text(Fmt(grandFinal, "TRY"))
                            .FontFamily("Courier New").FontSize(8).Bold().FontColor("#ffffff");
                    });
                }
            });
    }

    // ── Footer ─────────────────────────────────────────────────────────────

    static void Footer(IContainer container)
    {
        container.BorderTop(1).BorderColor("#e5e7eb").PaddingTop(6)
            .Text("Yukarıdaki tabloda belirtilen tedavi planlaması ve/veya ücretlendirmeler bilgi niteliğindedir. " +
                  "Tedavinizi yaptırmaya karar verdiğinizde, güncel tedavi ve ücretler geçerli olacaktır.")
            .FontSize(7).FontColor("#6b7280").Italic();
    }

    async Task<Dictionary<string, decimal>> FetchFxRatesAsync(CancellationToken ct)
    {
        // Hedef: { "USD": 44.87, "EUR": 52.79, "GBP": 60.73, "CHF": 50.12 }
        // Frankfurter: 1 TRY = X yabancı → tersine çevir → 1 yabancı = Y TRY
        try
        {
            var client = _httpClientFactory.CreateClient();
            var resp = await client.GetFromJsonAsync<FrankfurterResponse>(
                "https://api.frankfurter.app/latest?from=TRY&to=USD,EUR,GBP,CHF", ct);

            return resp?.Rates?.ToDictionary(
                kv => kv.Key,
                kv => kv.Value > 0 ? Math.Round(1m / kv.Value, 4) : 0m
            ) ?? FallbackRates();
        }
        catch
        {
            return FallbackRates();
        }
    }

    static Dictionary<string, decimal> FallbackRates() => new()
    {
        ["USD"] = 0, ["EUR"] = 0, ["GBP"] = 0, ["CHF"] = 0,
    };

    record FrankfurterResponse(string Base, string Date, Dictionary<string, decimal> Rates);
}
