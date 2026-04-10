namespace Oravity.Core.Modules.Visit.Application;

// ─── Visit ────────────────────────────────────────────────────────────────

public record WaitingListItemResponse(
    Guid    PublicId,
    long    PatientId,
    string  PatientName,
    string? Phone,
    DateTime CheckInAt,
    bool    IsWalkIn,
    int     Status,
    string  StatusLabel,
    string? AppointmentTime,  // HH:mm formatında randevu saati, null=walk-in
    bool    HasOpenProtocol,
    int     WaitingMinutes,
    long?   AppointmentDoctorId,      // Randevuyu alan hekim (walk-in=null)
    int?    AppointmentSpecializationId // Randevunun uzmanlığı (walk-in=null)
);

public record VisitResponse(
    Guid     PublicId,
    long     PatientId,
    string   PatientName,
    long     BranchId,
    DateTime CheckInAt,
    DateTime? CheckOutAt,
    bool     IsWalkIn,
    int      Status,
    string   StatusLabel,
    string?  Notes,
    IReadOnlyList<ProtocolSummaryResponse> Protocols
);

// ─── Protocol ─────────────────────────────────────────────────────────────

public record ProtocolSummaryResponse(
    Guid    PublicId,
    string  ProtocolNo,
    int     ProtocolType,
    string  ProtocolTypeName,
    int     Status,
    string  StatusName,
    long    DoctorId,
    string  DoctorName,
    DateTime? StartedAt,
    DateTime? CompletedAt
);

public record ProtocolDetailResponse(
    Guid     PublicId,
    string   ProtocolNo,
    long     VisitId,
    long     PatientId,
    string   PatientName,
    long     DoctorId,
    string   DoctorName,
    long     BranchId,
    int      ProtocolType,
    string   ProtocolTypeName,
    int      Status,
    string   StatusName,
    string?  ChiefComplaint,
    string?  Diagnosis,
    string?  Notes,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt
);

public record DoctorProtocolResponse(
    Guid    PublicId,
    string  ProtocolNo,
    long    PatientId,
    string  PatientName,
    string? Phone,
    int     ProtocolType,
    string  ProtocolTypeName,
    int     Status,
    string  StatusName,
    DateTime? StartedAt
);

// ─── Helpers ──────────────────────────────────────────────────────────────

public static class VisitLabels
{
    public static string VisitStatus(int status) => status switch
    {
        1 => "Bekliyor",
        2 => "Protokol Açıldı",
        3 => "Tamamlandı",
        4 => "İptal",
        _ => "Bilinmiyor"
    };

    public static string ProtocolStatus(int status) => status switch
    {
        1 => "Açık",
        2 => "Tamamlandı",
        3 => "İptal",
        _ => "Bilinmiyor"
    };

    public static string ProtocolType(int type) => type switch
    {
        1 => "Muayene",
        2 => "Tedavi",
        3 => "Konsültasyon",
        4 => "Kontrol",
        5 => "Acil",
        _ => "Diğer"
    };
}
