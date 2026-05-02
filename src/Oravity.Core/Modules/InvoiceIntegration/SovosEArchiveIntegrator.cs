using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Oravity.Core.Modules.InvoiceIntegration;

/// <summary>
/// Sovos Bulut e-Arşiv entegratörü.
/// SOAP over HTTP, Basic Auth (username:password Base64).
/// Docs: savos/Sovos Bulut e-Arşiv Fatura WS API v2.3/
/// </summary>
public class SovosEArchiveIntegrator : IInvoiceIntegrator
{
    // ── Sabit namespace'ler ───────────────────────────────────────────────
    private const string NsEnv  = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string NsInv  = "http://fitcons.com/earchive/invoice";

    // ── Sovos status kodları ──────────────────────────────────────────────
    // 10  = Başarılı / İmzalandı
    // 130 = Invoice is signed successfully
    // 4xx = Hata

    private readonly HttpClient _http;
    private readonly ILogger<SovosEArchiveIntegrator> _logger;
    private readonly string _endpoint;
    private readonly string _companyVkn;

    public SovosEArchiveIntegrator(
        HttpClient http,
        ILogger<SovosEArchiveIntegrator> logger,
        string endpoint,
        string companyVkn,
        string username,
        string password)
    {
        _http     = http;
        _logger   = logger;
        _endpoint = endpoint;
        _companyVkn = companyVkn;

        // HTTP Basic Auth
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
    }

    // ── 1. Fatura numarası üret ───────────────────────────────────────────

    public async Task<InvoiceNumberResult> GenerateInvoiceNumberAsync(
        GenerateInvoiceNumberRequest request, CancellationToken ct = default)
    {
        var custInvId = $"{request.BranchId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        var soapBody = $"""
            <inv:invIdGenerationRequest>
              <Identifier>{_companyVkn}</Identifier>
              <Branch>default</Branch>
              <Cust_inv_id>{custInvId}</Cust_inv_id>
              <Issue_date>{request.IssueDate:yyyy-MM-dd}</Issue_date>
            </inv:invIdGenerationRequest>
            """;

        var response = await SendSoapAsync("invIdGenerationRequest", soapBody, ct);
        var body     = GetBody(response);

        var invoiceId = body.Descendants("Invoice_ID").FirstOrDefault()?.Value
            ?? throw new InvalidOperationException("Sovos: Invoice_ID eksik response'da.");
        var uuid      = body.Descendants("UUID").FirstOrDefault()?.Value;

        _logger.LogInformation("Sovos e-Arşiv fatura no üretildi: {No} (UUID: {UUID})", invoiceId, uuid);
        return new InvoiceNumberResult(invoiceId, uuid);
    }

    // ── 2. Fatura gönder (UBL-TR XML) ────────────────────────────────────

    public Task<SendInvoiceResult> SendInvoiceAsync(
        SendInvoiceRequest request, CancellationToken ct = default)
    {
        // TODO Faz 2: UBL-TR XML üretimi implement edilecek.
        // Gereksinimler:
        //   - UblTrBuilder.BuildEArchiveXml() → XML byte[]
        //   - ZIP sıkıştır
        //   - MD5 hash hesapla
        //   - Base64'e çevir
        //   - sendInvoiceRequestType SOAP call
        //
        // Referans: savos/Sovos Bulut e-Arşiv Fatura WS API v2.3/
        //           EK1 - Teknik Belgeler/e-Arşiv Fatura WS Soap istek ve Yanıt Örnekleri/sendInvoiceRequest.xml

        throw new NotImplementedException(
            "Sovos e-Arşiv fatura gönderimi Faz 2'de implement edilecek. " +
            "UBL-TR XML üretimi (UblTrBuilder) tamamlanmalı.");
    }

    // ── 3. Durum sorgula ─────────────────────────────────────────────────

    public async Task<InvoiceStatusResult> GetInvoiceStatusAsync(
        string invoiceNo, string? externalUuid, CancellationToken ct = default)
    {
        var soapBody = $"""
            <inv:getStatusRequestType>
              <UUID>{externalUuid ?? ""}</UUID>
              <vkn>{_companyVkn}</vkn>
              <invoiceNumber>{invoiceNo}</invoiceNumber>
            </inv:getStatusRequestType>
            """;

        var response = await SendSoapAsync("getStatus", soapBody, ct);
        var body     = GetBody(response);

        var statusCode = body.Descendants("statusCode").FirstOrDefault()?.Value ?? "0";
        var detail     = body.Descendants("Detail").FirstOrDefault()?.Value;

        // Sovos: 10 veya 130 = başarılı
        var isAccepted = statusCode is "10" or "130";
        var isError    = int.TryParse(statusCode, out var code) && code >= 400;

        return new InvoiceStatusResult(
            InvoiceNo:      invoiceNo,
            ExternalStatus: $"{statusCode}: {detail}",
            IsAccepted:     isAccepted,
            IsError:        isError,
            ErrorMessage:   isError ? detail : null);
    }

    // ── SOAP yardımcılar ──────────────────────────────────────────────────

    private async Task<XDocument> SendSoapAsync(string action, string body, CancellationToken ct)
    {
        var envelope = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soapenv:Envelope
                xmlns:soapenv="{NsEnv}"
                xmlns:inv="{NsInv}">
              <soapenv:Header/>
              <soapenv:Body>
                {body}
              </soapenv:Body>
            </soapenv:Envelope>
            """;

        using var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", $"\"{action}\"");

        _logger.LogDebug("Sovos SOAP → {Action}", action);

        var httpResponse = await _http.PostAsync(_endpoint, content, ct);
        var responseText = await httpResponse.Content.ReadAsStringAsync(ct);

        if (!httpResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Sovos HTTP {Status}: {Body}", httpResponse.StatusCode, responseText);
            throw new InvalidOperationException($"Sovos HTTP {(int)httpResponse.StatusCode}: {responseText}");
        }

        try
        {
            return XDocument.Parse(responseText);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Sovos SOAP parse hatası: {responseText}", ex);
        }
    }

    private static XElement GetBody(XDocument doc)
    {
        var body = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "Body")
            ?? throw new InvalidOperationException("Sovos: SOAP Body bulunamadı.");

        var fault = body.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
        if (fault != null)
        {
            var faultString = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value;
            throw new InvalidOperationException($"Sovos SOAP Fault: {faultString}");
        }

        return body;
    }
}
