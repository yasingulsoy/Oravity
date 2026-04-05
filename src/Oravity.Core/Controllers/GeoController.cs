using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;

namespace Oravity.Core.Controllers;

[ApiController]
[Route("api/geo")]
[Authorize]
[Produces("application/json")]
public class GeoController : ControllerBase
{
    private readonly AppDbContext _db;

    public GeoController(AppDbContext db) => _db = db;

    /// <summary>Tüm ülkeler</summary>
    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries(CancellationToken ct)
    {
        var items = await _db.Countries
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.IsoCode })
            .ToListAsync(ct);
        return Ok(items);
    }

    /// <summary>Ülkeye göre iller (TR için 81 il)</summary>
    [HttpGet("cities")]
    public async Task<IActionResult> GetCities([FromQuery] long countryId, CancellationToken ct)
    {
        var items = await _db.Cities
            .AsNoTracking()
            .Where(x => x.CountryId == countryId && x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(ct);
        return Ok(items);
    }

    /// <summary>İle göre ilçeler</summary>
    [HttpGet("districts")]
    public async Task<IActionResult> GetDistricts([FromQuery] long cityId, CancellationToken ct)
    {
        var items = await _db.Districts
            .AsNoTracking()
            .Where(x => x.CityId == cityId && x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(ct);
        return Ok(items);
    }

    /// <summary>Uyruk listesi</summary>
    [HttpGet("nationalities")]
    public async Task<IActionResult> GetNationalities(CancellationToken ct)
    {
        var items = await _db.Nationalities
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Code })
            .ToListAsync(ct);
        return Ok(items);
    }
}
