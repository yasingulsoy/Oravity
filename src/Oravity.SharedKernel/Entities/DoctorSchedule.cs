namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hekim haftalık tekrarlayan çalışma takvimi (iç kullanım).
/// Aynı hekim aynı şubede aynı gün için birden fazla kayıt olamaz.
/// UNIQUE (doctor_id, branch_id, day_of_week)
/// </summary>
public class DoctorSchedule
{
    public long Id { get; private set; }

    public long DoctorId { get; private set; }
    public User Doctor { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    /// <summary>1=Pazartesi … 7=Pazar</summary>
    public int DayOfWeek { get; private set; }

    public bool IsWorking { get; private set; } = true;

    public TimeOnly StartTime { get; private set; } = new(9, 0);
    public TimeOnly EndTime { get; private set; } = new(18, 0);

    /// <summary>Öğle arası başlangıcı (null = ara yok)</summary>
    public TimeOnly? BreakStart { get; private set; }
    public TimeOnly? BreakEnd { get; private set; }

    public bool IsActive { get; private set; } = true;

    private DoctorSchedule() { }

    public static DoctorSchedule Create(long doctorId, long branchId, int dayOfWeek) => new()
    {
        DoctorId  = doctorId,
        BranchId  = branchId,
        DayOfWeek = dayOfWeek,
        IsWorking = true
    };

    public void Update(bool isWorking, TimeOnly startTime, TimeOnly endTime, TimeOnly? breakStart, TimeOnly? breakEnd)
    {
        IsWorking  = isWorking;
        StartTime  = startTime;
        EndTime    = endTime;
        BreakStart = breakStart;
        BreakEnd   = breakEnd;
    }

    public void SetActive(bool value) => IsActive = value;
}
