using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Core.DentalChart.Application;
using Oravity.Core.Modules.Core.DentalChart.Application.Commands;
using Oravity.Core.Modules.Core.DentalChart.Application.Queries;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Controllers;

/// <summary>
/// FDI diş şeması yönetimi — 32 diş, durum kaydı ve geçmiş izleme.
/// </summary>
[ApiController]
[Route("api/patients/{patientPublicId:guid}/dental-chart")]
[Authorize]
[Produces("application/json")]
public class DentalChartController : ControllerBase
{
    private readonly IMediator _mediator;

    public DentalChartController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Diş Şeması Görüntüleme ─────────────────────────────────────────────

    /// <summary>
    /// Hastanın 32 dişini FDI haritasıyla döner.
    /// Kayıt olmayan dişler Sağlıklı (default) olarak gelir.
    /// </summary>
    [HttpGet]
    [RequirePermission("patient:view")]
    [ProducesResponseType(typeof(DentalChartResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDentalChart(Guid patientPublicId)
    {
        var result = await _mediator.Send(new GetPatientDentalChartQuery(patientPublicId));
        return Ok(result);
    }

    // ── Diş Durumu Güncelleme ──────────────────────────────────────────────

    /// <summary>
    /// Belirli bir dişin durumunu günceller veya ilk kaydını oluşturur.
    /// Her güncelleme ToothConditionHistory'e yazılır.
    /// </summary>
    [HttpPut("teeth/{toothNumber}")]
    [RequirePermission("protocol:update")]
    [ProducesResponseType(typeof(ToothRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateToothStatus(
        Guid patientPublicId,
        string toothNumber,
        [FromBody] UpdateToothStatusRequest request)
    {
        var result = await _mediator.Send(new UpdateToothStatusCommand(
            patientPublicId,
            toothNumber,
            request.Status,
            request.Surfaces,
            request.Notes,
            request.Reason));

        return Ok(result);
    }

    // ── Toplu Güncelleme (Sesli Komut) ────────────────────────────────────

    /// <summary>
    /// Birden fazla dişi tek seferde günceller.
    /// Sesli komut entegrasyonundan gelen JSON ile çalışır.
    /// </summary>
    [HttpPut("bulk")]
    [RequirePermission("protocol:update")]
    [ProducesResponseType(typeof(IReadOnlyList<ToothRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BulkUpdateTeeth(
        Guid patientPublicId,
        [FromBody] BulkUpdateTeethRequest request)
    {
        var items = request.Teeth
            .Select(t => new ToothUpdateItem(t.ToothNumber, t.Status, t.Surfaces, t.Notes))
            .ToList();

        var result = await _mediator.Send(new BulkUpdateTeethCommand(
            patientPublicId, items, request.Reason));

        return Ok(result);
    }

    // ── Geçmiş ────────────────────────────────────────────────────────────

    /// <summary>
    /// Belirli bir dişin durum değişikliği geçmişini döner (tarih azalan sıra).
    /// </summary>
    [HttpGet("teeth/{toothNumber}/history")]
    [RequirePermission("patient:view")]
    [ProducesResponseType(typeof(IReadOnlyList<ToothHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetToothHistory(Guid patientPublicId, string toothNumber)
    {
        var result = await _mediator.Send(
            new GetToothHistoryQuery(patientPublicId, toothNumber));
        return Ok(result);
    }
}

// ─── Request DTO'lar ───────────────────────────────────────────────────────

public record UpdateToothStatusRequest(
    ToothStatus Status,
    string? Surfaces = null,
    string? Notes = null,
    string? Reason = null
);

public record BulkToothUpdateItem(
    string ToothNumber,
    ToothStatus Status,
    string? Surfaces = null,
    string? Notes = null
);

public record BulkUpdateTeethRequest(
    IReadOnlyList<BulkToothUpdateItem> Teeth,
    string? Reason = null
);
