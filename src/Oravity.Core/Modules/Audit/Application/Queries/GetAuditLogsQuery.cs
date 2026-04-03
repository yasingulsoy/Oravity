using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Audit.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Audit.Application.Queries;

public record GetAuditLogsQuery(
    string? EntityType  = null,
    string? EntityId    = null,
    long?   UserId      = null,
    string? Action      = null,
    DateTime? From      = null,
    DateTime? To        = null,
    int Page            = 1,
    int PageSize        = 50
) : IRequest<AuditLogPagedResult>;

public class GetAuditLogsQueryHandler
    : IRequestHandler<GetAuditLogsQuery, AuditLogPagedResult>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetAuditLogsQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<AuditLogPagedResult> Handle(
        GetAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenant.CompanyId
            ?? throw new UnauthorizedAccessException("Şirket bağlamı bulunamadı.");

        var query = _db.AuditLogs
            .Where(l => l.CompanyId == companyId || l.CompanyId == null);

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(l => l.EntityType == request.EntityType);

        if (!string.IsNullOrWhiteSpace(request.EntityId))
            query = query.Where(l => l.EntityId == request.EntityId);

        if (request.UserId.HasValue)
            query = query.Where(l => l.UserId == request.UserId.Value);

        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(l => l.Action == request.Action);

        if (request.From.HasValue)
            query = query.Where(l => l.CreatedAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(l => l.CreatedAt <= request.To.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new AuditLogResponse(
                l.Id, l.CompanyId, l.BranchId, l.UserId, l.UserEmail,
                l.Action, l.EntityType, l.EntityId,
                l.OldValues, l.NewValues, l.IpAddress, l.CreatedAt))
            .ToListAsync(cancellationToken);

        return new AuditLogPagedResult(items, total, request.Page, request.PageSize);
    }
}

// ── Hasta bazlı audit log query ──────────────────────────────────────────────

public record GetPatientAuditLogsQuery(
    string PatientPublicId,
    int Page     = 1,
    int PageSize = 50
) : IRequest<AuditLogPagedResult>;

public class GetPatientAuditLogsQueryHandler
    : IRequestHandler<GetPatientAuditLogsQuery, AuditLogPagedResult>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetPatientAuditLogsQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<AuditLogPagedResult> Handle(
        GetPatientAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.AuditLogs
            .Where(l => l.EntityType == "Patient" &&
                        l.EntityId   == request.PatientPublicId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new AuditLogResponse(
                l.Id, l.CompanyId, l.BranchId, l.UserId, l.UserEmail,
                l.Action, l.EntityType, l.EntityId,
                l.OldValues, l.NewValues, l.IpAddress, l.CreatedAt))
            .ToListAsync(cancellationToken);

        return new AuditLogPagedResult(items, total, request.Page, request.PageSize);
    }
}
