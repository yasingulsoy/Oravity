using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Patient.Application.Commands;

public record CreatePatientNoteCommand(
    Guid    PatientPublicId,
    int     Type,
    string  Content,
    string? Title    = null,
    bool    IsPinned = false,
    bool    IsAlert  = false
) : IRequest<PatientNoteDto>;

public class CreatePatientNoteCommandHandler
    : IRequestHandler<CreatePatientNoteCommand, PatientNoteDto>
{
    private readonly AppDbContext   _db;
    private readonly ICurrentUser   _user;
    private readonly ITenantContext _tenant;

    public CreatePatientNoteCommandHandler(AppDbContext db, ICurrentUser user, ITenantContext tenant)
    {
        _db     = db;
        _user   = user;
        _tenant = tenant;
    }

    public async Task<PatientNoteDto> Handle(
        CreatePatientNoteCommand request, CancellationToken ct)
    {
        var branchId = _tenant.BranchId
            ?? throw new ForbiddenException("Bu işlem için şube bağlamı gereklidir.");

        var patientId = await _db.Patients
            .Where(p => p.PublicId == request.PatientPublicId)
            .Select(p => (long?)p.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Hasta bulunamadı: {request.PatientPublicId}");

        var noteType = (NoteType)request.Type;

        if (noteType == NoteType.Hidden && !_user.HasPermission("patient:write_hidden_note"))
            throw new ForbiddenException("Gizli not yazma yetkiniz bulunmuyor.");

        var note = PatientNote.Create(
            patientId:  patientId,
            branchId:   branchId,
            type:       noteType,
            content:    request.Content,
            createdBy:  _user.UserId,
            title:      request.Title,
            isPinned:   request.IsPinned,
            isHidden:   noteType == NoteType.Hidden,
            isAlert:    request.IsAlert);

        _db.PatientNotes.Add(note);
        await _db.SaveChangesAsync(ct);

        return new PatientNoteDto(
            note.PublicId, (int)note.Type, TypeLabel(note.Type),
            note.Title, note.Content, note.IsPinned, note.IsHidden, note.IsAlert,
            note.CreatedBy, _user.FullName, note.CreatedAt, note.NoteUpdatedAt);
    }

    private static string TypeLabel(NoteType t) => t switch
    {
        NoteType.General     => "Genel Not",
        NoteType.Clinical    => "Klinik Not",
        NoteType.Hidden      => "Gizli Not",
        NoteType.Plan        => "Plan Notu",
        NoteType.Treatment   => "Tedavi Notu",
        NoteType.Orthodontic => "Ortodonti Notu",
        _                    => t.ToString(),
    };
}
