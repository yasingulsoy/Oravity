using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Laboratory.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Queries;

public record GetLaboratoryWorksQuery(
    string? Status,
    Guid?   LaboratoryPublicId,
    Guid?   PatientPublicId,
    Guid?   DoctorPublicId,
    Guid?   BranchPublicId,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Search,
    int     Page = 1,
    int     PageSize = 50
) : IRequest<LaboratoryWorksPage>;

public record LaboratoryWorksPage(
    int TotalCount,
    IReadOnlyList<LaboratoryWorkListItemResponse> Items
);

public class GetLaboratoryWorksQueryHandler
    : IRequestHandler<GetLaboratoryWorksQuery, LaboratoryWorksPage>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public GetLaboratoryWorksQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<LaboratoryWorksPage> Handle(
        GetLaboratoryWorksQuery request,
        CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct);
        if (companyId == null) return new LaboratoryWorksPage(0, []);

        var q = _db.LaboratoryWorks.AsNoTracking()
            .Where(w => w.CompanyId == companyId.Value);

        if (!string.IsNullOrWhiteSpace(request.Status))
            q = q.Where(w => w.Status == request.Status);

        if (request.LaboratoryPublicId is { } labPid)
            q = q.Where(w => w.Laboratory.PublicId == labPid);

        if (request.PatientPublicId is { } patPid)
            q = q.Where(w => w.Patient.PublicId == patPid);

        if (request.DoctorPublicId is { } docPid)
            q = q.Where(w => w.Doctor.PublicId == docPid);

        if (request.BranchPublicId is { } bPid)
            q = q.Where(w => w.Branch.PublicId == bPid);

        if (request.FromDate.HasValue)
            q = q.Where(w => w.CreatedAt >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            q = q.Where(w => w.CreatedAt < request.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = $"%{request.Search.Trim()}%";
            q = q.Where(w =>
                EF.Functions.ILike(w.WorkNo, s)
                || EF.Functions.ILike(w.Patient.FirstName + " " + w.Patient.LastName, s)
                || EF.Functions.ILike(w.Laboratory.Name, s));
        }

        var total = await q.CountAsync(ct);

        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 200);

        var items = await q
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(w => new LaboratoryWorkListItemResponse(
                w.PublicId, w.WorkNo,
                w.Patient.PublicId, w.Patient.FirstName + " " + w.Patient.LastName,
                w.Doctor.PublicId,  w.Doctor.FullName,
                w.Laboratory.PublicId, w.Laboratory.Name,
                w.Branch.PublicId, w.Branch.Name,
                w.WorkType, w.DeliveryType, w.ToothNumbers, w.ShadeColor,
                w.Status, w.CreatedAt,
                w.SentToLabAt, w.EstimatedDeliveryDate,
                w.ReceivedFromLabAt, w.CompletedAt,
                w.TotalCost, w.Currency))
            .ToListAsync(ct);

        return new LaboratoryWorksPage(total, items);
    }
}
