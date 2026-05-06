using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;
using Oravity.SharedKernel.Extensions;

namespace Oravity.Core.Modules.Core.Patient.Application.Queries;

public record GetPatientNotesQuery(Guid PatientPublicId, int? Type = null)
    : IRequest<IReadOnlyList<PatientNoteDto>>;

public class GetPatientNotesQueryHandler
    : IRequestHandler<GetPatientNotesQuery, IReadOnlyList<PatientNoteDto>>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly ITenantContext _tenant;

    public GetPatientNotesQueryHandler(AppDbContext db, ICurrentUser user, ITenantContext tenant)
    {
        _db     = db;
        _user   = user;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<PatientNoteDto>> Handle(
        GetPatientNotesQuery request, CancellationToken ct)
    {
        var canViewHidden = _user.HasPermission("patient:write_hidden_note")
                         || _user.HasPermission("platform_admin");

        var companyId = _tenant.CompanyId;
        var branchId  = _tenant.BranchId;

        var query = _db.PatientNotes
            .AsNoTracking()
            .Include(n => n.CreatedByUser)
            .Where(n => n.Patient.PublicId == request.PatientPublicId && n.DeletedAt == null)
            // Genel Not → şirket geneli; Klinik Not → sadece yazıldığı şube
            .Where(n =>
                (n.Type == NoteType.General  && (companyId == null || n.Branch.CompanyId == companyId)) ||
                (n.Type == NoteType.Clinical && (branchId  == null || n.BranchId == branchId)) ||
                (n.Type != NoteType.General  && n.Type != NoteType.Clinical));

        if (!canViewHidden) query = query.Where(n => !n.IsHidden);
        if (request.Type.HasValue) query = query.Where(n => (int)n.Type == request.Type.Value);

        var rows = await query
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(n => new PatientNoteDto(
            n.PublicId, (int)n.Type, TypeLabel(n.Type),
            n.Title, n.Content, n.IsPinned, n.IsHidden, n.IsAlert,
            n.CreatedBy, n.CreatedByUser?.FullName ?? "", n.CreatedAt, n.NoteUpdatedAt))
            .ToList();
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
