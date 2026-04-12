using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Core.Modules.Visit.Application.Commands;
using Oravity.Core.Modules.Visit.Application.Queries;
using Oravity.Infrastructure.Database;

namespace Oravity.Core.Controllers;

[ApiController]
[Route("api/protocols")]
[Authorize]
[Produces("application/json")]
public class ProtocolsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _db;

    public ProtocolsController(IMediator mediator, AppDbContext db)
    {
        _mediator = mediator;
        _db       = db;
    }

    /// <summary>Aktif protokol tiplerini döner.</summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(IReadOnlyList<ProtocolTypeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTypes(CancellationToken ct)
    {
        var items = await _db.ProtocolTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .Select(t => new ProtocolTypeResponse(t.Id, t.Name, t.Code, t.Color, t.Description))
            .ToListAsync(ct);
        return Ok(items);
    }

    /// <summary>Vizite için yeni protokol oluşturur.</summary>
    [HttpPost]
    [RequirePermission("protocol:create")]
    [ProducesResponseType(typeof(ProtocolDetailResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProtocolRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateProtocolCommand(req.VisitPublicId, req.DoctorId, req.ProtocolType), ct);
        return CreatedAtAction(nameof(GetDetail), new { publicId = result.PublicId }, result);
    }

    /// <summary>Hekim hastayı odaya çağırır — started_at set edilir, randevu "Odaya Alındı" olur.</summary>
    [HttpPost("{publicId:guid}/start")]
    [RequirePermission("protocol:update")]
    [ProducesResponseType(typeof(ProtocolDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Start(Guid publicId, CancellationToken ct)
    {
        var result = await _mediator.Send(new StartProtocolCommand(publicId), ct);
        return Ok(result);
    }

    /// <summary>Protokolü tamamlar.</summary>
    [HttpPost("{publicId:guid}/complete")]
    [RequirePermission("protocol:update")]
    [ProducesResponseType(typeof(ProtocolDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(Guid publicId, CancellationToken ct)
    {
        var result = await _mediator.Send(new CompleteProtocolCommand(publicId), ct);
        return Ok(result);
    }

    /// <summary>Hekimin aktif protokolleri.</summary>
    [HttpGet("my")]
    [RequirePermission("protocol:view")]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorProtocolResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProtocols([FromQuery] long? doctorId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDoctorProtocolsQuery(doctorId), ct);
        return Ok(result);
    }

    /// <summary>Protokol detayı (tanılar dahil).</summary>
    [HttpGet("{publicId:guid}")]
    [RequirePermission("protocol:view")]
    [ProducesResponseType(typeof(ProtocolDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetail(Guid publicId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProtocolDetailQuery(publicId), ct);
        return Ok(result);
    }

    /// <summary>Protokol notlarını güncelle (şikayet, muayene bulguları, tanı, tedavi planı).</summary>
    [HttpPut("{publicId:guid}/details")]
    [RequirePermission("protocol:update")]
    [ProducesResponseType(typeof(ProtocolDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDetails(Guid publicId, [FromBody] UpdateProtocolDetailsRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateProtocolDetailsCommand(publicId, req.ChiefComplaint, req.ExaminationFindings, req.Diagnosis, req.TreatmentPlan, req.Notes), ct);
        return Ok(result);
    }

    /// <summary>ICD kodu arama (autocomplete).</summary>
    [HttpGet("icd/search")]
    [RequirePermission("protocol:view")]
    [ProducesResponseType(typeof(IReadOnlyList<IcdCodeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchIcd([FromQuery] string? q, [FromQuery] int? type, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SearchIcdCodesQuery(q, type, Math.Min(limit, 50)), ct);
        return Ok(result);
    }

    /// <summary>Protokole ICD tanı kodu ekle.</summary>
    [HttpPost("{publicId:guid}/diagnoses")]
    [RequirePermission("protocol:update")]
    [ProducesResponseType(typeof(ProtocolDiagnosisResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddDiagnosis(Guid publicId, [FromBody] AddDiagnosisRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new AddProtocolDiagnosisCommand(publicId, req.IcdCodeId, req.IsPrimary, req.Note), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Protokol tanısını sil.</summary>
    [HttpDelete("{publicId:guid}/diagnoses/{entryId:guid}")]
    [RequirePermission("protocol:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveDiagnosis(Guid publicId, Guid entryId, CancellationToken ct)
    {
        await _mediator.Send(new RemoveProtocolDiagnosisCommand(publicId, entryId), ct);
        return NoContent();
    }

    /// <summary>Hastanın protokol geçmişi.</summary>
    [HttpGet("patient/{patientPublicId:guid}/history")]
    [RequirePermission("protocol:view")]
    [ProducesResponseType(typeof(IReadOnlyList<ProtocolHistoryItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatientHistory(Guid patientPublicId, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPatientProtocolHistoryQuery(patientPublicId, limit), ct);
        return Ok(result);
    }
}

// ─── Request DTO ──────────────────────────────────────────────────────────

public record CreateProtocolRequest(Guid VisitPublicId, long DoctorId, int ProtocolType);
public record ProtocolTypeResponse(int Id, string Name, string Code, string Color, string? Description);
public record UpdateProtocolDetailsRequest(string? ChiefComplaint, string? ExaminationFindings, string? Diagnosis, string? TreatmentPlan, string? Notes);
public record AddDiagnosisRequest(long IcdCodeId, bool IsPrimary, string? Note);
