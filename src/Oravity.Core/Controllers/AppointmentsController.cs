using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Core.Modules.Appointment.Application.Commands;
using Oravity.Core.Modules.Appointment.Application.Queries;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

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
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public AppointmentsController(IMediator mediator, AppDbContext db, ITenantContext tenant)
    {
        _mediator = mediator;
        _db = db;
        _tenant = tenant;
    }

    /// <summary>
    /// Randevu durumlarını döner (takvim renkleri dahil).
    /// Frontend bu endpoint üzerinden statüsleri dinamik yükler.
    /// </summary>
    [HttpGet("statuses")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentStatusResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatuses()
    {
        var items = await _db.AppointmentStatuses
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .Select(s => new AppointmentStatusResponse(
                s.Id, s.Name, s.Code,
                s.TitleColor, s.ContainerColor, s.BorderColor, s.TextColor,
                s.ClassName, s.IsPatientStatus, s.AllowedNextStatusIds))
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Randevu tiplerini döner (hasta randevusu ve hekim blokları).</summary>
    [HttpGet("types")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentTypeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTypes()
    {
        var items = await _db.AppointmentTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .Select(t => new AppointmentTypeResponse(
                t.Id, t.Name, t.Code, t.Color, t.IsPatientAppointment, t.DefaultDurationMinutes))
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Uzmanlık alanlarını döner.</summary>
    [HttpGet("specializations")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(IReadOnlyList<SpecializationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpecializations()
    {
        var items = await _db.Specializations
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .Select(s => new SpecializationResponse(s.Id, s.Name, s.Code))
            .ToListAsync();

        return Ok(items);
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
            request.BranchId ?? _tenant.BranchId,
            request.AppointmentTypeId,
            request.StartTime,
            request.EndTime,
            request.Notes,
            request.IsUrgent,
            request.IsEarlierRequest));

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
            new UpdateAppointmentStatusCommand(publicId, request.StatusId));
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

    /// <summary>
    /// Takvim görünüm ayarlarını döner (slot aralığı, gün başlangıç/bitiş saati).
    /// Birden fazla şube varsa, erişilebilir ilk şubenin ayarı kullanılır.
    /// </summary>
    [HttpGet("calendar-settings")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(CalendarSettingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCalendarSettings()
    {
        // Kullanıcının birincil şubesini bul
        long? branchId = _tenant.BranchId;

        if (branchId is null && _tenant.CompanyId.HasValue)
        {
            branchId = await _db.Branches
                .Where(b => b.CompanyId == _tenant.CompanyId.Value && b.IsActive)
                .OrderBy(b => b.Id)
                .Select(b => (long?)b.Id)
                .FirstOrDefaultAsync();
        }

        if (branchId is null)
            return Ok(new CalendarSettingsResponse(30, 8, 20));

        var settings = await _db.BranchCalendarSettings
            .Where(s => s.BranchId == branchId.Value)
            .Select(s => new CalendarSettingsResponse(s.SlotIntervalMinutes, s.DayStartHour, s.DayEndHour))
            .FirstOrDefaultAsync();

        return Ok(settings ?? new CalendarSettingsResponse(30, 8, 20));
    }

    /// <summary>
    /// Şube takvim ayarlarını günceller (slot aralığı, gün saatleri).
    /// </summary>
    [HttpPut("calendar-settings")]
    [RequirePermission("appointment:edit")]
    [ProducesResponseType(typeof(CalendarSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCalendarSettings([FromBody] UpdateCalendarSettingsRequest request)
    {
        int[] validIntervals = [10, 15, 20, 30, 60];
        if (!validIntervals.Contains(request.SlotIntervalMinutes))
            return BadRequest($"Geçerli slot aralıkları: {string.Join(", ", validIntervals)} dakika.");

        if (request.DayStartHour < 0 || request.DayStartHour > 22 ||
            request.DayEndHour < 1 || request.DayEndHour > 24 ||
            request.DayStartHour >= request.DayEndHour)
            return BadRequest("Geçersiz gün saati aralığı.");

        long? branchId = _tenant.BranchId;
        if (branchId is null && _tenant.CompanyId.HasValue)
        {
            branchId = await _db.Branches
                .Where(b => b.CompanyId == _tenant.CompanyId.Value && b.IsActive)
                .OrderBy(b => b.Id)
                .Select(b => (long?)b.Id)
                .FirstOrDefaultAsync();
        }

        if (branchId is null) return BadRequest("Şube bulunamadı.");

        var settings = await _db.BranchCalendarSettings
            .FirstOrDefaultAsync(s => s.BranchId == branchId.Value);

        if (settings is null)
        {
            settings = BranchCalendarSettings.Create(branchId.Value);
            _db.BranchCalendarSettings.Add(settings);
        }

        settings.Update(request.SlotIntervalMinutes, request.DayStartHour, request.DayEndHour);
        await _db.SaveChangesAsync();

        return Ok(new CalendarSettingsResponse(
            settings.SlotIntervalMinutes, settings.DayStartHour, settings.DayEndHour));
    }

    /// <summary>
    /// Oturum açan kullanıcının erişebildiği şubeleri döner.
    /// Şube seviyesindeyse yalnızca kendi şubesi; şirket/platform admin ise tüm şubeleri görür.
    /// </summary>
    [HttpGet("accessible-branches")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(IReadOnlyList<AccessibleBranchResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccessibleBranches()
    {
        IQueryable<Branch> query = _db.Branches.Where(b => b.IsActive);

        if (_tenant.IsPlatformAdmin)
        {
            // Platform admin her şeyi görür — herhangi bir filtre yok
        }
        else if (_tenant.IsCompanyAdmin && _tenant.CompanyId.HasValue)
        {
            query = query.Where(b => b.CompanyId == _tenant.CompanyId.Value);
        }
        else
        {
            // Şube seviyesindeki kullanıcılar: UserRoleAssignment.BranchId üzerinden
            var assignedBranchIds = await _db.UserRoleAssignments
                .Where(a => a.UserId == _tenant.UserId && a.IsActive && a.BranchId != null)
                .Select(a => a.BranchId!.Value)
                .Distinct()
                .ToListAsync();

            // Şirket geneli atamaları da kontrol et (BranchId null = tüm şubeler)
            var hasCompanyWideRole = await _db.UserRoleAssignments
                .AnyAsync(a => a.UserId == _tenant.UserId && a.IsActive
                               && a.BranchId == null && a.CompanyId == _tenant.CompanyId);

            if (hasCompanyWideRole && _tenant.CompanyId.HasValue)
                query = query.Where(b => b.CompanyId == _tenant.CompanyId.Value);
            else
                query = query.Where(b => assignedBranchIds.Contains(b.Id));
        }

        var branches = await query
            .OrderBy(b => b.Name)
            .Select(b => new AccessibleBranchResponse(b.Id, b.Name))
            .ToListAsync();

        return Ok(branches);
    }

    /// <summary>
    /// Belirli bir tarihteki doktor takvim bilgilerini döner.
    /// Özel gün kaydı (DoctorSpecialDay) varsa genel takvimi (DoctorSchedule) override eder.
    /// </summary>
    [HttpGet("calendar-doctors")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorCalendarInfoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCalendarDoctors(
        [FromQuery] DateOnly date,
        [FromQuery] long[]? branchIds = null,
        [FromQuery] long[]? specializationIds = null)
    {
        // 1. Kullanıcının erişebildiği şube id'leri
        IQueryable<long> accessibleBranchIdQuery;

        if (_tenant.IsPlatformAdmin)
        {
            accessibleBranchIdQuery = _db.Branches.Where(b => b.IsActive).Select(b => b.Id);
        }
        else if (_tenant.IsCompanyAdmin && _tenant.CompanyId.HasValue)
        {
            accessibleBranchIdQuery = _db.Branches
                .Where(b => b.IsActive && b.CompanyId == _tenant.CompanyId.Value)
                .Select(b => b.Id);
        }
        else
        {
            var assignedBranchIds = await _db.UserRoleAssignments
                .Where(a => a.UserId == _tenant.UserId && a.IsActive && a.BranchId != null)
                .Select(a => a.BranchId!.Value)
                .Distinct()
                .ToListAsync();

            var hasCompanyWide = await _db.UserRoleAssignments
                .AnyAsync(a => a.UserId == _tenant.UserId && a.IsActive
                               && a.BranchId == null && a.CompanyId == _tenant.CompanyId);

            if (hasCompanyWide && _tenant.CompanyId.HasValue)
                accessibleBranchIdQuery = _db.Branches
                    .Where(b => b.IsActive && b.CompanyId == _tenant.CompanyId.Value)
                    .Select(b => b.Id);
            else
                accessibleBranchIdQuery = _db.Branches
                    .Where(b => b.IsActive && assignedBranchIds.Contains(b.Id))
                    .Select(b => b.Id);
        }

        var accessibleIds = await accessibleBranchIdQuery.ToListAsync();

        // 2. Filtre: istekse gelen branchIds ile kesişim
        var effectiveBranchIds = (branchIds != null && branchIds.Length > 0)
            ? accessibleIds.Intersect(branchIds).ToList()
            : accessibleIds;

        if (effectiveBranchIds.Count == 0)
            return Ok(Array.Empty<DoctorCalendarInfoResponse>());

        // 3. DOCTOR rolüne sahip kullanıcıları bul
        var doctorQuery = _db.UserRoleAssignments
            .Where(a => a.IsActive
                        && a.RoleTemplate.Code == "DOCTOR"
                        && (a.BranchId == null
                            ? effectiveBranchIds.Contains(a.CompanyId ?? 0)  // company-wide (edge case)
                            : effectiveBranchIds.Contains(a.BranchId.Value)))
            .Select(a => new { a.UserId, a.BranchId });

        // BranchId null olan şirket geneli doktor atamaları için
        var companyWideDoctors = _db.UserRoleAssignments
            .Where(a => a.IsActive
                        && a.RoleTemplate.Code == "DOCTOR"
                        && a.BranchId == null
                        && a.CompanyId == _tenant.CompanyId)
            .Select(a => new { a.UserId, BranchId = (long?)null });

        var doctorUserIds = await _db.UserRoleAssignments
            .Where(a => a.IsActive
                        && a.RoleTemplate.Code == "DOCTOR"
                        && (effectiveBranchIds.Contains(a.BranchId ?? 0) || a.BranchId == null))
            .Select(a => a.UserId)
            .Distinct()
            .ToListAsync();

        if (doctorUserIds.Count == 0)
            return Ok(Array.Empty<DoctorCalendarInfoResponse>());

        // 4. Doktor bilgilerini çek
        var doctors = await _db.Users
            .Where(u => u.IsActive && doctorUserIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Title,
                u.CalendarColor,
                u.SpecializationId,
                SpecializationName = u.Specialization != null ? u.Specialization.Name : null,
                u.IsChiefPhysician
            })
            .ToListAsync();

        // Uzmanlık filtresi
        if (specializationIds != null && specializationIds.Length > 0)
            doctors = doctors.Where(d => d.SpecializationId.HasValue
                                         && specializationIds.Contains(d.SpecializationId.Value)).ToList();

        if (doctors.Count == 0)
            return Ok(Array.Empty<DoctorCalendarInfoResponse>());

        var finalDoctorIds = doctors.Select(d => d.Id).ToList();

        // 5. DoctorSchedule haftalık takvim — o gün için
        // DayOfWeek mapping: C# Sunday=0→7, Monday=1→1, ..., Saturday=6→6
        int customDow = date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;

        var schedules = await _db.DoctorSchedules
            .Where(s => s.IsActive
                        && finalDoctorIds.Contains(s.DoctorId)
                        && effectiveBranchIds.Contains(s.BranchId)
                        && s.DayOfWeek == customDow)
            .Select(s => new
            {
                s.DoctorId,
                s.BranchId,
                s.IsWorking,
                s.StartTime,
                s.EndTime,
                s.BreakStart,
                s.BreakEnd,
                s.BreakLabel
            })
            .ToListAsync();

        // 6. DoctorSpecialDay — override
        var specialDays = await _db.DoctorSpecialDays
            .Where(sd => sd.IsActive
                         && finalDoctorIds.Contains(sd.DoctorId)
                         && effectiveBranchIds.Contains(sd.BranchId)
                         && sd.SpecificDate == date)
            .Select(sd => new
            {
                sd.DoctorId,
                sd.BranchId,
                sd.StartTime,
                sd.EndTime,
                sd.Type,
                sd.Reason
            })
            .ToListAsync();

        // 7. DoctorOnCallSettings
        var onCallSettings = await _db.DoctorOnCallSettings
            .Where(oc => oc.IsActive && finalDoctorIds.Contains(oc.DoctorId)
                         && effectiveBranchIds.Contains(oc.BranchId))
            .Select(oc => new
            {
                oc.DoctorId,
                oc.BranchId,
                oc.Monday, oc.Tuesday, oc.Wednesday, oc.Thursday,
                oc.Friday, oc.Saturday, oc.Sunday
            })
            .ToListAsync();

        // 8. Doktor-şube eşleşmelerini bul (UserRoleAssignment.BranchId ile)
        var doctorBranchMap = await _db.UserRoleAssignments
            .Where(a => a.IsActive
                        && a.RoleTemplate.Code == "DOCTOR"
                        && finalDoctorIds.Contains(a.UserId)
                        && (a.BranchId == null || effectiveBranchIds.Contains(a.BranchId.Value)))
            .Select(a => new { a.UserId, a.BranchId, a.CompanyId })
            .ToListAsync();

        var branches = await _db.Branches
            .Where(b => effectiveBranchIds.Contains(b.Id))
            .Select(b => new { b.Id, b.Name })
            .ToDictionaryAsync(b => b.Id, b => b.Name);

        // 9. Sonuç listesi oluştur
        var result = new List<DoctorCalendarInfoResponse>();

        foreach (var doctor in doctors)
        {
            // Her doktorun şubelerini bul
            var doctorBranches = doctorBranchMap
                .Where(m => m.UserId == doctor.Id)
                .Select(m => m.BranchId)
                .Where(bid => bid.HasValue)
                .Select(bid => bid!.Value)
                .Distinct()
                .ToList();

            // BranchId null (company-wide) varsa tüm accessible şubeler
            if (doctorBranchMap.Any(m => m.UserId == doctor.Id && m.BranchId == null))
                doctorBranches = effectiveBranchIds;

            foreach (var branchId in doctorBranches)
            {
                if (!branches.TryGetValue(branchId, out var branchName)) continue;

                // Özel gün override
                var special = specialDays.FirstOrDefault(sd => sd.DoctorId == doctor.Id && sd.BranchId == branchId);
                // Haftalık program
                var sched = schedules.FirstOrDefault(s => s.DoctorId == doctor.Id && s.BranchId == branchId);
                // Nöbet
                var onCall = onCallSettings.FirstOrDefault(oc => oc.DoctorId == doctor.Id && oc.BranchId == branchId);

                string? workStart = null, workEnd = null, breakStart = null, breakEnd = null, breakLabel = null;
                bool isOnCall = false;

                if (special != null)
                {
                    // DayOff → tüm saatler null (closed)
                    if (special.StartTime.HasValue)
                    {
                        workStart = special.StartTime.Value.ToString("HH:mm");
                        workEnd   = special.EndTime?.ToString("HH:mm");
                    }
                }
                else if (sched != null && sched.IsWorking)
                {
                    workStart  = sched.StartTime.ToString("HH:mm");
                    workEnd    = sched.EndTime.ToString("HH:mm");
                    breakStart = sched.BreakStart?.ToString("HH:mm");
                    breakEnd   = sched.BreakEnd?.ToString("HH:mm");
                    breakLabel = sched.BreakLabel;
                }

                if (onCall != null)
                {
                    isOnCall = date.DayOfWeek switch
                    {
                        DayOfWeek.Monday    => onCall.Monday,
                        DayOfWeek.Tuesday   => onCall.Tuesday,
                        DayOfWeek.Wednesday => onCall.Wednesday,
                        DayOfWeek.Thursday  => onCall.Thursday,
                        DayOfWeek.Friday    => onCall.Friday,
                        DayOfWeek.Saturday  => onCall.Saturday,
                        DayOfWeek.Sunday    => onCall.Sunday,
                        _                   => false
                    };
                }

                // O gün çalışmıyorsa (workStart null) takvime ekleme
                if (workStart is null) continue;

                result.Add(new DoctorCalendarInfoResponse(
                    DoctorId:          doctor.Id,
                    FullName:          doctor.FullName,
                    Title:             doctor.Title,
                    CalendarColor:     doctor.CalendarColor,
                    SpecializationId:  doctor.SpecializationId,
                    SpecializationName: doctor.SpecializationName,
                    BranchId:          branchId,
                    BranchName:        branchName,
                    WorkStart:         workStart,
                    WorkEnd:           workEnd,
                    BreakStart:        breakStart,
                    BreakEnd:          breakEnd,
                    BreakLabel:        breakLabel,
                    IsOnCall:          isOnCall,
                    IsChiefPhysician:  doctor.IsChiefPhysician,
                    IsSpecialDay:      special != null,
                    SpecialDayType:    special != null ? (int)special.Type : null,
                    SpecialDayReason:  special?.Reason
                ));
            }
        }

        return Ok(result.OrderBy(r => r.BranchName).ThenBy(r => r.FullName).ToList());
    }

    /// <summary>
    /// Belirli hastanın tüm randevularını listeler (en yeni önce).
    /// </summary>
    [HttpGet("patient/{patientPublicId:guid}")]
    [RequirePermission("appointment:view")]
    [ProducesResponseType(typeof(PatientAppointmentsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPatient(
        Guid patientPublicId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _mediator.Send(
            new GetPatientAppointmentsQuery(patientPublicId, pageSize, page));
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
    long? BranchId,
    int? AppointmentTypeId,
    DateTime StartTime,
    DateTime EndTime,
    string? Notes,
    bool IsUrgent = false,
    bool IsEarlierRequest = false
);

public record UpdateStatusRequest(int StatusId);

public record MoveAppointmentRequest(
    DateTime NewStartTime,
    DateTime NewEndTime,
    long? NewDoctorId,
    int ExpectedRowVersion
);

public record AppointmentStatusResponse(
    int    Id,
    string Name,
    string Code,
    string TitleColor,
    string ContainerColor,
    string BorderColor,
    string TextColor,
    string ClassName,
    bool   IsPatientStatus,
    string AllowedNextStatusIds
);

public record AppointmentTypeResponse(
    int    Id,
    string Name,
    string Code,
    string Color,
    bool   IsPatientAppointment,
    int    DefaultDurationMinutes
);

public record SpecializationResponse(int Id, string Name, string Code);

public record CalendarSettingsResponse(int SlotIntervalMinutes, int DayStartHour, int DayEndHour);
public record UpdateCalendarSettingsRequest(int SlotIntervalMinutes, int DayStartHour, int DayEndHour);

public record AccessibleBranchResponse(long Id, string Name);

public record DoctorCalendarInfoResponse(
    long    DoctorId,
    string  FullName,
    string? Title,
    string? CalendarColor,
    long?   SpecializationId,
    string? SpecializationName,
    long    BranchId,
    string  BranchName,
    string? WorkStart,
    string? WorkEnd,
    string? BreakStart,
    string? BreakEnd,
    string? BreakLabel,
    bool    IsOnCall,
    bool    IsChiefPhysician = false,
    bool    IsSpecialDay = false,
    int?    SpecialDayType = null,
    string? SpecialDayReason = null
);
