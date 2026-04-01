using MediatR;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Modules.Appointment.OnlineBooking.Application.Commands;
using Oravity.Core.Modules.Appointment.OnlineBooking.Application.Queries;
using Oravity.Core.Modules.Appointment.OnlineBooking.Application.Services;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace Oravity.Core.Controllers;

/// <summary>
/// Anonim online randevu widget/portal endpoint'leri (SPEC §ONLİNE RANDEVU SİSTEMİ §5).
/// JWT gerektirmez — halka açık erişim.
/// </summary>
[ApiController]
[Route("api/public/{slug}")]
[Tags("Public Booking")]
public class PublicBookingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IOnlineBookingFilterService _filterService;
    private readonly AppDbContext _db;

    public PublicBookingController(
        IMediator mediator,
        IOnlineBookingFilterService filterService,
        AppDbContext db)
    {
        _mediator      = mediator;
        _filterService = filterService;
        _db            = db;
    }

    /// <summary>
    /// Şubenin online görünür hekimleri — hasta tipi ve telefon/TC bilgisine göre filtreli.
    /// </summary>
    /// <remarks>
    /// phone veya tc parametresi gönderilirse hasta tipi (yeni/mevcut) otomatik tespit edilir
    /// ve patient_type_filter uygulanır.
    /// </remarks>
    [HttpGet("doctors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOnlineDoctors(
        string slug,
        [FromQuery] string? phone = null,
        [FromQuery] string? tc = null,
        CancellationToken ct = default)
    {
        // Slug → BranchId
        var settings = await _db.BranchOnlineBookingSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.WidgetSlug == slug, ct);

        if (settings is null || !settings.IsEnabled)
            return NotFound(new { message = "Bu slug ile etkin bir şube bulunamadı." });

        var context = await _filterService.ResolveContext(settings.BranchId, phone, tc, ct);

        return Ok(new
        {
            branchId         = settings.BranchId,
            isNewPatient     = context.IsNewPatient,
            patientTypeSplit = context.PatientTypeSplit,
            primaryColor     = context.PrimaryColor,
            cancellationHours = context.CancellationHours,
            doctors          = context.AvailableDoctors
        });
    }

    /// <summary>
    /// Belirli hekim + tarih için müsait randevu slotları.
    /// </summary>
    [HttpGet("slots")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableSlots(
        string slug,
        [FromQuery] long doctorId,
        [FromQuery] DateOnly date,
        CancellationToken ct = default)
    {
        var slots = await _mediator.Send(
            new GetAvailableSlotsQuery(slug, doctorId, date), ct);
        return Ok(slots);
    }

    /// <summary>
    /// Yeni online randevu talebi oluştur.
    /// Telefon numarası varsa SMS doğrulama kodu gönderilir.
    /// auto_approve=true olan hekimler için doğrudan Appointment oluşturulur.
    /// </summary>
    [HttpPost("requests")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateBookingRequest(
        string slug,
        [FromBody] CreateBookingRequestBody body,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CreateOnlineBookingRequestCommand(
            BranchSlug:    slug,
            DoctorId:      body.DoctorId,
            PatientType:   body.PatientType,
            RequestedDate: body.RequestedDate,
            RequestedTime: body.RequestedTime,
            SlotDuration:  body.SlotDuration,
            Source:        body.Source,
            PatientId:     body.PatientId,
            FirstName:     body.FirstName,
            LastName:      body.LastName,
            Phone:         body.Phone,
            Email:         body.Email,
            PatientNote:   body.PatientNote), ct);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Telefon doğrulama kodu onayla.
    /// </summary>
    [HttpPost("verify-phone")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyPhone(
        string slug,
        [FromBody] VerifyPhoneBody body,
        CancellationToken ct = default)
    {
        var success = await _mediator.Send(
            new VerifyPhoneCommand(body.RequestPublicId, body.Code), ct);

        if (!success)
            return BadRequest(new { message = "Doğrulama kodu hatalı veya süresi dolmuş." });

        return Ok(new { message = "Telefon doğrulandı." });
    }
}

public record CreateBookingRequestBody(
    long DoctorId,
    BookingPatientType PatientType,
    DateOnly RequestedDate,
    TimeOnly RequestedTime,
    int SlotDuration,
    BookingSource Source = BookingSource.Widget,
    long? PatientId = null,
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    string? Email = null,
    string? PatientNote = null
);

public record VerifyPhoneBody(Guid RequestPublicId, string Code);
