using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Core.Patient.Application;
using Oravity.Core.Modules.Core.Patient.Application.Commands;
using Oravity.Core.Modules.Core.Patient.Application.Queries;

namespace Oravity.Core.Controllers;

/// <summary>
/// Hasta yönetimi endpoint'leri.
/// Tüm işlemler JWT ile kimlik doğrulaması + izin kontrolü gerektirir.
/// Tenant izolasyonu: JWT branch_id veya company_id claim'i üzerinden otomatik uygulanır.
/// </summary>
[ApiController]
[Route("api/patients")]
[Authorize]
[Produces("application/json")]
public class PatientsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PatientsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Hasta listesi — ad, soyad, telefon veya TC hash ile arama (sayfalı).
    /// TC ile arama için önce client tarafında SHA-256 hash hesaplayıp tcHash parametresi olarak gönderin.
    /// </summary>
    [HttpGet]
    [RequirePermission("patient:view")]
    [ProducesResponseType(typeof(PagedResult<PatientResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] string? firstName,
        [FromQuery] string? lastName,
        [FromQuery] string? phone,
        [FromQuery] string? tcHash,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(
            new SearchPatientsQuery(search, firstName, lastName, phone, tcHash, page, pageSize));
        return Ok(result);
    }

    /// <summary>Tek hasta detayı — publicId (UUID) ile sorgula</summary>
    [HttpGet("{publicId:guid}")]
    [RequirePermission("patient:view")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid publicId)
    {
        var result = await _mediator.Send(new GetPatientByIdQuery(publicId));
        return Ok(result);
    }

    /// <summary>
    /// Yeni hasta kaydı oluştur.
    /// TcNumber verilirse şifreli olarak kaydedilir, arama için hash'i tutulur.
    /// </summary>
    [HttpPost]
    [RequirePermission("patient:create")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request)
    {
        var result = await _mediator.Send(new CreatePatientCommand(
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Email,
            request.BirthDate,
            request.Gender,
            request.TcNumber,
            request.Address,
            request.BloodType,
            request.PreferredLanguageCode,
            request.BranchId));

        return CreatedAtAction(nameof(GetById), new { publicId = result.PublicId }, result);
    }

    /// <summary>Hasta temel bilgilerini güncelle (TC Kimlik No dahil değil)</summary>
    [HttpPut("{publicId:guid}")]
    [RequirePermission("patient:edit_basic")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid publicId, [FromBody] UpdatePatientRequest request)
    {
        var result = await _mediator.Send(new UpdatePatientCommand(
            publicId,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Email,
            request.BirthDate,
            request.Gender,
            request.Address,
            request.BloodType,
            request.PreferredLanguageCode,
            request.MotherName,
            request.FatherName,
            request.MaritalStatus,
            request.Nationality,
            request.Occupation,
            request.SmokingType,
            request.PregnancyStatus,
            request.HomePhone,
            request.WorkPhone,
            request.Country,
            request.City,
            request.District,
            request.ReferralSourceId,
            request.ReferralPerson,
            request.LastInstitutionId,
            request.CitizenshipTypeId,
            request.Notes,
            request.SmsOptIn,
            request.CampaignOptIn,
            request.TcNumber,
            request.PassportNo));

        return Ok(result);
    }

    /// <summary>Hastanın anamnez formunu getir — kayıt yoksa 204 döner</summary>
    [HttpGet("{publicId:guid}/anamnesis")]
    [RequirePermission("patient:view")]
    [ProducesResponseType(typeof(PatientAnamnesisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnamnesis(Guid publicId)
    {
        var result = await _mediator.Send(new GetPatientAnamnesisQuery(publicId));
        return result == null ? NoContent() : Ok(result);
    }

    /// <summary>Hastanın anamnez geçmişini döner — tüm kayıtlar, yeniden eskiye</summary>
    [HttpGet("{publicId:guid}/anamnesis/history")]
    [RequirePermission("patient:view")]
    [ProducesResponseType(typeof(IReadOnlyList<AnamnesisHistoryItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnamnesisHistory(Guid publicId, [FromQuery] int limit = 50)
    {
        var result = await _mediator.Send(new GetPatientAnamnesisHistoryQuery(publicId, limit));
        return Ok(result);
    }

    /// <summary>Belirli bir anamnez kaydını publicId ile döner.</summary>
    [HttpGet("{publicId:guid}/anamnesis/{anamnesisPublicId:guid}")]
    [RequirePermission("patient:view")]
    [ProducesResponseType(typeof(PatientAnamnesisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnamnesisById(Guid publicId, Guid anamnesisPublicId)
    {
        var result = await _mediator.Send(new GetPatientAnamnesisByIdQuery(publicId, anamnesisPublicId));
        return Ok(result);
    }

    /// <summary>Anamnez formunu kaydet — her kayıt yeni satır olarak eklenir (geçmiş korunur)</summary>
    [HttpPut("{publicId:guid}/anamnesis")]
    [RequirePermission("patient:edit_basic")]
    [ProducesResponseType(typeof(PatientAnamnesisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpsertAnamnesis(Guid publicId, [FromBody] UpsertAnamnesisRequest request)
    {
        var data = new SharedKernel.Entities.PatientAnamnesisData(
            request.BloodType, request.IsPregnant, request.IsBreastfeeding,
            request.HasDiabetes, request.HasHypertension, request.HasHeartDisease, request.HasPacemaker,
            request.HasAsthma, request.HasEpilepsy, request.HasKidneyDisease, request.HasLiverDisease,
            request.HasHiv, request.HasHepatitisB, request.HasHepatitisC, request.OtherSystemicDiseases,
            request.LocalAnesthesiaAllergy, request.LocalAnesthesiaAllergyNote,
            request.BleedingTendency, request.OnAnticoagulant, request.AnticoagulantDrug, request.BisphosphonateUse,
            request.HasPenicillinAllergy, request.HasAspirinAllergy, request.HasLatexAllergy, request.OtherAllergies,
            request.PreviousSurgeries,
            request.BrushingFrequency, request.UsesFloss,
            request.SmokingStatus, request.SmokingAmount, request.AlcoholUse,
            request.AdditionalNotes);

        var result = await _mediator.Send(new UpsertPatientAnamnesisCommand(publicId, data));
        return Ok(result);
    }

    /// <summary>Hasta kaydını sil (soft delete — fiziksel silme yapılmaz)</summary>
    [HttpDelete("{publicId:guid}")]
    [RequirePermission("patient:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid publicId)
    {
        await _mediator.Send(new DeletePatientCommand(publicId));
        return NoContent();
    }
}

// ─── Request DTO'lar ───────────────────────────────────────────────────────

/// <summary>Hasta oluşturma isteği</summary>
public record CreatePatientRequest(
    string FirstName,
    string LastName,
    string? Phone,
    string? Email,
    DateOnly? BirthDate,
    string? Gender,
    /// <summary>Ham TC Kimlik No — API'ye gönderilir, şifreli saklanır</summary>
    string? TcNumber,
    string? Address,
    string? BloodType,
    string? PreferredLanguageCode,
    long? BranchId = null
);

/// <summary>Anamnez upsert isteği</summary>
public record UpsertAnamnesisRequest(
    string? BloodType,
    bool IsPregnant,
    bool IsBreastfeeding,
    bool HasDiabetes,
    bool HasHypertension,
    bool HasHeartDisease,
    bool HasPacemaker,
    bool HasAsthma,
    bool HasEpilepsy,
    bool HasKidneyDisease,
    bool HasLiverDisease,
    bool HasHiv,
    bool HasHepatitisB,
    bool HasHepatitisC,
    string? OtherSystemicDiseases,
    bool LocalAnesthesiaAllergy,
    string? LocalAnesthesiaAllergyNote,
    bool BleedingTendency,
    bool OnAnticoagulant,
    string? AnticoagulantDrug,
    bool BisphosphonateUse,
    bool HasPenicillinAllergy,
    bool HasAspirinAllergy,
    bool HasLatexAllergy,
    string? OtherAllergies,
    string? PreviousSurgeries,
    int? BrushingFrequency,
    bool UsesFloss,
    int? SmokingStatus,
    string? SmokingAmount,
    int? AlcoholUse,
    string? AdditionalNotes
);

/// <summary>Hasta güncelleme isteği</summary>
public record UpdatePatientRequest(
    string FirstName,
    string LastName,
    // Kimlik
    string? TcNumber,
    string? PassportNo,
    // Kişisel
    string? MotherName,
    string? FatherName,
    string? Gender,
    string? MaritalStatus,
    string? Nationality,
    long? CitizenshipTypeId,
    string? Occupation,
    string? SmokingType,
    int? PregnancyStatus,
    DateOnly? BirthDate,
    // İletişim
    string? Phone,
    string? HomePhone,
    string? WorkPhone,
    string? Email,
    // Adres
    string? Country,
    string? City,
    string? District,
    string? Address,
    // Tıbbi
    string? BloodType,
    // Geliş / Kurum
    long? ReferralSourceId,
    string? ReferralPerson,
    long? LastInstitutionId,
    // Sistem
    string? Notes,
    string? PreferredLanguageCode,
    bool? SmsOptIn,
    bool? CampaignOptIn
);
