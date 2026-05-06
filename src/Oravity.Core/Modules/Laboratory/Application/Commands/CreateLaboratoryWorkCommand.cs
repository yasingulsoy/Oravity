using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Laboratory.Application;
using Oravity.Core.Modules.Laboratory.Application.Queries;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Laboratory.Application.Commands;

public record CreateLaboratoryWorkCommand(
    Guid    PatientPublicId,
    Guid    LaboratoryPublicId,
    Guid?   TreatmentPlanItemPublicId,
    Guid?   BranchPublicId,           // null ise kullanıcının şubesi
    Guid?   DoctorPublicId,           // null ise mevcut kullanıcı
    string  WorkType,
    string  DeliveryType,
    string? ToothNumbers,
    string? ShadeColor,
    string? DoctorNotes,
    IReadOnlyList<LabWorkItemInputDto> Items
) : IRequest<LaboratoryWorkDetailResponse>;

public class CreateLaboratoryWorkCommandHandler
    : IRequestHandler<CreateLaboratoryWorkCommand, LaboratoryWorkDetailResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public CreateLaboratoryWorkCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<LaboratoryWorkDetailResponse> Handle(
        CreateLaboratoryWorkCommand request,
        CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct)
            ?? throw new ForbiddenException("Şirket bağlamı gerekli.");

        // Branch resolution
        long branchId;
        if (request.BranchPublicId is { } bpid)
        {
            var branch = await _db.Branches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.PublicId == bpid && b.CompanyId == companyId, ct)
                ?? throw new NotFoundException("Şube bulunamadı.");
            branchId = branch.Id;
        }
        else if (_tenant.BranchId.HasValue)
        {
            branchId = _tenant.BranchId.Value;
        }
        else
        {
            throw new InvalidOperationException("Şube bilgisi çözümlenemedi.");
        }

        // Laboratory resolution
        var lab = await _db.Laboratories.AsNoTracking()
            .FirstOrDefaultAsync(l => l.PublicId == request.LaboratoryPublicId
                                       && l.CompanyId == companyId
                                       && l.IsActive, ct)
            ?? throw new NotFoundException("Laboratuvar bulunamadı veya pasif.");

        // Patient resolution
        var patient = await _db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PublicId == request.PatientPublicId, ct)
            ?? throw new NotFoundException("Hasta bulunamadı.");

        // Doctor resolution
        long doctorId;
        if (request.DoctorPublicId is { } dpid)
        {
            var doctor = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.PublicId == dpid, ct)
                ?? throw new NotFoundException("Hekim bulunamadı.");
            doctorId = doctor.Id;
        }
        else
        {
            doctorId = _tenant.UserId > 0
                ? _tenant.UserId
                : throw new InvalidOperationException("Hekim çözümlenemedi.");
        }

        // TreatmentPlanItem (opsiyonel)
        long? planItemId = null;
        if (request.TreatmentPlanItemPublicId is { } tpipid)
        {
            var planItem = await _db.TreatmentPlanItems.AsNoTracking()
                .FirstOrDefaultAsync(i => i.PublicId == tpipid, ct)
                ?? throw new NotFoundException("Tedavi planı kalemi bulunamadı.");
            planItemId = planItem.Id;
        }

        // İş numarası üret: LAB-YYYY-#####
        var year = DateTime.UtcNow.Year;
        var prefix = $"LAB-{year}-";
        var lastNo = await _db.LaboratoryWorks.AsNoTracking()
            .Where(w => w.CompanyId == companyId && w.WorkNo.StartsWith(prefix))
            .OrderByDescending(w => w.WorkNo)
            .Select(w => w.WorkNo)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (!string.IsNullOrWhiteSpace(lastNo)
            && int.TryParse(lastNo[prefix.Length..], out var n)) seq = n + 1;
        var workNo = $"{prefix}{seq:D5}";

        // Fiyat kalemi publicId → Id lookup
        var priceItemPids = (request.Items ?? [])
            .Where(i => i.LabPriceItemPublicId.HasValue)
            .Select(i => i.LabPriceItemPublicId!.Value)
            .Distinct()
            .ToArray();
        var priceLookup = priceItemPids.Length == 0
            ? new Dictionary<Guid, long>()
            : await _db.LaboratoryPriceItems.AsNoTracking()
                .Where(p => priceItemPids.Contains(p.PublicId) && p.LaboratoryId == lab.Id)
                .ToDictionaryAsync(p => p.PublicId, p => p.Id, ct);

        // Entity oluştur
        var work = LaboratoryWork.Create(
            companyId, branchId, workNo,
            patient.Id, doctorId, lab.Id, planItemId,
            request.WorkType, request.DeliveryType,
            request.ToothNumbers, request.ShadeColor, request.DoctorNotes);

        decimal total = 0m;
        var currency = "TRY";
        var details = new List<object>();

        foreach (var i in request.Items ?? [])
        {
            long? priceId = null;
            if (i.LabPriceItemPublicId is { } ppid
                && priceLookup.TryGetValue(ppid, out var pid)) priceId = pid;

            var workItem = LaboratoryWorkItem.Create(
                priceId, i.ItemName, i.Quantity, i.UnitPrice, i.Currency, i.Notes);
            work.AddItem(workItem);
            total    += workItem.TotalPrice;
            currency  = workItem.Currency;
            details.Add(new {
                item       = workItem.ItemName,
                quantity   = workItem.Quantity,
                unit_price = workItem.UnitPrice,
                total      = workItem.TotalPrice,
                currency   = workItem.Currency
            });
        }

        work.SetCostSummary(total, currency, JsonSerializer.Serialize(details));

        _db.LaboratoryWorks.Add(work);
        await _db.SaveChangesAsync(ct);

        return await GetLaboratoryWorkDetailQueryHandler.BuildDetailAsync(_db, work.Id, ct);
    }
}
