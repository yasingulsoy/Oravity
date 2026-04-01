using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Appointment.OnlineBooking.Application.Commands;
using Oravity.Core.Modules.Appointment.OnlineBooking.Application.Queries;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Controllers;

/// <summary>
/// Resepsiyon paneli — online randevu taleplerini yönet (SPEC §ONLİNE RANDEVU SİSTEMİ §7).
/// Tüm endpoint'ler JWT gerektirir.
/// </summary>
[ApiController]
[Route("api/booking-requests")]
[Authorize]
[Tags("Booking Requests")]
public class BookingRequestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _user;
    private readonly ITenantContext _tenant;

    public BookingRequestsController(
        IMediator mediator, ICurrentUser user, ITenantContext tenant)
    {
        _mediator = mediator;
        _user     = user;
        _tenant   = tenant;
    }

    /// <summary>
    /// Bekleyen online randevu taleplerini listele — resepsiyon paneli için.
    /// </summary>
    [HttpGet]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var branchId = _tenant.BranchId
            ?? throw new UnauthorizedAccessException("Şube bağlamı bulunamadı.");

        var result = await _mediator.Send(
            new GetPendingBookingRequestsQuery(branchId, page, pageSize), ct);

        return Ok(result);
    }

    /// <summary>
    /// Online randevu talebini onayla — Appointment oluşturur, hastaya SMS gönderir.
    /// </summary>
    [HttpPut("{id:guid}/approve")]
    [RequirePermission("appointment:create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ApproveOnlineBookingCommand(id), ct);
        return Ok(result);
    }

    /// <summary>
    /// Online randevu talebini reddet — hastaya SMS gönderir.
    /// </summary>
    [HttpPut("{id:guid}/reject")]
    [RequirePermission("appointment:cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RejectBookingBody body,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new RejectOnlineBookingCommand(id, body.Reason), ct);
        return Ok(result);
    }
}

public record RejectBookingBody(string? Reason = null);
