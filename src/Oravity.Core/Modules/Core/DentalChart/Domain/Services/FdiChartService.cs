namespace Oravity.Core.Modules.Core.DentalChart.Domain.Services;

/// <summary>
/// FDI (Fédération Dentaire Internationale) numaralama sistemi yardımcı servisi.
/// Quadrant: 1=Sağ Üst, 2=Sol Üst, 3=Sol Alt, 4=Sağ Alt
/// Sıra:     1=Merkez Kesici ... 8=Yirmi Yaş Dişi
/// Yetişkin: 11-18, 21-28, 31-38, 41-48 (toplam 32 diş)
/// </summary>
public class FdiChartService : IFdiChartService
{
    // Quadrant → sıra → tooth_number
    private static readonly IReadOnlyList<string> AllTeeth =
    [
        "18","17","16","15","14","13","12","11",  // Sağ Üst
        "21","22","23","24","25","26","27","28",  // Sol Üst
        "31","32","33","34","35","36","37","38",  // Sol Alt
        "41","42","43","44","45","46","47","48"   // Sağ Alt
    ];

    private static readonly HashSet<string> ValidTeeth = new(AllTeeth);

    /// <summary>32 dişin FDI numaralarını quadrant sırasıyla döner.</summary>
    public IReadOnlyList<string> GetAllToothNumbers() => AllTeeth;

    /// <summary>Geçerli FDI numarası mı kontrol eder.</summary>
    public bool IsValidToothNumber(string no) =>
        !string.IsNullOrWhiteSpace(no) && ValidTeeth.Contains(no.Trim());

    /// <summary>Dişin quadrant bilgisini döner.</summary>
    public string GetQuadrant(string no)
    {
        if (!IsValidToothNumber(no))
            throw new ArgumentException($"Geçersiz FDI diş numarası: {no}");

        return no[0] switch
        {
            '1' => "upper_right",
            '2' => "upper_left",
            '3' => "lower_left",
            '4' => "lower_right",
            _   => "unknown"
        };
    }

    /// <summary>Dişin anatomik tipini döner (SPEC §sesli komut bağlamı).</summary>
    public string GetToothType(string no)
    {
        if (!IsValidToothNumber(no))
            throw new ArgumentException($"Geçersiz FDI diş numarası: {no}");

        var position = int.Parse(no[1].ToString());
        return position switch
        {
            1 or 2 => "incisor",    // Kesici
            3      => "canine",     // Köpek
            4 or 5 => "premolar",   // Küçük azı
            6 or 7 => "molar",      // Büyük azı
            8      => "wisdom",     // Yirmi yaş
            _      => "unknown"
        };
    }

    /// <summary>
    /// "MOD", "M,O,D", "M O D" gibi girişleri normalize eder.
    /// Geçerli kodlar: M=Mezyal, D=Distal, O=Oklüzal, V=Vestibül, L=Lingual
    /// </summary>
    public List<string> ParseSurfaces(string surfaces)
    {
        if (string.IsNullOrWhiteSpace(surfaces))
            return [];

        var valid = new HashSet<char> { 'M', 'D', 'O', 'V', 'L' };
        return surfaces
            .ToUpperInvariant()
            .Where(c => valid.Contains(c))
            .Distinct()
            .Select(c => c.ToString())
            .ToList();
    }

    /// <summary>Quadrant bilgisinden Türkçe açıklama döner.</summary>
    public string GetQuadrantLabel(string no)
    {
        return GetQuadrant(no) switch
        {
            "upper_right" => "Sağ Üst",
            "upper_left"  => "Sol Üst",
            "lower_left"  => "Sol Alt",
            "lower_right" => "Sağ Alt",
            _             => "Bilinmiyor"
        };
    }

    /// <summary>
    /// 32 dişi quadrant sırasına göre gruplandırır.
    /// UI tarafında diş şeması render için kullanılır.
    /// </summary>
    public Dictionary<string, IReadOnlyList<string>> GetTeethByQuadrant() =>
        new()
        {
            ["upper_right"] = ["18","17","16","15","14","13","12","11"],
            ["upper_left"]  = ["21","22","23","24","25","26","27","28"],
            ["lower_left"]  = ["31","32","33","34","35","36","37","38"],
            ["lower_right"] = ["41","42","43","44","45","46","47","48"]
        };
}

// ─── Interface ────────────────────────────────────────────────────────────

public interface IFdiChartService
{
    IReadOnlyList<string> GetAllToothNumbers();
    bool IsValidToothNumber(string no);
    string GetQuadrant(string no);
    string GetToothType(string no);
    List<string> ParseSurfaces(string surfaces);
    string GetQuadrantLabel(string no);
    Dictionary<string, IReadOnlyList<string>> GetTeethByQuadrant();
}
