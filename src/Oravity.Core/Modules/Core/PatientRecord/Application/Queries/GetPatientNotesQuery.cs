using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.PatientRecord.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientRecord.Application.Queries;

public record GetPatientNotesQuery(
    long PatientId,
    NoteType? TypeFilter = null
) : IRequest<IReadOnlyList<PatientNoteResponse>>;

public class GetPatientNotesQueryHandler
    : IRequestHandler<GetPatientNotesQuery, IReadOnlyList<PatientNoteResponse>>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public GetPatientNotesQueryHandler(AppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<IReadOnlyList<PatientNoteResponse>> Handle(
        GetPatientNotesQuery request,
        CancellationToken cancellationToken)
    {
        var canViewHidden = _user.HasPermission("patient:write_hidden_note") ||
                            _user.HasPermission("platform_admin");

        var query = _db.PatientNotes
            .AsNoTracking()
            .Where(n =>
                n.PatientId == request.PatientId &&
                n.DeletedAt == null);

        // Gizli notları yalnızca yetkili kullanıcılar görebilir
        if (!canViewHidden)
            query = query.Where(n => !n.IsHidden);

        if (request.TypeFilter.HasValue)
            query = query.Where(n => n.Type == request.TypeFilter.Value);

        // Pinlenmiş önce, sonra oluşturma tarihi azalan
        var notes = await query
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .Select(n => PatientRecordMappings.ToNoteResponse(n))
            .ToListAsync(cancellationToken);

        return notes;
    }
}
