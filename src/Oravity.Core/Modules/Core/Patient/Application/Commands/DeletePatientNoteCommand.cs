using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Patient.Application.Commands;

public record DeletePatientNoteCommand(Guid PatientPublicId, Guid NotePublicId) : IRequest;

public class DeletePatientNoteCommandHandler : IRequestHandler<DeletePatientNoteCommand>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public DeletePatientNoteCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task Handle(DeletePatientNoteCommand request, CancellationToken ct)
    {
        var note = await _db.PatientNotes
            .Include(n => n.Patient)
            .FirstOrDefaultAsync(
                n => n.PublicId == request.NotePublicId
                  && n.Patient.PublicId == request.PatientPublicId
                  && n.DeletedAt == null, ct)
            ?? throw new NotFoundException($"Not bulunamadı: {request.NotePublicId}");

        note.SoftDelete();
        await _db.SaveChangesAsync(ct);
    }
}
