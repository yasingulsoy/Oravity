using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.PatientRecord.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Core.PatientRecord.Application.Queries;

public record GetPatientFilesQuery(
    long PatientId,
    PatientFileType? TypeFilter = null
) : IRequest<IReadOnlyList<PatientFileResponse>>;

public class GetPatientFilesQueryHandler
    : IRequestHandler<GetPatientFilesQuery, IReadOnlyList<PatientFileResponse>>
{
    private readonly AppDbContext _db;

    public GetPatientFilesQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PatientFileResponse>> Handle(
        GetPatientFilesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.PatientFiles
            .AsNoTracking()
            .Where(f =>
                f.PatientId == request.PatientId &&
                f.DeletedAt == null);

        if (request.TypeFilter.HasValue)
            query = query.Where(f => f.FileType == request.TypeFilter.Value);

        return await query
            .OrderByDescending(f => f.UploadedAt)
            .Select(f => PatientRecordMappings.ToFileResponse(f))
            .ToListAsync(cancellationToken);
    }
}
