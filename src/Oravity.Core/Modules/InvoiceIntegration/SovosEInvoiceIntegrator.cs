using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Oravity.Core.Modules.InvoiceIntegration;

/// <summary>
/// Sovos Bulut e-Fatura entegratörü (kurumdan kuruma, GIB üzerinden).
/// SOAP over HTTP, Basic Auth.
/// Docs: savos/Sovos Bulut e-Fatura WS API v2.3/
///
/// NOT: e-Fatura sadece IsEInvoiceTaxpayer == true olan kurumlar için kullanılır.
/// Diğerleri için SovosEArchiveIntegrator kullanılmalı.
/// </summary>
public class SovosEInvoiceIntegrator : IInvoiceIntegrator
{
    private const string NsEnv = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string NsEin = "http:/fitcons.com/eInvoice/";

    private readonly HttpClient _http;
    private readonly ILogger<SovosEInvoiceIntegrator> _logger;
    private readonly string _endpoint;
    private readonly string _companyVkn;

    public SovosEInvoiceIntegrator(
        HttpClient http,
        ILogger<SovosEInvoiceIntegrator> logger,
        string endpoint,
        string companyVkn,
        string username,
        string password)
    {
        _http       = http;
        _logger     = logger;
        _endpoint   = endpoint;
        _companyVkn = companyVkn;

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
    }

    // ── 1. Fatura numarası ────────────────────────────────────────────────
    // e-Fatura'da numara entegratörden üretilmez — yerel sayaç kullanılır,
    // e-Fatura gönderimi zarf içinde yapılır (UUID gönderim sırasında oluşur).

    public Task<InvoiceNumberResult> GenerateInvoiceNumberAsync(
        GenerateInvoiceNumberRequest request, CancellationToken ct = default)
    {
        // e-Fatura prefix + local counter — UUID gönderimde oluşacak
        throw new NotImplementedException(
            "e-Fatura için yerel sayaç kullanılır. " +
            "LocalCounterIntegrator delegate edilmeli.");
    }

    // ── 2. Fatura gönder ─────────────────────────────────────────────────

    public Task<SendInvoiceResult> SendInvoiceAsync(
        SendInvoiceRequest request, CancellationToken ct = default)
    {
        // TODO Faz 2: UBL-TR XML üret → sendUBL(INVOICE) çağır
        // Referans: savos/EK1/SOAP - sendUBL(INVOICE).xml
        // Alıcı VKN önce getUserList ile GIB'de kontrol edilmeli
        // Zarf (Envelope) ile gönderilir: sendUBL(ENVELOPE)
        throw new NotImplementedException(
            "Sovos e-Fatura gönderimi Faz 2'de implement edilecek.");
    }

    // ── 3. Durum sorgula ─────────────────────────────────────────────────

    public async Task<InvoiceStatusResult> GetInvoiceStatusAsync(
        string invoiceNo, string? externalUuid, CancellationToken ct = default)
    {
        // getInvResponses (OUTBOUND) ile gönderilen faturaların durumu
        var soapBody = $"""
            <ein:getEnvelopeStatusRequest>
              <ein:VKN_TCKN>{_companyVkn}</ein:VKN_TCKN>
              <ein:EnvelopeIdentifier>{externalUuid ?? invoiceNo}</ein:EnvelopeIdentifier>
            </ein:getEnvelopeStatusRequest>
            """;

        var response = await SendSoapAsync("getEnvelopeStatus", soapBody, ct);
        var body     = GetBody(response);

        var status = body.Descendants("Status").FirstOrDefault()?.Value ?? "UNKNOWN";
        _logger.LogDebug("e-Fatura durum {No}: {Status}", invoiceNo, status);

        return new InvoiceStatusResult(
            InvoiceNo:      invoiceNo,
            ExternalStatus: status,
            IsAccepted:     status == "SUCCESS",
            IsError:        status == "ERROR",
            ErrorMessage:   status == "ERROR" ? "Sovos e-Fatura gönderim hatası." : null);
    }

    private async Task<XDocument> SendSoapAsync(string action, string body, CancellationToken ct)
    {
        var envelope = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soapenv:Envelope
                xmlns:soapenv="{NsEnv}"
                xmlns:ein="{NsEin}">
              <soapenv:Header/>
              <soapenv:Body>
                {body}
              </soapenv:Body>
            </soapenv:Envelope>
            """;

        using var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", $"\"{action}\"");

        var httpResponse = await _http.PostAsync(_endpoint, content, ct);
        var responseText = await httpResponse.Content.ReadAsStringAsync(ct);

        if (!httpResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Sovos e-Fatura HTTP {(int)httpResponse.StatusCode}: {responseText}");

        return XDocument.Parse(responseText);
    }

    private static XElement GetBody(XDocument doc)
    {
        var body = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Body")
            ?? throw new InvalidOperationException("Sovos: SOAP Body bulunamadı.");

        var fault = body.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
        if (fault != null)
        {
            var msg = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value;
            throw new InvalidOperationException($"Sovos SOAP Fault: {msg}");
        }

        return body;
    }
}
