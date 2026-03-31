using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Core.PatientRecord.Application;
using Oravity.Core.Modules.Core.PatientRecord.Application.Commands;
using Oravity.Core.Modules.Core.PatientRecord.Application.Queries;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Controllers;

/// <summary>
/// Hasta dosyası ek sekmeleri: Anamnez, İlaçlar, Notlar, Dosyalar.
/// </summary>
[ApiController]
[Route("api/patients/{patientId:long}")]
[Authorize]
[Produces("application/json")]
public class PatientRecordsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PatientRecordsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Anamnesis ──────────────────────────────────────────────────────────

    /// <summary>
    /// Hastanın anamnez formunu döner. Kritik uyarılar (alerji, antikoagülan vb.)
    /// HasCriticalAlert ve CriticalAlerts alanlarında listelenir.
    /// </summary>
    [HttpGet("anamnesis")]
    [RequirePermission("patient:view")]
    [ProducesResponseType(typeof(PatientAnamnesisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetAnamnesis(long patientId)
    {
        var result = await _mediator.Send(new GetPatientAnamnesisQuery(patientId));
        return result is null ? NoContent() : Ok(result);
    }

    /// <summary>
    /// Anamnez formunu upsert eder (yok ise oluşturur, var ise günceller).
    /// </summary>
    [HttpPut("anamnesis")]
    [RequirePermission("anamnesis:edit")]
    [ProducesResponseType(typeof(PatientAnamnesisResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveAnamnesis(
        long patientId,
        [FromBody] PatientAnamnesisData data)
    {
        var result = await _mediator.Send(new SavePatientAnamnesisCommand(patientId, data));
        return Ok(result);
    }

    // ── Medications ────────────────────────────────────────────────────────

    /// <summary>Hastanın aktif ilaçlarını listeler.</summary>
    [HttpGet("medications")]
    [RequirePermission("patient:view")]
    [ProducesResponseType(typeof(IReadOnlyList<PatientMedicationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMedications(long patientId, [FromQuery] bool? activeOnly = true)
    {
        // Inline query — bağımsız bir MediatR query yerine doğrudan kullanıyoruz
        var meds = await _mediator.Send(new GetPatientMedicationsQuery(patientId, activeOnly));
        return Ok(meds);
    }

    /// <summary>Hastaya yeni ilaç ekler.</summary>
    [HttpPost("medications")]
    [RequirePermission("anamnesis:edit")]
    [ProducesResponseType(typeof(PatientMedicationResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddMedication(
        long patientId,
        [FromBody] AddMedicationRequest request)
    {
        var result = await _mediator.Send(new AddPatientMedicationCommand(
            patientId, request.DrugName, request.Dose, request.Frequency, request.Reason));
        return CreatedAtAction(nameof(GetMedications), new { patientId }, result);
    }

    /// <summary>İlaç kaydını günceller.</summary>
    [HttpPut("medications/{id:long}")]
    [RequirePermission("anamnesis:edit")]
    [ProducesResponseType(typeof(PatientMedicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMedication(
        long patientId,
        long id,
        [FromBody] UpdateMedicationRequest request)
    {
        var result = await _mediator.Send(new UpdatePatientMedicationCommand(
            patientId, id,
            request.DrugName, request.Dose, request.Frequency, request.Reason, request.IsActive));
        return Ok(result);
    }

    // ── Notes ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Hasta notlarını döner. Pinlenmiş notlar önce gelir.
    /// Gizli notlar (NoteType=3) yalnızca patient:write_hidden_note yetkisiyle görülür.
    /// </summary>
    [HttpGet("notes")]
    [RequirePermission("note:write_patient")]
    [ProducesResponseType(typeof(IReadOnlyList<PatientNoteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotes(
        long patientId,
        [FromQuery] NoteType? type = null)
    {
        var result = await _mediator.Send(new GetPatientNotesQuery(patientId, type));
        return Ok(result);
    }

    /// <summary>Yeni not oluşturur. Gizli not için ek yetki gerekir (handler'da kontrol edilir).</summary>
    [HttpPost("notes")]
    [RequirePermission("note:write_patient")]
    [ProducesResponseType(typeof(PatientNoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateNote(
        long patientId,
        [FromBody] CreateNoteRequest request)
    {
        var result = await _mediator.Send(new CreatePatientNoteCommand(
            patientId, request.Type, request.Content,
            request.Title, request.IsPinned, request.AppointmentId));
        return CreatedAtAction(nameof(GetNotes), new { patientId }, result);
    }

    /// <summary>Notu soft-delete ile siler (deleted_at = now).</summary>
    [HttpDelete("notes/{id:guid}")]
    [RequirePermission("note:delete_patient")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNote(long patientId, Guid id)
    {
        await _mediator.Send(new DeletePatientNoteCommand(patientId, id));
        return NoContent();
    }

    // ── Files ──────────────────────────────────────────────────────────────

    /// <summary>Hasta dosyalarını döner. file_type ile filtrelenebilir.</summary>
    [HttpGet("files")]
    [RequirePermission("patient:view")]
    [ProducesResponseType(typeof(IReadOnlyList<PatientFileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFiles(
        long patientId,
        [FromQuery] PatientFileType? type = null)
    {
        var result = await _mediator.Send(new GetPatientFilesQuery(patientId, type));
        return Ok(result);
    }

    /// <summary>
    /// Dosya meta kaydını oluşturur. FilePath: fiziksel upload tamamlandıktan sonra iletilen URL/path.
    /// </summary>
    [HttpPost("files")]
    [RequirePermission("patient:upload_document")]
    [ProducesResponseType(typeof(PatientFileResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> UploadFile(
        long patientId,
        [FromBody] UploadFileRequest request)
    {
        var result = await _mediator.Send(new UploadPatientFileCommand(
            patientId, request.FileType, request.FilePath,
            request.Category, request.Title,
            request.FileSize, request.FileExt, request.Note, request.TakenAt));
        return CreatedAtAction(nameof(GetFiles), new { patientId }, result);
    }
}

// ─── Request DTO'lar ───────────────────────────────────────────────────────

public record AddMedicationRequest(
    string DrugName,
    string? Dose = null,
    string? Frequency = null,
    string? Reason = null
);

public record UpdateMedicationRequest(
    string DrugName,
    string? Dose = null,
    string? Frequency = null,
    string? Reason = null,
    bool IsActive = true
);

public record CreateNoteRequest(
    NoteType Type,
    string Content,
    string? Title = null,
    bool IsPinned = false,
    long? AppointmentId = null
);

public record UploadFileRequest(
    PatientFileType FileType,
    string FilePath,
    string? Category = null,
    string? Title = null,
    int? FileSize = null,
    string? FileExt = null,
    string? Note = null,
    DateTime? TakenAt = null
);
