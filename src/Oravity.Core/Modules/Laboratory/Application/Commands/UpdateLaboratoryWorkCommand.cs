using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Laboratory.Application;
using Oravity.Core.Modules.Laboratory.Application.Queries;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Commands;

/// <summary>
/// Sadece 'pending' durumdaki bir lab iş emrinin üst bilgilerini günceller.
/// </summary>
public record UpdateLaboratoryWorkCommand(
    Guid    WorkPublicId,
    Guid?   TreatmentPlanItemPublicId,
    string  WorkType,
    string  DeliveryType,
    string? ToothNumbers,
    string? ShadeColor,
    string? DoctorNotes
) : IRequest<LaboratoryWorkDetailResponse>;

public class UpdateLaboratoryWorkCommandHandler
    : IRequestHandler<UpdateLaboratoryWorkCommand, LaboratoryWorkDetailResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public UpdateLaboratoryWorkCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<LaboratoryWorkDetailResponse> Handle(
        UpdateLaboratoryWorkCommand request,
        CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct)
            ?? throw new ForbiddenException("Şirket bağlamı gerekli.");

        var work = await _db.LaboratoryWorks
            .FirstOrDefaultAsync(w => w.PublicId == request.WorkPublicId
                                       && w.CompanyId == companyId, ct)
            ?? throw new NotFoundException("Laboratuvar iş emri bulunamadı.");

        long? planItemId = null;
        if (request.TreatmentPlanItemPublicId is { } tpipid)
        {
            var planItem = await _db.TreatmentPlanItems.AsNoTracking()
                .FirstOrDefaultAsync(i => i.PublicId == tpipid, ct)
                ?? throw new NotFoundException("Tedavi planı kalemi bulunamadı.");
            planItemId = planItem.Id;
        }

        work.UpdateMetadata(
            request.WorkType,
            request.DeliveryType,
            request.ToothNumbers,
            request.ShadeColor,
            request.DoctorNotes,
            planItemId);

        await _db.SaveChangesAsync(ct);

        return await GetLaboratoryWorkDetailQueryHandler.BuildDetailAsync(_db, work.Id, ct);
    }
}
