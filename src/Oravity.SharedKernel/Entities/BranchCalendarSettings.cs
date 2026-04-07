namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Şube takvim görünüm ayarları.
/// branch_id PRIMARY KEY — şube başına tek kayıt.
/// </summary>
public class BranchCalendarSettings
{
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    /// <summary>Takvim slot aralığı (dakika). Geçerli değerler: 10, 15, 20, 30, 60.</summary>
    public int SlotIntervalMinutes { get; private set; } = 30;

    /// <summary>Günlük görünüm başlangıç saati (saat). Varsayılan 8.</summary>
    public int DayStartHour { get; private set; } = 8;

    /// <summary>Günlük görünüm bitiş saati (saat). Varsayılan 20.</summary>
    public int DayEndHour { get; private set; } = 20;

    public DateTime UpdatedAt { get; private set; }

    private BranchCalendarSettings() { }

    public static BranchCalendarSettings Create(long branchId) => new()
    {
        BranchId  = branchId,
        UpdatedAt = DateTime.UtcNow,
    };

    public void Update(int slotIntervalMinutes, int dayStartHour, int dayEndHour)
    {
        SlotIntervalMinutes = slotIntervalMinutes;
        DayStartHour        = dayStartHour;
        DayEndHour          = dayEndHour;
        UpdatedAt           = DateTime.UtcNow;
    }
}
