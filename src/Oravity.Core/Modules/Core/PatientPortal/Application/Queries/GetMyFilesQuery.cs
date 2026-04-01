using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.PatientPortal.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientPortal.Application.Queries;

public record GetMyFilesQuery(
    PatientFileType? FileType = null
) : IRequest<List<PortalFileItem>>;

public class GetMyFilesQueryHandler
    : IRequestHandler<GetMyFilesQuery, List<PortalFileItem>>
{
    private readonly AppDbContext _db;
    private readonly ICurrentPortalUser _portalUser;

    public GetMyFilesQueryHandler(AppDbContext db, ICurrentPortalUser portalUser)
    {
        _db         = db;
        _portalUser = portalUser;
    }

    public async Task<List<PortalFileItem>> Handle(
        GetMyFilesQuery request,
        CancellationToken cancellationToken)
    {
        var patientId = _portalUser.PatientId;

        var query = _db.PatientFiles
            .AsNoTracking()
            .Where(f => f.PatientId == patientId && f.DeletedAt == null);

        if (request.FileType.HasValue)
            query = query.Where(f => f.FileType == request.FileType.Value);

        var files = await query
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);

        return files.Select(f => new PortalFileItem(
            f.PublicId,
            f.Title,
            (int)f.FileType,
            PatientPortalMappings.FileTypeLabel(f.FileType),
            f.FilePath,
            f.FileExt,
            f.FileSize,
            f.UploadedAt)).ToList();
    }
}
