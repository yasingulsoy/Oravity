using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Laboratory.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Queries;

public record GetLaboratoryWorkDetailQuery(Guid PublicId)
    : IRequest<LaboratoryWorkDetailResponse>;

public class GetLaboratoryWorkDetailQueryHandler
    : IRequestHandler<GetLaboratoryWorkDetailQuery, LaboratoryWorkDetailResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public GetLaboratoryWorkDetailQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<LaboratoryWorkDetailResponse> Handle(
        GetLaboratoryWorkDetailQuery request,
        CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct)
            ?? throw new ForbiddenException("Şirket bağlamı gerekli.");

        var workId = await _db.LaboratoryWorks.AsNoTracking()
            .Where(w => w.PublicId == request.PublicId && w.CompanyId == companyId)
            .Select(w => (long?)w.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Laboratuvar iş emri bulunamadı.");

        return await BuildDetailAsync(_db, workId, ct);
    }

    internal static async Task<LaboratoryWorkDetailResponse> BuildDetailAsync(
        AppDbContext db, long workId, CancellationToken ct)
    {
        var w = await db.LaboratoryWorks.AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Include(x => x.Laboratory)
            .Include(x => x.Branch)
            .Include(x => x.TreatmentPlanItem)
            .FirstOrDefaultAsync(x => x.Id == workId, ct)
            ?? throw new NotFoundException("Laboratuvar iş emri bulunamadı.");

        var items = await db.LaboratoryWorkItems.AsNoTracking()
            .Include(i => i.LabPriceItem)
            .Where(i => i.WorkId == workId)
            .OrderBy(i => i.Id)
            .Select(i => new LaboratoryWorkItemResponse(
                i.PublicId,
                i.LabPriceItem != null ? i.LabPriceItem.PublicId : (Guid?)null,
                i.ItemName, i.Quantity, i.UnitPrice, i.TotalPrice, i.Currency, i.Notes))
            .ToListAsync(ct);

        var history = await db.LaboratoryWorkHistories.AsNoTracking()
            .Where(h => h.WorkId == workId)
            .OrderBy(h => h.ChangedAt)
            .Select(h => new LaboratoryWorkHistoryEntry(
                h.ChangedAt, h.OldStatus, h.NewStatus, h.Notes, h.ChangedByUserId))
            .ToListAsync(ct);

        return new LaboratoryWorkDetailResponse(
            w.PublicId, w.WorkNo,
            w.Patient.PublicId, w.Patient.FirstName + " " + w.Patient.LastName,
            w.Doctor.PublicId, w.Doctor.FullName,
            w.Laboratory.PublicId, w.Laboratory.Name,
            w.Branch.PublicId, w.Branch.Name,
            w.TreatmentPlanItem != null ? w.TreatmentPlanItem.PublicId : (Guid?)null,
            w.WorkType, w.DeliveryType, w.ToothNumbers, w.ShadeColor, w.Status,
            w.SentToLabAt, w.EstimatedDeliveryDate, w.ReceivedFromLabAt,
            w.FittedToPatientAt, w.CompletedAt, w.ApprovedAt, w.ApprovedByUserId,
            w.TotalCost, w.Currency,
            w.DoctorNotes, w.LabNotes, w.ApprovalNotes, w.Attachments,
            items, history, w.CreatedAt);
    }
}
