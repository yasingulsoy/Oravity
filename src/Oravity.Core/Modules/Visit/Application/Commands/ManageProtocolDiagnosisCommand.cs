using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Visit.Application.Commands;

// ─── Add ──────────────────────────────────────────────────────────────────────

public record AddProtocolDiagnosisCommand(
    Guid   ProtocolPublicId,
    long   IcdCodeId,
    bool   IsPrimary,
    string? Note
) : IRequest<ProtocolDiagnosisResponse>;

public class AddProtocolDiagnosisCommandHandler
    : IRequestHandler<AddProtocolDiagnosisCommand, ProtocolDiagnosisResponse>
{
    private readonly AppDbContext _db;

    public AddProtocolDiagnosisCommandHandler(AppDbContext db) => _db = db;

    public async Task<ProtocolDiagnosisResponse> Handle(AddProtocolDiagnosisCommand request, CancellationToken ct)
    {
        var protocol = await _db.Protocols
            .FirstOrDefaultAsync(x => x.PublicId == request.ProtocolPublicId && !x.IsDeleted, ct)
            ?? throw new NotFoundException("Protokol bulunamadı.");

        var icdCode = await _db.IcdCodes
            .FirstOrDefaultAsync(x => x.Id == request.IcdCodeId && x.IsActive, ct)
            ?? throw new NotFoundException("ICD kodu bulunamadı.");

        var entry = protocol.AddIcdDiagnosis(icdCode.Id, icdCode.Code, icdCode.Description, icdCode.Category, request.IsPrimary);
        await _db.SaveChangesAsync(ct);

        return new ProtocolDiagnosisResponse(
            entry.EntryId, icdCode.Id,
            icdCode.Code, icdCode.Description, icdCode.Category,
            entry.IsPrimary, request.Note);
    }
}

// ─── Remove ───────────────────────────────────────────────────────────────────

public record RemoveProtocolDiagnosisCommand(Guid ProtocolPublicId, Guid EntryId) : IRequest;

public class RemoveProtocolDiagnosisCommandHandler : IRequestHandler<RemoveProtocolDiagnosisCommand>
{
    private readonly AppDbContext _db;

    public RemoveProtocolDiagnosisCommandHandler(AppDbContext db) => _db = db;

    public async Task Handle(RemoveProtocolDiagnosisCommand request, CancellationToken ct)
    {
        var protocol = await _db.Protocols
            .FirstOrDefaultAsync(x => x.PublicId == request.ProtocolPublicId && !x.IsDeleted, ct)
            ?? throw new NotFoundException("Protokol bulunamadı.");

        protocol.RemoveIcdDiagnosis(request.EntryId);
        await _db.SaveChangesAsync(ct);
    }
}
