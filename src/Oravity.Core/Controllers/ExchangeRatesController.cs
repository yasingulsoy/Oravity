using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
public class ExchangeRatesController : ControllerBase
{
    private readonly IExchangeRateService _rates;
    private readonly ITenantContext _tenant;

    public ExchangeRatesController(IExchangeRateService rates, ITenantContext tenant)
    {
        _rates = rates;
        _tenant = tenant;
    }

    /// <summary>
    /// Belirtilen para birimi ve tarih için TRY kurunu döner.
    /// Öncelik: şube override → TCMB/ECB tablosu → en son bilinen kur.
    /// </summary>
    [HttpGet("api/exchange-rates/current")]
    [ProducesResponseType(typeof(ExchangeRateResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrent(
        [FromQuery] string currency,
        [FromQuery] DateOnly? date = null,
        CancellationToken ct = default)
    {
        var resolvedDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var rate = await _rates.GetRate(
            currency, resolvedDate,
            _tenant.CompanyId, _tenant.BranchId, ct);

        return Ok(new ExchangeRateResponse(currency.ToUpperInvariant(), "TRY", rate, resolvedDate));
    }
}

public record ExchangeRateResponse(
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateOnly RateDate
);
