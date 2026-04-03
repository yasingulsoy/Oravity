using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Oravity.Core.Modules.Finance.EInvoice.Infrastructure.Adapters;

/// <summary>
/// Aşama-1 Adapter: GİB'e göndermez; UBL-TR 2.1 formatında XML üretir.
/// Müşteri kendi entegratörüne bu XML'i yükler (SPEC §ENTEGRATÖR §6 Aşama 1).
/// PdfPath: gerçek PDF oluşturma ilerleyen aşamada eklenecek (placeholder).
/// </summary>
public class XmlExportAdapter : IEInvoiceAdapter
{
    private readonly ILogger<XmlExportAdapter> _logger;

    // GİB UBL-TR namespace'leri
    private static readonly XNamespace UblInvoice = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace Cac        = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc        = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    public XmlExportAdapter(ILogger<XmlExportAdapter> logger) => _logger = logger;

    public Task<EInvoiceResult> SendEArchive(EInvoiceRequest request, CancellationToken ct = default)
        => Task.FromResult(ExportXml(request, "EARSIVFATURA"));

    public Task<EInvoiceResult> SendEInvoice(EInvoiceRequest request, CancellationToken ct = default)
        => Task.FromResult(ExportXml(request, "TICARIFATURA"));

    public Task<EInvoiceResult> SendESMM(EInvoiceRequest request, CancellationToken ct = default)
        => Task.FromResult(ExportXml(request, "EARSIVFATURA")); // SMM de e-arşiv profiliyle gönderilir

    public Task<EInvoiceStatus> QueryStatus(string uuid, CancellationToken ct = default)
    {
        // XML_EXPORT senaryosunda GİB sorgusu yapılmaz
        return Task.FromResult(new EInvoiceStatus(uuid, "LOCAL_EXPORT", null));
    }

    public Task<EInvoiceResult> Cancel(string uuid, string reason, CancellationToken ct = default)
    {
        _logger.LogInformation("XmlExportAdapter: İptal kaydedildi (GİB'e gönderilmedi) — uuid={Uuid}", uuid);
        return Task.FromResult(new EInvoiceResult(true, uuid, "CANCELLED", null, null, null, null));
    }

    public Task<bool> IsGibRegistered(string vkn, CancellationToken ct = default)
    {
        // XML_EXPORT'ta gerçek GİB sorgusu yoktur; varsayılan false
        return Task.FromResult(false);
    }

    // ──────────────────────────────────────────────────────────────────────
    private EInvoiceResult ExportXml(EInvoiceRequest req, string profileId)
    {
        try
        {
            var xml = BuildUblTrXml(req, profileId);

            _logger.LogInformation(
                "XmlExportAdapter: UBL-TR XML oluşturuldu → {EInvoiceNo} ({ProfileId})",
                req.EInvoiceNo, profileId);

            return new EInvoiceResult(
                Success:      true,
                GibUuid:      $"LOCAL-{req.PublicId}",
                Status:       "LOCAL_EXPORT",
                ResponseJson: null,
                ErrorMessage: null,
                XmlContent:   xml,
                PdfPath:      null); // PDF üretimi ilerleyen aşamada
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "XmlExportAdapter: XML oluşturma hatası — {EInvoiceNo}", req.EInvoiceNo);
            return new EInvoiceResult(false, null, "ERROR", null, ex.Message, null, null);
        }
    }

    private string BuildUblTrXml(EInvoiceRequest req, string profileId)
    {
        var issueTime = DateTime.UtcNow;

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(UblInvoice + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", Cac),
                new XAttribute(XNamespace.Xmlns + "cbc", Cbc),

                new XElement(Cbc + "UBLVersionID",       "2.1"),
                new XElement(Cbc + "CustomizationID",    "TR1.2"),
                new XElement(Cbc + "ProfileID",          profileId),
                new XElement(Cbc + "ID",                 req.EInvoiceNo),
                new XElement(Cbc + "IssueDate",          req.InvoiceDate.ToString("yyyy-MM-dd")),
                new XElement(Cbc + "IssueTime",          issueTime.ToString("HH:mm:ss")),
                new XElement(Cbc + "InvoiceTypeCode",    "SATIS"),
                new XElement(Cbc + "Note",               "DisinePlus üzerinden oluşturulmuştur."),
                new XElement(Cbc + "DocumentCurrencyCode", req.Currency),

                // Satıcı (klinik)
                new XElement(Cac + "AccountingSupplierParty",
                    new XElement(Cac + "Party",
                        new XElement(Cac + "PartyIdentification",
                            new XElement(Cbc + "ID",
                                new XAttribute("schemeID", "VKN"),
                                req.SupplierVkn)),
                        new XElement(Cac + "PartyName",
                            new XElement(Cbc + "Name", req.SupplierTitle)),
                        new XElement(Cac + "PostalAddress",
                            new XElement(Cbc + "StreetName", req.SupplierAddress ?? ""),
                            new XElement(Cac + "Country",
                                new XElement(Cbc + "Name", "Türkiye"))),
                        new XElement(Cac + "PartyTaxScheme",
                            new XElement(Cac + "TaxScheme",
                                new XElement(Cbc + "Name", req.SupplierTaxOffice))))),

                // Alıcı (hasta / kurum)
                new XElement(Cac + "AccountingCustomerParty",
                    new XElement(Cac + "Party",
                        new XElement(Cac + "PartyIdentification",
                            new XElement(Cbc + "ID",
                                new XAttribute("schemeID", req.ReceiverVkn is not null ? "VKN" : "TCKN"),
                                req.ReceiverVkn ?? req.ReceiverTc ?? "")),
                        new XElement(Cac + "PartyName",
                            new XElement(Cbc + "Name", req.ReceiverName)),
                        new XElement(Cac + "PostalAddress",
                            new XElement(Cbc + "StreetName", req.ReceiverAddress ?? ""),
                            new XElement(Cac + "Country",
                                new XElement(Cbc + "Name", "Türkiye"))))),

                // KDV
                new XElement(Cac + "TaxTotal",
                    new XElement(Cbc + "TaxAmount",
                        new XAttribute("currencyID", req.Currency),
                        req.TaxAmount.ToString("F2")),
                    new XElement(Cac + "TaxSubtotal",
                        new XElement(Cbc + "TaxableAmount",
                            new XAttribute("currencyID", req.Currency),
                            req.TaxableAmount.ToString("F2")),
                        new XElement(Cbc + "TaxAmount",
                            new XAttribute("currencyID", req.Currency),
                            req.TaxAmount.ToString("F2")),
                        new XElement(Cac + "TaxCategory",
                            new XElement(Cac + "TaxScheme",
                                new XElement(Cbc + "Name", "KDV")),
                            new XElement(Cbc + "Percent", req.TaxRate.ToString("F0"))))),

                // Yasal para toplamı
                new XElement(Cac + "LegalMonetaryTotal",
                    new XElement(Cbc + "LineExtensionAmount",
                        new XAttribute("currencyID", req.Currency),
                        req.Subtotal.ToString("F2")),
                    new XElement(Cbc + "AllowanceTotalAmount",
                        new XAttribute("currencyID", req.Currency),
                        req.DiscountAmount.ToString("F2")),
                    new XElement(Cbc + "TaxExclusiveAmount",
                        new XAttribute("currencyID", req.Currency),
                        req.TaxableAmount.ToString("F2")),
                    new XElement(Cbc + "TaxInclusiveAmount",
                        new XAttribute("currencyID", req.Currency),
                        req.Total.ToString("F2")),
                    new XElement(Cbc + "PayableAmount",
                        new XAttribute("currencyID", req.Currency),
                        req.Total.ToString("F2"))),

                // Kalem satırları
                BuildInvoiceLines(req)
            ));

        var sb = new StringBuilder();
        using var writer = new System.IO.StringWriter(sb);
        doc.Save(writer);
        return sb.ToString();
    }

    private static IEnumerable<XElement> BuildInvoiceLines(EInvoiceRequest req)
    {
        return req.Items.Select((item, index) =>
            new XElement(Cac + "InvoiceLine",
                new XElement(Cbc + "ID", (index + 1).ToString()),
                new XElement(Cbc + "InvoicedQuantity",
                    new XAttribute("unitCode", "C62"),
                    item.Quantity.ToString("F3")),
                new XElement(Cbc + "LineExtensionAmount",
                    new XAttribute("currencyID", req.Currency),
                    (item.UnitPrice * item.Quantity).ToString("F2")),
                new XElement(Cac + "Item",
                    new XElement(Cbc + "Name", item.Description)),
                new XElement(Cac + "Price",
                    new XElement(Cbc + "PriceAmount",
                        new XAttribute("currencyID", req.Currency),
                        item.UnitPrice.ToString("F2")))));
    }
}
