namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hekim bazlı online randevu ayarları (SPEC §ONLİNE RANDEVU SİSTEMİ §2.1).
/// UNIQUE(doctor_id, branch_id) — her hekim+şube çifti için tek kayıt.
/// BaseEntity türemez — public_id / is_deleted / updated_at gerekmez, settings tablosudur.
/// </summary>
public class DoctorOnlineBookingSettings
{
    public long Id { get; private set; }

    public long DoctorId { get; private set; }
    public User Doctor { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    /// <summary>Hekimin online randevuda görünüp görünmeyeceği.</summary>
    public bool IsOnlineVisible { get; private set; } = true;

    /// <summary>Slot süresi dakika cinsinden (10/15/20/30/45/60).</summary>
    public int SlotDurationMinutes { get; private set; } = 30;

    /// <summary>true = direkt onaylanır; false = resepsiyon onayı bekler.</summary>
    public bool AutoApprove { get; private set; }

    /// <summary>Kaç gün öncesine kadar randevu alınabilir.</summary>
    public int MaxAdvanceDays { get; private set; } = 60;

    /// <summary>Widget'ta hasta için gösterilecek yönlendirme mesajı.</summary>
    public string? BookingNote { get; private set; }

    /// <summary>0=Herkese, 1=Sadece Yeni, 2=Sadece Mevcut</summary>
    public int PatientTypeFilter { get; private set; }

    public long? SpecialityId { get; private set; }

    private DoctorOnlineBookingSettings() { }

    public static DoctorOnlineBookingSettings Create(long doctorId, long branchId) =>
        new() { DoctorId = doctorId, BranchId = branchId };

    public void Update(
        bool isOnlineVisible, int slotDurationMinutes, bool autoApprove,
        int maxAdvanceDays, string? bookingNote, int patientTypeFilter, long? specialityId)
    {
        IsOnlineVisible      = isOnlineVisible;
        SlotDurationMinutes  = slotDurationMinutes;
        AutoApprove          = autoApprove;
        MaxAdvanceDays       = maxAdvanceDays;
        BookingNote          = bookingNote;
        PatientTypeFilter    = patientTypeFilter;
        SpecialityId         = specialityId;
    }
}
