using MediatR;
using Microsoft.AspNetCore.Http;
using Oravity.Infrastructure.Audit;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Audit.Application.Commands;

// ─── KVKK Consent ─────────────────────────────────────────────────────────

public record RecordKvkkConsentCommand(
    long    PatientId,
    string  ConsentType,
    bool    IsGiven,
    string? IpAddress
) : IRequest<RecordKvkkConsentResult>;

public record RecordKvkkConsentResult(long Id, string Message);

public class RecordKvkkConsentCommandHandler
    : IRequestHandler<RecordKvkkConsentCommand, RecordKvkkConsentResult>
{
    private readonly AppDbContext  _db;
    private readonly AuditLogService _auditLog;

    public RecordKvkkConsentCommandHandler(AppDbContext db, AuditLogService auditLog)
    {
        _db       = db;
        _auditLog = auditLog;
    }

    public async Task<RecordKvkkConsentResult> Handle(
        RecordKvkkConsentCommand request, CancellationToken cancellationToken)
    {
        var log = KvkkConsentLog.Record(
            request.PatientId,
            request.ConsentType,
            request.IsGiven,
            request.IpAddress);

        _db.KvkkConsentLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync(new AuditLogEntry(
            Action:     request.IsGiven ? "KVKK_CONSENT_GIVEN" : "KVKK_CONSENT_REVOKED",
            EntityType: "Patient",
            EntityId:   request.PatientId.ToString(),
            NewValues:  $"{{\"consentType\":\"{request.ConsentType}\",\"isGiven\":{request.IsGiven.ToString().ToLower()}}}"),
            cancellationToken);

        return new RecordKvkkConsentResult(log.Id, "Onay kaydı oluşturuldu.");
    }
}

// ─── Data Export Request ───────────────────────────────────────────────────

public record CreateDataExportRequestCommand(long PatientId) : IRequest<DataExportRequestResult>;

public record DataExportRequestResult(
    long      Id,
    long      PatientId,
    string    Status,
    DateTime  CreatedAt,
    DateTime? CompletedAt,
    DateTime? ExpiresAt);

public class CreateDataExportRequestCommandHandler
    : IRequestHandler<CreateDataExportRequestCommand, DataExportRequestResult>
{
    private readonly AppDbContext    _db;
    private readonly ICurrentUser    _user;
    private readonly AuditLogService _auditLog;

    public CreateDataExportRequestCommandHandler(
        AppDbContext db, ICurrentUser user, AuditLogService auditLog)
    {
        _db       = db;
        _user     = user;
        _auditLog = auditLog;
    }

    public async Task<DataExportRequestResult> Handle(
        CreateDataExportRequestCommand request, CancellationToken cancellationToken)
    {
        var exportRequest = DataExportRequest.Create(request.PatientId, _user.UserId);
        _db.DataExportRequests.Add(exportRequest);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync(new AuditLogEntry(
            Action:     "EXPORT",
            EntityType: "Patient",
            EntityId:   request.PatientId.ToString(),
            NewValues:  $"{{\"requestId\":{exportRequest.Id}}}"),
            cancellationToken);

        return new DataExportRequestResult(
            exportRequest.Id,
            exportRequest.PatientId,
            exportRequest.Status.ToString(),
            exportRequest.CreatedAt,
            exportRequest.CompletedAt,
            exportRequest.ExpiresAt);
    }
}
