namespace Oravity.SharedKernel.Extensions;

public static class DateTimeExtensions
{
    public static string ToTurkishDate(this DateTime dt)
        => dt.ToString("dd.MM.yyyy");

    public static string ToTurkishDateTime(this DateTime dt)
        => dt.ToString("dd.MM.yyyy HH:mm");

    public static bool IsWorkingHour(this DateTime dt)
        => dt.DayOfWeek != DayOfWeek.Sunday && dt.Hour >= 8 && dt.Hour < 18;
}
