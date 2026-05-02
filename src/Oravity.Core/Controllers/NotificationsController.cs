using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Modules.Notification.Application;
using Oravity.Core.Modules.Notification.Application.Commands;
using Oravity.Core.Modules.Notification.Application.Queries;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Controllers;

/// <summary>
/// Klinik içi bildirim ve SMS kuyruğu yönetimi.
/// SignalR bağlantısı: /hubs/notifications
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ITenantContext _tenant;

    public NotificationsController(IMediator mediator, ITenantContext tenant)
    {
        _mediator = mediator;
        _tenant   = tenant;
    }

    // ── Bildirimler ────────────────────────────────────────────────────────

    /// <summary>
    /// Oturum açan kullanıcının bildirimlerini döner.
    /// Okunmamışlar önce sıralanır.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedNotificationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        [FromQuery] bool? unreadOnly = null)
    {
        var result = await _mediator.Send(
            new GetMyNotificationsQuery(page, pageSize, unreadOnly));
        return Ok(result);
    }

    /// <summary>Bildirimi okundu olarak işaretle.</summary>
    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var result = await _mediator.Send(new MarkNotificationReadCommand(id));
        return Ok(result);
    }

    /// <summary>
    /// Hekimin resepsiyona manuel mesaj göndermesi (protokol kapama sonrası vb.).
    /// Şubedeki tüm resepsiyonistlere (ToRole=1) iletilir.
    /// </summary>
    [HttpPost("doctor-message")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendDoctorMessage([FromBody] SendDoctorMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Mesaj boş olamaz." });

        var branchId = _tenant.BranchId
            ?? throw new InvalidOperationException("Şube bilgisi bulunamadı.");

        var result = await _mediator.Send(new SendInAppNotificationCommand(
            BranchId:          branchId,
            Type:              NotificationType.DoctorMessage,
            Title:             request.Title ?? "Hekim Mesajı",
            Message:           request.Message.Trim(),
            ToUserId:          null,
            ToRole:            1,   // Reception
            IsUrgent:          request.IsUrgent,
            RelatedEntityType: request.RelatedEntityType,
            RelatedEntityId:   request.RelatedEntityId));

        return StatusCode(StatusCodes.Status201Created, result);
    }

    // ── SMS ────────────────────────────────────────────────────────────────

    /// <summary>
    /// SMS kuyruğuna ekler. SmsDispatchService (Hangfire) her dakika işler.
    /// </summary>
    [HttpPost("sms/queue")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> QueueSms([FromBody] QueueSmsRequest request)
    {
        var id = await _mediator.Send(new QueueSmsCommand(
            request.ToPhone,
            request.Message,
            request.SourceType,
            request.ProviderId));

        return Accepted(new { Id = id, Message = "SMS kuyruğa eklendi." });
    }
}

// ─── Request DTO'lar ───────────────────────────────────────────────────────

public record SendDoctorMessageRequest(
    string  Message,
    string? Title             = null,
    bool    IsUrgent          = false,
    string? RelatedEntityType = null,
    long?   RelatedEntityId   = null
);

public record QueueSmsRequest(
    string ToPhone,
    string Message,
    string SourceType,
    int ProviderId = 1
);
