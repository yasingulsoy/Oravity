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

        // Ana tanı tekli olsun — varsa diğerini secondary'ye al
        if (request.IsPrimary)
        {
            var existing = await _db.ProtocolDiagnoses
                .Where(d => d.ProtocolId == protocol.Id && d.IsPrimary && !d.IsDeleted)
                .ToListAsync(ct);
            foreach (var e in existing) e.SetPrimary(false);
        }

        var diagnosis = ProtocolDiagnosis.Create(protocol.Id, icdCode.Id, request.IsPrimary, request.Note);
        _db.ProtocolDiagnoses.Add(diagnosis);
        await _db.SaveChangesAsync(ct);

        return new ProtocolDiagnosisResponse(
            diagnosis.PublicId, icdCode.Id,
            icdCode.Code, icdCode.Description, icdCode.Category,
            diagnosis.IsPrimary, diagnosis.Note);
    }
}

// ─── Remove ───────────────────────────────────────────────────────────────────

public record RemoveProtocolDiagnosisCommand(Guid DiagnosisPublicId) : IRequest;

public class RemoveProtocolDiagnosisCommandHandler : IRequestHandler<RemoveProtocolDiagnosisCommand>
{
    private readonly AppDbContext _db;

    public RemoveProtocolDiagnosisCommandHandler(AppDbContext db) => _db = db;

    public async Task Handle(RemoveProtocolDiagnosisCommand request, CancellationToken ct)
    {
        var d = await _db.ProtocolDiagnoses
            .FirstOrDefaultAsync(x => x.PublicId == request.DiagnosisPublicId && !x.IsDeleted, ct)
            ?? throw new NotFoundException("Tanı bulunamadı.");

        d.SoftDelete();
        await _db.SaveChangesAsync(ct);
    }
}
