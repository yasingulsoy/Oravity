namespace Oravity.Infrastructure.Audit;

/// <summary>
/// Manuel audit log talebi için transfer nesnesi.
/// AuditLogService.LogAsync() tarafından tüketilir.
/// </summary>
public record AuditLogEntry(
    string Action,
    long? CompanyId   = null,
    long? BranchId    = null,
    long? UserId      = null,
    string? UserEmail = null,
    string? EntityType = null,
    string? EntityId   = null,
    string? OldValues  = null,
    string? NewValues  = null,
    string? IpAddress  = null,
    string? UserAgent  = null
);
