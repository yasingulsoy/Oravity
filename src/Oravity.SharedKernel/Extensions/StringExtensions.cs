namespace Oravity.SharedKernel.Extensions;

public static class StringExtensions
{
    public static string ToSlug(this string value)
        => value.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("ş", "s").Replace("ğ", "g").Replace("ü", "u")
                .Replace("ö", "o").Replace("ı", "i").Replace("ç", "c")
                .Replace("Ş", "s").Replace("Ğ", "g").Replace("Ü", "u")
                .Replace("Ö", "o").Replace("İ", "i").Replace("Ç", "c");

    public static bool IsNullOrEmpty(this string? value)
        => string.IsNullOrEmpty(value);

    public static string Truncate(this string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";
}
