using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.DentalChart.Application;
using Oravity.Core.Modules.Core.DentalChart.Domain.Services;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Core.DentalChart.Application.Queries;

public record GetToothHistoryQuery(
    Guid PatientPublicId,
    string ToothNumber
) : IRequest<IReadOnlyList<ToothHistoryResponse>>;

public class GetToothHistoryQueryHandler
    : IRequestHandler<GetToothHistoryQuery, IReadOnlyList<ToothHistoryResponse>>
{
    private readonly AppDbContext _db;
    private readonly IFdiChartService _fdi;

    public GetToothHistoryQueryHandler(AppDbContext db, IFdiChartService fdi)
    {
        _db = db;
        _fdi = fdi;
    }

    public async Task<IReadOnlyList<ToothHistoryResponse>> Handle(
        GetToothHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (!_fdi.IsValidToothNumber(request.ToothNumber))
            throw new ArgumentException($"Geçersiz FDI diş numarası: {request.ToothNumber}");

        var patientId = await _db.Patients
            .Where(p => p.PublicId == request.PatientPublicId && !p.IsDeleted)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var history = await (
            from h in _db.ToothConditionHistories.AsNoTracking()
            where h.PatientId == patientId && h.ToothNumber == request.ToothNumber
            join u in _db.Users.AsNoTracking() on h.ChangedBy equals u.Id into uj
            from u in uj.DefaultIfEmpty()
            orderby h.ChangedAt descending
            select new { History = h, ChangedByName = u != null ? u.FullName : "" }
        ).ToListAsync(cancellationToken);

        return history
            .Select(x => DentalChartMappings.ToHistoryResponse(x.History, x.ChangedByName))
            .ToList();
    }
}
