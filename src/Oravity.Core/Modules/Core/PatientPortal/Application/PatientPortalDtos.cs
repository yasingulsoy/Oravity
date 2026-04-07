using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Core.PatientPortal.Application;

public record PortalAccountResponse(
    Guid PublicId,
    string Email,
    string Phone,
    bool IsEmailVerified,
    bool IsPhoneVerified,
    bool IsActive,
    string PreferredLanguageCode,
    long? PatientId,
    DateTime? LastLoginAt
);

public record PortalLoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    PortalAccountResponse Account
);

public record PortalAppointmentItem(
    Guid PublicId,
    DateTime StartTime,
    DateTime EndTime,
    string DoctorName,
    int Status,
    string StatusLabel,
    bool IsFuture
);

public record PortalPaymentItem(
    Guid PublicId,
    decimal Amount,
    string Currency,
    int PaymentMethod,
    string PaymentMethodLabel,
    DateOnly PaymentDate,
    string? Notes
);

public record PortalBalanceResponse(
    decimal TotalTreatmentAmount,
    decimal TotalPaid,
    decimal Balance,
    IReadOnlyList<PortalPaymentItem> Payments
);

public record PortalTreatmentPlanItem(
    Guid PublicId,
    string Name,
    int Status,
    string StatusLabel,
    decimal TotalAmount,
    int ItemCount
);

public record PortalFileItem(
    Guid PublicId,
    string? Title,
    int FileType,
    string FileTypeLabel,
    string FilePath,
    string? FileExt,
    int? FileSizeBytes,
    DateTime UploadedAt
);

public static class PatientPortalMappings
{
    public static PortalAccountResponse ToResponse(PatientPortalAccount a) => new(
        a.PublicId, a.Email, a.Phone,
        a.IsEmailVerified, a.IsPhoneVerified,
        a.IsActive, a.PreferredLanguageCode,
        a.PatientId, a.LastLoginAt);

    public static string AppointmentStatusLabel(int statusId) => statusId switch
    {
        AppointmentStatus.WellKnownIds.Planned   => "Planlandı",
        AppointmentStatus.WellKnownIds.Confirmed => "Onaylandı",
        AppointmentStatus.WellKnownIds.Arrived   => "Geldi",
        AppointmentStatus.WellKnownIds.InRoom    => "Odaya Alındı",
        AppointmentStatus.WellKnownIds.Left      => "Ayrıldı",
        AppointmentStatus.WellKnownIds.Completed => "Tamamlandı",
        AppointmentStatus.WellKnownIds.Cancelled => "İptal",
        AppointmentStatus.WellKnownIds.NoShow    => "Gelmedi",
        _                                        => statusId.ToString()
    };

    public static string TreatmentStatusLabel(int s) => s switch
    {
        1 => "Taslak",
        2 => "Onaylandı",
        3 => "Tamamlandı",
        4 => "İptal",
        _ => s.ToString()
    };

    public static string FileTypeLabel(PatientFileType t) => t switch
    {
        PatientFileType.XRay          => "Röntgen",
        PatientFileType.Photo         => "Fotoğraf",
        PatientFileType.Orthodontic   => "Ortodonti",
        PatientFileType.MedicalReport => "Medikal Rapor",
        PatientFileType.Prescription  => "Reçete",
        PatientFileType.Consent       => "ONAM",
        PatientFileType.Document      => "Doküman",
        _                             => t.ToString()
    };

    public static string PaymentMethodLabel(int m) => m switch
    {
        1 => "Nakit",
        2 => "Kredi Kartı",
        3 => "Havale/EFT",
        4 => "Taksit",
        5 => "Çek",
        _ => m.ToString()
    };
}
