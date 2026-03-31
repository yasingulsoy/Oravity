using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientRecord.Application.Commands;

public record DeletePatientNoteCommand(long PatientId, Guid PublicId) : IRequest;

public class DeletePatientNoteCommandHandler : IRequestHandler<DeletePatientNoteCommand>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly ITenantContext _tenant;

    public DeletePatientNoteCommandHandler(
        AppDbContext db, ICurrentUser user, ITenantContext tenant)
    {
        _db = db;
        _user = user;
        _tenant = tenant;
    }

    public async Task Handle(
        DeletePatientNoteCommand request,
        CancellationToken cancellationToken)
    {
        var note = await _db.PatientNotes
            .FirstOrDefaultAsync(
                n => n.PublicId == request.PublicId &&
                     n.PatientId == request.PatientId &&
                     n.DeletedAt == null,
                cancellationToken)
            ?? throw new NotFoundException($"Not bulunamadı: {request.PublicId}");

        // Şube izolasyonu
        if (_tenant.BranchId.HasValue && note.BranchId != _tenant.BranchId.Value &&
            !_user.HasPermission("platform_admin"))
        {
            throw new ForbiddenException("Bu notu silme yetkiniz bulunmuyor.");
        }

        note.SoftDelete();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
