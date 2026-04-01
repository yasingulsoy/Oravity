using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Survey.Application.Commands;

public record AddComplaintNoteCommand(
    Guid ComplaintPublicId,
    string Note,
    bool IsInternal = true
) : IRequest<long>;

public class AddComplaintNoteCommandHandler
    : IRequestHandler<AddComplaintNoteCommand, long>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public AddComplaintNoteCommandHandler(AppDbContext db, ICurrentUser user)
    {
        _db   = db;
        _user = user;
    }

    public async Task<long> Handle(
        AddComplaintNoteCommand request,
        CancellationToken cancellationToken)
    {
        var complaint = await _db.Complaints
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PublicId == request.ComplaintPublicId, cancellationToken)
            ?? throw new NotFoundException($"Şikayet bulunamadı: {request.ComplaintPublicId}");

        var note = ComplaintNote.Create(complaint.Id, request.Note, _user.UserId, request.IsInternal);
        _db.ComplaintNotes.Add(note);
        await _db.SaveChangesAsync(cancellationToken);

        return note.Id;
    }
}
