using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Infrastructure.Audit;

/// <summary>
/// Manuel audit log servisi — LOGIN, LOGOUT, EXPORT, PRINT gibi
/// EF Core interceptor'ının yakalayamayacağı olaylar için.
///
/// Fire-and-forget değil; await edilir ama exception yutulur ve
/// loglara düşürülür — ana işlemi bloklamamak için.
/// </summary>
public class AuditLogService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        AppDbContext db,
        ICurrentUser user,
        IHttpContextAccessor http,
        ILogger<AuditLogService> logger)
    {
        _db     = db;
        _user   = user;
        _http   = http;
        _logger = logger;
    }

    /// <summary>
    /// Asenkron log kaydı — hata ana iş akışını engellemez.
    /// </summary>
    public async Task LogAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        try
        {
            var ip        = GetClientIp();
            var userAgent = _http.HttpContext?.Request.Headers["User-Agent"].ToString();

            var log = AuditLog.Create(
                action:     entry.Action,
                companyId:  entry.CompanyId ?? (_user.IsAuthenticated ? _user.TenantId : null),
                branchId:   entry.BranchId  ?? (_user.IsAuthenticated ? _user.BranchId : null),
                userId:     entry.UserId    ?? (_user.IsAuthenticated ? _user.UserId : null),
                userEmail:  entry.UserEmail ?? (_user.IsAuthenticated ? _user.Email : null),
                entityType: entry.EntityType,
                entityId:   entry.EntityId,
                oldValues:  entry.OldValues,
                newValues:  entry.NewValues,
                ipAddress:  entry.IpAddress ?? ip,
                userAgent:  entry.UserAgent ?? userAgent);

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Audit hatası ana işlemi engellemez; sadece loglara düşer
            _logger.LogError(ex,
                "AuditLogService: Log kaydedilemedi — Action={Action} Entity={EntityType}",
                entry.Action, entry.EntityType);
        }
    }

    private string? GetClientIp()
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return null;

        // X-Forwarded-For (load balancer / proxy arkasında)
        var forwarded = ctx.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',').First().Trim();

        return ctx.Connection.RemoteIpAddress?.ToString();
    }
}
