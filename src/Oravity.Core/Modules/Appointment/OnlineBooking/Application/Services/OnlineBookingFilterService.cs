using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Appointment.OnlineBooking.Application.Services;

public record OnlineDoctorDto(
    long DoctorId,
    string FullName,
    int SlotDurationMinutes,
    bool AutoApprove,
    string? BookingNote,
    long? SpecialityId
);

public record OnlineBookingContext(
    bool IsNewPatient,
    long? ExistingPatientId,
    IReadOnlyList<OnlineDoctorDto> AvailableDoctors,
    bool IsEnabled,
    string PrimaryColor,
    bool PatientTypeSplit,
    int CancellationHours
);

public interface IOnlineBookingFilterService
{
    Task<OnlineBookingContext> ResolveContext(
        long branchId, string? phone, string? tc,
        CancellationToken ct = default);
}

/// <summary>
/// Hasta tipi tespiti ve hekim filtreleme servisi (SPEC §ONLİNE RANDEVU SİSTEMİ §4).
/// Telefon/TC ile hastayı tespit eder; randevu geçmişine bakarak yeni/mevcut kararı verir.
/// patient_type_filter'a göre hekim listesini filtreler.
/// </summary>
public class OnlineBookingFilterService : IOnlineBookingFilterService
{
    private readonly AppDbContext _db;
    private readonly IEncryptionService _encryption;

    public OnlineBookingFilterService(AppDbContext db, IEncryptionService encryption)
    {
        _db         = db;
        _encryption = encryption;
    }

    public async Task<OnlineBookingContext> ResolveContext(
        long branchId, string? phone, string? tc,
        CancellationToken ct = default)
    {
        // Şube ayarları
        var branchSettings = await _db.BranchOnlineBookingSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BranchId == branchId, ct);

        if (branchSettings is null || !branchSettings.IsEnabled)
            return new OnlineBookingContext(true, null, [], false, "#2563eb", true, 24);

        // Hasta tespiti
        long? patientId = null;
        bool isNewPatient = true;

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var patient = await _db.Patients
                .AsNoTracking()
                .Where(p => p.Phone == phone && !p.IsDeleted)
                .Select(p => new { p.Id })
                .FirstOrDefaultAsync(ct);
            if (patient is not null) patientId = patient.Id;
        }

        if (patientId is null && !string.IsNullOrWhiteSpace(tc))
        {
            var hash = _encryption.HashSha256(tc);
            var patient = await _db.Patients
                .AsNoTracking()
                .Where(p => p.TcNumberHash == hash && !p.IsDeleted)
                .Select(p => new { p.Id })
                .FirstOrDefaultAsync(ct);
            if (patient is not null) patientId = patient.Id;
        }

        if (patientId is not null)
        {
            // Bu şubede daha önce randevusu var mı?
            isNewPatient = !await _db.Appointments
                .AnyAsync(a =>
                    a.PatientId == patientId.Value &&
                    a.BranchId == branchId &&
                    a.StatusId != AppointmentStatus.WellKnownIds.Cancelled,
                    ct);
        }

        // Online görünür hekimleri al + hasta tipine göre filtrele
        var doctors = await _db.DoctorOnlineBookingSettings
            .AsNoTracking()
            .Where(s =>
                s.BranchId == branchId &&
                s.IsOnlineVisible &&
                (s.PatientTypeFilter == 0 ||
                 (isNewPatient && s.PatientTypeFilter == 1) ||
                 (!isNewPatient && s.PatientTypeFilter == 2)))
            .Select(s => new OnlineDoctorDto(
                s.DoctorId,
                s.Doctor.FullName,
                s.SlotDurationMinutes,
                s.AutoApprove,
                s.BookingNote,
                s.SpecialityId))
            .ToListAsync(ct);

        return new OnlineBookingContext(
            IsNewPatient:      isNewPatient,
            ExistingPatientId: patientId,
            AvailableDoctors:  doctors,
            IsEnabled:         branchSettings.IsEnabled,
            PrimaryColor:      branchSettings.PrimaryColor,
            PatientTypeSplit:  branchSettings.PatientTypeSplit,
            CancellationHours: branchSettings.CancellationHours);
    }
}
