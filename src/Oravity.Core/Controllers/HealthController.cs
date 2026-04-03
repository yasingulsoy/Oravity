using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Oravity.Core.Controllers;

/// <summary>
/// /api/health — uygulama sağlık kontrolü.
/// Outbox dead-letter ve genel DB erişimi kontrol edilir.
/// </summary>
[ApiController]
[Route("api/health")]
[AllowAnonymous]
[Tags("Sağlık")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(HealthReportResponse), 200)]
    [ProducesResponseType(typeof(HealthReportResponse), 503)]
    public async Task<IActionResult> GetHealth(CancellationToken ct = default)
    {
        var report = await _healthCheckService.CheckHealthAsync(ct);

        var response = new HealthReportResponse(
            Status: report.Status.ToString(),
            TotalDurationMs: (int)report.TotalDuration.TotalMilliseconds,
            Checks: report.Entries.Select(e => new HealthCheckEntry(
                Name:        e.Key,
                Status:      e.Value.Status.ToString(),
                Description: e.Value.Description,
                DurationMs:  (int)e.Value.Duration.TotalMilliseconds,
                Data:        e.Value.Data
            )).ToList()
        );

        return report.Status == HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(503, response);
    }

    public record HealthReportResponse(
        string Status,
        int TotalDurationMs,
        IReadOnlyList<HealthCheckEntry> Checks
    );

    public record HealthCheckEntry(
        string Name,
        string Status,
        string? Description,
        int DurationMs,
        IReadOnlyDictionary<string, object> Data
    );
}
