namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hekim gün bazlı online çalışma saatleri (SPEC §ONLİNE RANDEVU SİSTEMİ §2.1).
/// UNIQUE(doctor_id, branch_id, day_of_week) — her hekim+şube+gün için tek kayıt.
/// day_of_week: 1=Pazartesi … 7=Pazar.
/// </summary>
public class DoctorOnlineSchedule
{
    public long Id { get; private set; }

    public long DoctorId { get; private set; }
    public User Doctor { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    /// <summary>1=Pazartesi, 2=Salı, 3=Çarşamba, 4=Perşembe, 5=Cuma, 6=Cumartesi, 7=Pazar</summary>
    public int DayOfWeek { get; private set; }

    public bool IsWorking { get; private set; } = true;

    public TimeOnly StartTime { get; private set; } = new(9, 0);
    public TimeOnly EndTime { get; private set; } = new(18, 0);

    /// <summary>Öğle arası başlangıcı (null ise ara yok).</summary>
    public TimeOnly? BreakStart { get; private set; }
    public TimeOnly? BreakEnd { get; private set; }

    private DoctorOnlineSchedule() { }

    public static DoctorOnlineSchedule Create(long doctorId, long branchId, int dayOfWeek) =>
        new() { DoctorId = doctorId, BranchId = branchId, DayOfWeek = dayOfWeek };

    public void Update(
        bool isWorking, TimeOnly startTime, TimeOnly endTime,
        TimeOnly? breakStart, TimeOnly? breakEnd)
    {
        IsWorking  = isWorking;
        StartTime  = startTime;
        EndTime    = endTime;
        BreakStart = breakStart;
        BreakEnd   = breakEnd;
    }
}
