namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hekim nöbet ayarları. Nöbet günleri takvimde ayrı renkte gösterilir.
/// UNIQUE (doctor_id, branch_id)
/// </summary>
public class DoctorOnCallSettings
{
    public long Id { get; private set; }

    public long DoctorId { get; private set; }
    public User Doctor { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    // ─── Nöbet günleri ────────────────────────────────────────────────────
    public bool Monday    { get; private set; }
    public bool Tuesday   { get; private set; }
    public bool Wednesday { get; private set; }
    public bool Thursday  { get; private set; }
    public bool Friday    { get; private set; }
    public bool Saturday  { get; private set; }
    public bool Sunday    { get; private set; }

    public OnCallPeriodType PeriodType { get; private set; } = OnCallPeriodType.Monthly;
    public DateOnly? PeriodStart { get; private set; }
    public DateOnly? PeriodEnd { get; private set; }

    public bool IsActive { get; private set; } = true;

    private DoctorOnCallSettings() { }

    public static DoctorOnCallSettings Create(long doctorId, long branchId) => new()
    {
        DoctorId = doctorId,
        BranchId = branchId,
        IsActive = true
    };

    public void Update(
        bool monday, bool tuesday, bool wednesday, bool thursday,
        bool friday, bool saturday, bool sunday,
        OnCallPeriodType periodType, DateOnly? periodStart, DateOnly? periodEnd)
    {
        Monday    = monday;
        Tuesday   = tuesday;
        Wednesday = wednesday;
        Thursday  = thursday;
        Friday    = friday;
        Saturday  = saturday;
        Sunday    = sunday;
        PeriodType  = periodType;
        PeriodStart = periodStart;
        PeriodEnd   = periodEnd;
    }

    public void SetActive(bool value) => IsActive = value;
}

public enum OnCallPeriodType
{
    Weekly    = 1,
    Monthly   = 2,
    Quarterly = 3,
    SixMonths = 4
}
