using MediatR;
using Oravity.Core.Modules.Core.PatientRecord.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientRecord.Application.Commands;

public record CreatePatientNoteCommand(
    long PatientId,
    NoteType Type,
    string Content,
    string? Title = null,
    bool IsPinned = false,
    long? AppointmentId = null
) : IRequest<PatientNoteResponse>;

public class CreatePatientNoteCommandHandler
    : IRequestHandler<CreatePatientNoteCommand, PatientNoteResponse>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly ITenantContext _tenant;

    public CreatePatientNoteCommandHandler(
        AppDbContext db, ICurrentUser user, ITenantContext tenant)
    {
        _db = db;
        _user = user;
        _tenant = tenant;
    }

    public async Task<PatientNoteResponse> Handle(
        CreatePatientNoteCommand request,
        CancellationToken cancellationToken)
    {
        var branchId = _tenant.BranchId
            ?? throw new ForbiddenException("Bu işlem için şube bağlamı gereklidir.");

        // Gizli not için ayrı yetki kontrolü
        if (request.Type == NoteType.Hidden &&
            !_user.HasPermission("patient:write_hidden_note"))
        {
            throw new ForbiddenException("Gizli not yazma yetkiniz bulunmuyor.");
        }

        var note = PatientNote.Create(
            patientId:     request.PatientId,
            branchId:      branchId,
            type:          request.Type,
            content:       request.Content,
            createdBy:     _user.UserId,
            title:         request.Title,
            isPinned:      request.IsPinned,
            isHidden:      request.Type == NoteType.Hidden,
            appointmentId: request.AppointmentId);

        _db.PatientNotes.Add(note);
        await _db.SaveChangesAsync(cancellationToken);

        return PatientRecordMappings.ToNoteResponse(note);
    }
}
