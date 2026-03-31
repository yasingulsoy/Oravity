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
[Route("api/patients/{patientId:long}/dental-chart")]
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
    public async Task<IActionResult> GetDentalChart(long patientId)
    {
        var result = await _mediator.Send(new GetPatientDentalChartQuery(patientId));
        return Ok(result);
    }

    // ── Diş Durumu Güncelleme ──────────────────────────────────────────────

    /// <summary>
    /// Belirli bir dişin durumunu günceller veya ilk kaydını oluşturur.
    /// Her güncelleme ToothConditionHistory'e yazılır.
    /// </summary>
    [HttpPut("teeth/{toothNumber}")]
    [RequirePermission("patient:edit_basic")]
    [ProducesResponseType(typeof(ToothRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateToothStatus(
        long patientId,
        string toothNumber,
        [FromBody] UpdateToothStatusRequest request)
    {
        var result = await _mediator.Send(new UpdateToothStatusCommand(
            patientId,
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
    [RequirePermission("patient:edit_basic")]
    [ProducesResponseType(typeof(IReadOnlyList<ToothRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BulkUpdateTeeth(
        long patientId,
        [FromBody] BulkUpdateTeethRequest request)
    {
        var items = request.Teeth
            .Select(t => new ToothUpdateItem(t.ToothNumber, t.Status, t.Surfaces, t.Notes))
            .ToList();

        var result = await _mediator.Send(new BulkUpdateTeethCommand(
            patientId, items, request.Reason));

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
    public async Task<IActionResult> GetToothHistory(long patientId, string toothNumber)
    {
        var result = await _mediator.Send(
            new GetToothHistoryQuery(patientId, toothNumber));
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
