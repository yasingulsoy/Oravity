namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Randevu durumu — takvimde blok rengi bu tablodan gelir.
/// title_color: header rengi, container_color: arka plan, border_color: kenar, text_color: yazı.
/// Geçiş kuralları: AllowedTransitions seed verisiyle birlikte yüklenir.
/// </summary>
public class AppointmentStatus
{
    public int Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;

    /// <summary>Takvim bloğu renkleri (hex)</summary>
    public string TitleColor { get; private set; } = "#3598DC";
    public string ContainerColor { get; private set; } = "#4c4cff";
    public string BorderColor { get; private set; } = "#3333ff";
    public string TextColor { get; private set; } = "#ffffff";

    /// <summary>CSS sınıfı: cl-white, cl-black, cl-purple</summary>
    public string ClassName { get; private set; } = "cl-white";

    /// <summary>Bu durum hasta randevusu mu, yoksa hekim bloğu mu?</summary>
    public bool IsPatientStatus { get; private set; } = true;

    /// <summary>Bu durumdan geçilebilecek durum ID'leri (JSON array)</summary>
    public string AllowedNextStatusIds { get; private set; } = "[]";

    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Seed verisindeki sabit ID'ler. DB filter ve LINQ sorgularında kullanılır.
    /// Seed sırası: 1-7 ardışık, 8-9 rezerve, 10=NO_SHOW.
    /// Terminal'lar: LEFT(4), CANCELLED(6), NO_SHOW(10).
    /// </summary>
    public static class WellKnownIds
    {
        public const int Planned   = 1;
        public const int Confirmed = 2;
        public const int Arrived   = 3;
        public const int Left      = 4;  // Terminal: hasta kliniği terk etti
        public const int InRoom    = 5;
        public const int Cancelled = 6;  // Terminal
        public const int Completed = 7;
        public const int NoShow    = 8;  // Terminal
    }

    private AppointmentStatus() { }

    public static AppointmentStatus Create(
        string name, string code,
        string titleColor, string containerColor, string borderColor, string textColor,
        string className, bool isPatientStatus, int sortOrder) => new()
    {
        Name = name,
        Code = code.ToUpperInvariant(),
        TitleColor = titleColor,
        ContainerColor = containerColor,
        BorderColor = borderColor,
        TextColor = textColor,
        ClassName = className,
        IsPatientStatus = isPatientStatus,
        SortOrder = sortOrder,
        IsActive = true,
        AllowedNextStatusIds = "[]"
    };

    public void UpdateColors(string titleColor, string containerColor, string borderColor, string textColor, string className)
    {
        TitleColor = titleColor;
        ContainerColor = containerColor;
        BorderColor = borderColor;
        TextColor = textColor;
        ClassName = className;
    }

    public void SetAllowedNextStatusIds(string json) => AllowedNextStatusIds = json;
    public void SetActive(bool value) => IsActive = value;
}
