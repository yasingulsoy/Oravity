namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Tüm sistem hareketlerinin değiştirilemez kaydı (SPEC §AUDİT LOG).
/// EF Core AuditInterceptor tarafından otomatik, AuditLogService tarafından
/// manuel olarak oluşturulur. Hiçbir zaman güncellenmez/silinmez.
/// </summary>
public class AuditLog
{
    public long Id { get; private set; }

    public long? CompanyId { get; private set; }
    public long? BranchId { get; private set; }
    public long? UserId { get; private set; }

    /// <summary>Kullanıcı e-posta adresi — kullanıcı silinse bile saklansın.</summary>
    public string? UserEmail { get; private set; }

    /// <summary>
    /// Eylem kodu: CREATE | UPDATE | DELETE | VIEW | LOGIN | LOGOUT | EXPORT | PRINT
    /// </summary>
    public string Action { get; private set; } = default!;

    /// <summary>Etkilenen entity türü: Patient, Appointment, Payment vb.</summary>
    public string? EntityType { get; private set; }

    /// <summary>Etkilenen kaydın public_id veya internal id'si (string).</summary>
    public string? EntityId { get; private set; }

    /// <summary>Değişmeden önceki değerler (JSONB). Sadece UPDATE/DELETE'de dolu.</summary>
    public string? OldValues { get; private set; }

    /// <summary>Değişen yeni değerler (JSONB). CREATE/UPDATE'de dolu.</summary>
    public string? NewValues { get; private set; }

    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string action,
        long? companyId = null,
        long? branchId = null,
        long? userId = null,
        string? userEmail = null,
        string? entityType = null,
        string? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new AuditLog
        {
            Action     = action,
            CompanyId  = companyId,
            BranchId   = branchId,
            UserId     = userId,
            UserEmail  = userEmail,
            EntityType = entityType,
            EntityId   = entityId,
            OldValues  = oldValues,
            NewValues  = newValues,
            IpAddress  = ipAddress,
            UserAgent  = userAgent,
            CreatedAt  = DateTime.UtcNow
        };
    }
}
