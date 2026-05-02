namespace Oravity.Core.Modules.Visit.Application;

// ─── Visit ────────────────────────────────────────────────────────────────

public record WaitingListItemResponse(
    Guid     PublicId,
    long     PatientId,
    string   PatientName,
    string?  Phone,
    DateTime CheckInAt,
    bool     IsWalkIn,
    int      Status,
    string   StatusLabel,
    string?  AppointmentTime,
    bool     HasOpenProtocol,
    int      WaitingMinutes,
    long?    AppointmentDoctorId,
    int?     AppointmentSpecializationId,
    DateOnly? PatientBirthDate,
    string?  PatientGender,
    bool     IsBeingCalled,
    string?  BranchName,
    IReadOnlyList<WaitingProtocolItem> Protocols
);

public record WaitingProtocolItem(
    Guid      PublicId,
    string    ProtocolNo,
    int       ProtocolTypeId,
    string    TypeName,
    string    TypeColor,
    int       Status,
    string    StatusName,
    string    DoctorName,
    string?   Diagnosis,
    DateTime? StartedAt
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

public record ProtocolDiagnosisResponse(
    Guid    PublicId,
    long    IcdCodeId,
    string  Code,
    string  Description,
    string  Category,
    bool    IsPrimary,
    string? Note
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
    string?  ExaminationFindings,
    string?  Diagnosis,
    string?  TreatmentPlan,
    string?  Notes,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    IReadOnlyList<ProtocolDiagnosisResponse> Diagnoses
);

public record IcdCodeResponse(
    long   Id,
    string Code,
    string Description,
    string Category,
    int    Type
);

public record DoctorProtocolResponse(
    Guid    PublicId,
    string  ProtocolNo,
    long    PatientId,
    Guid    PatientPublicId,
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
