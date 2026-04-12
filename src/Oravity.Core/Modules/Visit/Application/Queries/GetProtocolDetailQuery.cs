using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Visit.Application.Queries;

public record GetProtocolDetailQuery(Guid PublicId) : IRequest<ProtocolDetailResponse>;

public class GetProtocolDetailQueryHandler : IRequestHandler<GetProtocolDetailQuery, ProtocolDetailResponse>
{
    private readonly AppDbContext _db;

    public GetProtocolDetailQueryHandler(AppDbContext db) => _db = db;

    public async Task<ProtocolDetailResponse> Handle(GetProtocolDetailQuery request, CancellationToken ct)
    {
        var p = await _db.Protocols
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Include(x => x.Diagnoses.Where(d => !d.IsDeleted))
                .ThenInclude(d => d.IcdCode)
            .FirstOrDefaultAsync(x => x.PublicId == request.PublicId && !x.IsDeleted, ct)
            ?? throw new NotFoundException("Protokol bulunamadı.");

        return new ProtocolDetailResponse(
            p.PublicId,
            p.ProtocolNo,
            p.VisitId,
            p.PatientId,
            p.Patient != null ? $"{p.Patient.FirstName} {p.Patient.LastName}".Trim() : "",
            p.DoctorId,
            p.Doctor?.FullName ?? "",
            p.BranchId,
            (int)p.ProtocolType,
            VisitLabels.ProtocolType((int)p.ProtocolType),
            (int)p.Status,
            VisitLabels.ProtocolStatus((int)p.Status),
            p.ChiefComplaint,
            p.ExaminationFindings,
            p.Diagnosis,
            p.TreatmentPlan,
            p.Notes,
            p.StartedAt,
            p.CompletedAt,
            p.CreatedAt,
            p.Diagnoses.OrderByDescending(d => d.IsPrimary).ThenBy(d => d.Id)
                .Select(d => new ProtocolDiagnosisResponse(
                    d.PublicId, d.IcdCodeId,
                    d.IcdCode.Code, d.IcdCode.Description, d.IcdCode.Category,
                    d.IsPrimary, d.Note))
                .ToList());
    }
}
