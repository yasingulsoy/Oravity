using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Reporting.Application;
using Oravity.Core.Modules.Reporting.Application.Queries;

namespace Oravity.Core.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
[Tags("Raporlama & Dashboard")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Bugünkü dashboard KPI özeti.
    /// Redis ile 5 dk cache. Şube ve kullanıcı bazında invalidate edilir.
    /// </summary>
    [HttpGet("dashboard")]
    [RequirePermission("report:view")]
    [ProducesResponseType(typeof(DashboardSummary), 200)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
        => Ok(await _mediator.Send(new DashboardSummaryQuery(), ct));

    /// <summary>
    /// Günlük gelir raporu. Ödeme yöntemi ve hekim bazında gruplama.
    /// </summary>
    [HttpGet("daily-revenue")]
    [RequirePermission("report:view_daily")]
    [ProducesResponseType(typeof(DailyRevenueReport), 200)]
    public async Task<IActionResult> GetDailyRevenue(
        [FromQuery] DateTime  startDate,
        [FromQuery] DateTime  endDate,
        [FromQuery] long?     branchId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new DailyRevenueReportQuery(startDate, endDate, branchId), ct));

    /// <summary>
    /// Hekim performans raporu: randevu, tedavi, ciro, hakediş.
    /// </summary>
    [HttpGet("doctor-performance")]
    [RequirePermission("commission:view")]
    [ProducesResponseType(typeof(DoctorPerformanceReport), 200)]
    public async Task<IActionResult> GetDoctorPerformance(
        [FromQuery] DateTime  startDate,
        [FromQuery] DateTime  endDate,
        [FromQuery] long?     doctorId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new DoctorPerformanceReportQuery(startDate, endDate, doctorId), ct));

    /// <summary>
    /// Randevu istatistikleri: status dağılımı, gelmedi oranı, ortalama süre.
    /// </summary>
    [HttpGet("appointments")]
    [RequirePermission("report:view")]
    [ProducesResponseType(typeof(AppointmentStatsReport), 200)]
    public async Task<IActionResult> GetAppointmentStats(
        [FromQuery] DateTime  startDate,
        [FromQuery] DateTime  endDate,
        [FromQuery] long?     branchId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new AppointmentStatsQuery(startDate, endDate, branchId), ct));

    /// <summary>
    /// Hasta istatistikleri: yeni hasta, toplam aktif, en çok tedavi görenler.
    /// </summary>
    [HttpGet("patients")]
    [RequirePermission("report:view")]
    [ProducesResponseType(typeof(PatientStatsReport), 200)]
    public async Task<IActionResult> GetPatientStats(
        [FromQuery] DateTime  startDate,
        [FromQuery] DateTime  endDate,
        [FromQuery] int       topCount = 10,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new PatientStatsQuery(startDate, endDate, topCount), ct));
}
