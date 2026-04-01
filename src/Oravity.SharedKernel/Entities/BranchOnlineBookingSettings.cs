namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Şube bazlı online randevu genel ayarları (SPEC §ONLİNE RANDEVU SİSTEMİ §2.1).
/// branch_id PRIMARY KEY — şube başına tek kayıt.
/// widget_slug UNIQUE — portal URL'sini belirler: portal.disineplus.com/{slug}/randevu
/// </summary>
public class BranchOnlineBookingSettings
{
    /// <summary>branch_id hem PK hem FK.</summary>
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public bool IsEnabled { get; private set; } = true;

    /// <summary>Örn: "hospitadent-pendik" → portal.disineplus.com/hospitadent-pendik</summary>
    public string WidgetSlug { get; private set; } = default!;

    /// <summary>Yeni / mevcut hasta ayrımı widget'ta gösterilsin mi.</summary>
    public bool PatientTypeSplit { get; private set; } = true;

    /// <summary>Widget birincil renk (HEX). Örn: "#2563eb"</summary>
    public string PrimaryColor { get; private set; } = "#2563eb";

    public string? LogoUrl { get; private set; }

    /// <summary>Randevudan kaç saat öncesine kadar iptal edilebilir.</summary>
    public int CancellationHours { get; private set; } = 24;

    public DateTime UpdatedAt { get; private set; }

    private BranchOnlineBookingSettings() { }

    public static BranchOnlineBookingSettings Create(long branchId, string widgetSlug) =>
        new()
        {
            BranchId   = branchId,
            WidgetSlug = widgetSlug,
            UpdatedAt  = DateTime.UtcNow
        };

    public void Update(
        bool isEnabled, bool patientTypeSplit,
        string primaryColor, string? logoUrl, int cancellationHours)
    {
        IsEnabled          = isEnabled;
        PatientTypeSplit   = patientTypeSplit;
        PrimaryColor       = primaryColor;
        LogoUrl            = logoUrl;
        CancellationHours  = cancellationHours;
        UpdatedAt          = DateTime.UtcNow;
    }
}
