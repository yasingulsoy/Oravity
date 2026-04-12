using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Core.Modules.Visit.Application.Queries;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Visit.Application.Commands;

public record UpdateProtocolDetailsCommand(
    Guid    PublicId,
    string? ChiefComplaint,
    string? ExaminationFindings,
    string? Diagnosis,
    string? TreatmentPlan,
    string? Notes
) : IRequest<ProtocolDetailResponse>;

public class UpdateProtocolDetailsCommandHandler
    : IRequestHandler<UpdateProtocolDetailsCommand, ProtocolDetailResponse>
{
    private readonly AppDbContext _db;

    public UpdateProtocolDetailsCommandHandler(AppDbContext db) => _db = db;

    public async Task<ProtocolDetailResponse> Handle(UpdateProtocolDetailsCommand request, CancellationToken ct)
    {
        var p = await _db.Protocols
            .FirstOrDefaultAsync(x => x.PublicId == request.PublicId && !x.IsDeleted, ct)
            ?? throw new NotFoundException("Protokol bulunamadı.");

        p.UpdateDetails(
            request.ChiefComplaint,
            request.ExaminationFindings,
            request.Diagnosis,
            request.TreatmentPlan,
            request.Notes);

        await _db.SaveChangesAsync(ct);

        return await new GetProtocolDetailQueryHandler(_db)
            .Handle(new GetProtocolDetailQuery(request.PublicId), ct);
    }
}
