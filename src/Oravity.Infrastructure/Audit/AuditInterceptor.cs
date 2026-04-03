using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Infrastructure.Audit;

/// <summary>
/// EF Core SaveChanges interceptor — entity değişikliklerini otomatik audit-log'a yazar.
///
/// Kapsam:
///   Added    → action='CREATE', new_values dolu
///   Modified → action='UPDATE', old_values + new_values dolu
///   Deleted/SoftDeleted → action='DELETE'
///
/// Hassas alan maskeleme:
///   PasswordHash, TcNumber, TotpSecret → "***"
///
/// Hariç tutulan entity'ler:
///   AuditLog, OutboxMessage, SmsQueue — sonsuz döngüyü önler
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<AuditInterceptor> _logger;

    private static readonly IReadOnlySet<string> ExcludedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        nameof(AuditLog),
        nameof(OutboxMessage),
        nameof(SmsQueue),
        "LoginAttempt"
    };

    private static readonly IReadOnlySet<string> SensitiveFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash",
        "TcNumber",
        "TotpSecret",
        "Token",
        "EmailVerificationToken",
        "PhoneVerificationCode",
        "VerificationCode",
        "TokenHash"
    };

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented        = false
    };

    public AuditInterceptor(
        ICurrentUser currentUser,
        IHttpContextAccessor http,
        ILogger<AuditInterceptor> logger)
    {
        _currentUser = currentUser;
        _http        = http;
        _logger      = logger;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is null)
            return await base.SavingChangesAsync(eventData, result, ct);

        var entries = CollectAuditEntries(eventData.Context);
        // Entries'i context'e eklemeden önce yakala;
        // SaveChangesCompleted'a erteleyerek generated PK'ları da alabiliriz.
        // Ancak sadelik için burada kayıt yaparız — PK henüz atanmamış olabilir.
        if (entries.Count > 0)
        {
            var db = eventData.Context;
            foreach (var log in entries)
                db.Set<AuditLog>().Add(log);
        }

        return await base.SavingChangesAsync(eventData, result, ct);
    }

    // ── Yardımcı metotlar ─────────────────────────────────────────────────────

    private List<AuditLog> CollectAuditEntries(DbContext context)
    {
        var logs      = new List<AuditLog>();
        var ipAddress = GetClientIp();
        var userAgent = _http.HttpContext?.Request.Headers["User-Agent"].ToString();

        var userId    = _currentUser.IsAuthenticated ? (long?)_currentUser.UserId  : null;
        var email     = _currentUser.IsAuthenticated ? _currentUser.Email : null;
        var companyId = _currentUser.IsAuthenticated ? (long?)_currentUser.TenantId : null;
        var branchId  = _currentUser.IsAuthenticated ? _currentUser.BranchId : null;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            var typeName = entry.Entity.GetType().Name;
            if (ExcludedTypes.Contains(typeName)) continue;

            string? action = entry.State switch
            {
                EntityState.Added    => "CREATE",
                EntityState.Modified => "UPDATE",
                EntityState.Deleted  => "DELETE",
                _                    => null
            };

            if (action is null) continue;

            // Soft-delete tespiti: IsDeleted=true olan Modified → DELETE olarak logla
            if (action == "UPDATE" && IsSoftDelete(entry))
                action = "DELETE";

            string? entityId = null;
            try
            {
                var pk = entry.Metadata.FindPrimaryKey();
                if (pk != null)
                {
                    var pkVal = pk.Properties
                        .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
                        .FirstOrDefault();
                    entityId = pkVal;
                }
            }
            catch { /* PK henüz atanamadıysa sessizce geç */ }

            string? oldValues = null;
            string? newValues = null;

            if (action == "CREATE")
            {
                newValues = SerializeProperties(entry.CurrentValues.Properties
                    .ToDictionary(p => p.Name,
                                  p => MaskIfSensitive(p.Name, entry.CurrentValues[p])));
            }
            else if (action == "UPDATE")
            {
                var changed = entry.Properties
                    .Where(p => p.IsModified)
                    .ToList();

                if (changed.Count == 0) continue;

                oldValues = SerializeProperties(changed
                    .ToDictionary(p => p.Metadata.Name,
                                  p => MaskIfSensitive(p.Metadata.Name, p.OriginalValue)));

                newValues = SerializeProperties(changed
                    .ToDictionary(p => p.Metadata.Name,
                                  p => MaskIfSensitive(p.Metadata.Name, p.CurrentValue)));
            }
            else // DELETE
            {
                oldValues = SerializeProperties(entry.CurrentValues.Properties
                    .ToDictionary(p => p.Name,
                                  p => MaskIfSensitive(p.Name, entry.CurrentValues[p])));
            }

            logs.Add(AuditLog.Create(
                action:     action,
                companyId:  companyId,
                branchId:   branchId,
                userId:     userId,
                userEmail:  email,
                entityType: typeName,
                entityId:   entityId,
                oldValues:  oldValues,
                newValues:  newValues,
                ipAddress:  ipAddress,
                userAgent:  userAgent));
        }

        return logs;
    }

    private static bool IsSoftDelete(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var isDeletedProp = entry.Properties
            .FirstOrDefault(p => p.Metadata.Name == "IsDeleted");
        return isDeletedProp is not null && isDeletedProp.IsModified &&
               isDeletedProp.CurrentValue is true;
    }

    private static object? MaskIfSensitive(string propertyName, object? value)
    {
        if (value is null) return null;
        return SensitiveFields.Contains(propertyName) ? "***" : value;
    }

    private static string SerializeProperties(Dictionary<string, object?> props)
    {
        try { return JsonSerializer.Serialize(props, JsonOpts); }
        catch { return "{}"; }
    }

    private string? GetClientIp()
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return null;

        var forwarded = ctx.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',').First().Trim();

        return ctx.Connection.RemoteIpAddress?.ToString();
    }
}
