namespace Oravity.Core.Modules.Audit.Application;

public record AuditLogResponse(
    long Id,
    long? CompanyId,
    long? BranchId,
    long? UserId,
    string? UserEmail,
    string Action,
    string? EntityType,
    string? EntityId,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    DateTime CreatedAt
);

public record AuditLogPagedResult(
    IReadOnlyList<AuditLogResponse> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record KvkkConsentRequest(
    long PatientId,
    string ConsentType,
    bool IsGiven,
    string? IpAddress = null
);

public record DataExportRequestResponse(
    long Id,
    long PatientId,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    DateTime? ExpiresAt
);
