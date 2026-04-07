namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Randevu tipi: Yeni Hasta, Klinik Hastası, Online, Toplantı, İzin vb.
/// is_patient_appointment=false ise hekim bloğu (öğle molası, toplantı, izin).
/// </summary>
public class AppointmentType
{
    public int Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;

    /// <summary>Takvimde randevu bloğu rengi (hex)</summary>
    public string Color { get; private set; } = "#3598DC";

    /// <summary>true = hasta randevusu, false = hekim bloğu (toplantı, izin, vb.)</summary>
    public bool IsPatientAppointment { get; private set; } = true;

    /// <summary>Varsayılan randevu süresi (dakika)</summary>
    public int DefaultDurationMinutes { get; private set; } = 30;

    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private AppointmentType() { }

    public static AppointmentType Create(
        string name, string code, string color,
        bool isPatientAppointment, int defaultDurationMinutes = 30, int sortOrder = 0) => new()
    {
        Name = name,
        Code = code.ToUpperInvariant(),
        Color = color,
        IsPatientAppointment = isPatientAppointment,
        DefaultDurationMinutes = defaultDurationMinutes,
        SortOrder = sortOrder,
        IsActive = true
    };

    public void Update(string name, string color, int defaultDurationMinutes, int sortOrder)
    {
        Name = name;
        Color = color;
        DefaultDurationMinutes = defaultDurationMinutes;
        SortOrder = sortOrder;
    }

    public void SetActive(bool value) => IsActive = value;
}
