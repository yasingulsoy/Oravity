using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Core.Modules.Appointment.Application.Commands;
using Oravity.Core.Modules.Appointment.Application.Queries;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Controllers;

/// <summary>
/// Randevu yönetimi endpoint'leri.
/// Real-time güncelleme: her değişiklik SignalR CalendarHub üzerinden yayınlanır.
/// </summary>
[ApiController]
[Route("api/appointments")]
[Authorize]
[Produces("application/json")]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AppointmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Belirli tarihteki randevuları listeler.
    /// İsteğe bağlı: branchId ve doctorId filtresi.
    /// </summary>
    [HttpGet]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByDate(
        [FromQuery] DateOnly date,
        [FromQuery] long? branchId = null,
        [FromQuery] long? doctorId = null)
    {
        var result = await _mediator.Send(new GetAppointmentsByDateQuery(date, branchId, doctorId));
        return Ok(result);
    }

    /// <summary>
    /// Doktorun belirli bir gün için müsait slotlarını döner.
    /// </summary>
    [HttpGet("availability")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(IReadOnlyList<TimeSlotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] long doctorId,
        [FromQuery] DateOnly date,
        [FromQuery] int slotMinutes = 30)
    {
        var result = await _mediator.Send(
            new GetDoctorAvailabilityQuery(doctorId, date, slotMinutes));
        return Ok(result);
    }

    /// <summary>
    /// Yeni randevu oluşturur.
    /// Slot çakışması durumunda 409 döner.
    /// Başarıda SignalR üzerinden CalendarUpdated(Created) yayınlanır.
    /// </summary>
    [HttpPost]
    [RequirePermission("appointment:create")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request)
    {
        var result = await _mediator.Send(new CreateAppointmentCommand(
            request.PatientId,
            request.DoctorId,
            request.StartTime,
            request.EndTime,
            request.Notes));

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Randevu durumunu günceller.
    /// Geçerli geçişler: Planlandı→Onaylandı→Geldi→OdayaAlındı→Tamamlandı | Her an→İptal/Gelmedi
    /// </summary>
    [HttpPut("{publicId:guid}/status")]
    [RequirePermission("appointment:edit")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid publicId,
        [FromBody] UpdateStatusRequest request)
    {
        var result = await _mediator.Send(
            new UpdateAppointmentStatusCommand(publicId, request.Status));
        return Ok(result);
    }

    /// <summary>
    /// Randevuyu yeni zaman dilimine / hekime taşır.
    /// rowVersion optimistic lock ile çakışma koruması sağlar.
    /// </summary>
    [HttpPut("{publicId:guid}/move")]
    [RequirePermission("appointment:edit")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Move(
        Guid publicId,
        [FromBody] MoveAppointmentRequest request)
    {
        var result = await _mediator.Send(new MoveAppointmentCommand(
            publicId,
            request.NewStartTime,
            request.NewEndTime,
            request.NewDoctorId,
            request.ExpectedRowVersion));
        return Ok(result);
    }

    /// <summary>Randevuyu iptal eder (soft cancel — status=6).</summary>
    [HttpDelete("{publicId:guid}")]
    [RequirePermission("appointment:cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        Guid publicId,
        [FromQuery] string? reason = null)
    {
        await _mediator.Send(new CancelAppointmentCommand(publicId, reason));
        return NoContent();
    }
}

// ─── Request DTO'lar ───────────────────────────────────────────────────────

public record CreateAppointmentRequest(
    long PatientId,
    long DoctorId,
    DateTime StartTime,
    DateTime EndTime,
    string? Notes
);

public record UpdateStatusRequest(AppointmentStatus Status);

public record MoveAppointmentRequest(
    DateTime NewStartTime,
    DateTime NewEndTime,
    long? NewDoctorId,
    int ExpectedRowVersion
);
