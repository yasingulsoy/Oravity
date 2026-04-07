namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hekim tek seferlik özel gün (genel takvimi override eder).
/// Öncelik sırası: Özel gün kaydı varsa genel takvim görmezden gelinir.
/// UNIQUE (doctor_id, branch_id, specific_date)
/// </summary>
public class DoctorSpecialDay
{
    public long Id { get; private set; }

    public long DoctorId { get; private set; }
    public User Doctor { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public DateOnly SpecificDate { get; private set; }

    /// <summary>Null ise o gün tamamen kapalı (izin)</summary>
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }

    /// <summary>Kongre, Yıllık İzin, Ekstra Mesai vb.</summary>
    public string? Reason { get; private set; }

    public DoctorSpecialDayType Type { get; private set; } = DoctorSpecialDayType.ExtraWork;

    public bool IsActive { get; private set; } = true;

    private DoctorSpecialDay() { }

    public static DoctorSpecialDay Create(
        long doctorId, long branchId, DateOnly specificDate,
        DoctorSpecialDayType type, TimeOnly? startTime, TimeOnly? endTime, string? reason) => new()
    {
        DoctorId     = doctorId,
        BranchId     = branchId,
        SpecificDate = specificDate,
        Type         = type,
        StartTime    = startTime,
        EndTime      = endTime,
        Reason       = reason,
        IsActive     = true
    };

    public void Update(DoctorSpecialDayType type, TimeOnly? startTime, TimeOnly? endTime, string? reason)
    {
        Type      = type;
        StartTime = startTime;
        EndTime   = endTime;
        Reason    = reason;
    }

    public void SetActive(bool value) => IsActive = value;
}

public enum DoctorSpecialDayType
{
    /// <summary>Normalde olmadığı güne ekstra geliş</summary>
    ExtraWork = 1,
    /// <summary>Genel takvimi o tarih için override (farklı saat)</summary>
    HourChange = 2,
    /// <summary>O gün tamamen kapalı (izin, start/end_time null)</summary>
    DayOff = 3
}
