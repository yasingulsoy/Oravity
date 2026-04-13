using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.DentalChart.Application;
using Oravity.Core.Modules.Core.DentalChart.Domain.Services;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.DentalChart.Application.Commands;

public record ToothUpdateItem(
    string ToothNumber,
    ToothStatus Status,
    string? Surfaces = null,
    string? Notes = null
);

public record BulkUpdateTeethCommand(
    Guid PatientPublicId,
    IReadOnlyList<ToothUpdateItem> Teeth,
    /// <summary>Toplu güncelleme sebebi (sesli komut kaynağı için).</summary>
    string? Reason = null
) : IRequest<IReadOnlyList<ToothRecordResponse>>;

public class BulkUpdateTeethCommandHandler
    : IRequestHandler<BulkUpdateTeethCommand, IReadOnlyList<ToothRecordResponse>>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly ITenantContext _tenant;
    private readonly IFdiChartService _fdi;

    public BulkUpdateTeethCommandHandler(
        AppDbContext db, ICurrentUser user,
        ITenantContext tenant, IFdiChartService fdi)
    {
        _db = db;
        _user = user;
        _tenant = tenant;
        _fdi = fdi;
    }

    public async Task<IReadOnlyList<ToothRecordResponse>> Handle(
        BulkUpdateTeethCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Teeth.Count == 0)
            throw new ArgumentException("En az bir diş güncellemesi gereklidir.");

        // Tüm diş numaralarını doğrula
        var invalid = request.Teeth
            .Where(t => !_fdi.IsValidToothNumber(t.ToothNumber))
            .Select(t => t.ToothNumber)
            .ToList();

        if (invalid.Count > 0)
            throw new ArgumentException(
                $"Geçersiz FDI diş numaraları: {string.Join(", ", invalid)}");

        var patient = await _db.Patients
            .Where(p => p.PublicId == request.PatientPublicId && !p.IsDeleted)
            .Select(p => new { p.Id, p.BranchId })
            .FirstOrDefaultAsync(cancellationToken);

        if (patient is null)
            throw new ArgumentException("Hasta bulunamadı.");

        var patientId = patient.Id;

        var toothNumbers = request.Teeth.Select(t => t.ToothNumber).ToList();

        // Mevcut kayıtları toplu çek
        var existingRecords = await _db.ToothRecords
            .Where(r => r.PatientId == patientId &&
                        toothNumbers.Contains(r.ToothNumber))
            .ToDictionaryAsync(r => r.ToothNumber, cancellationToken);

        var results = new List<ToothRecord>();
        var historyEntries = new List<ToothConditionHistory>();

        foreach (var item in request.Teeth)
        {
            var normalizedSurfaces = item.Surfaces is not null
                ? string.Concat(_fdi.ParseSurfaces(item.Surfaces))
                : null;

            ToothStatus? oldStatus = null;
            ToothRecord record;

            if (existingRecords.TryGetValue(item.ToothNumber, out var existing))
            {
                oldStatus = existing.UpdateStatus(
                    item.Status, _user.UserId,
                    normalizedSurfaces, item.Notes);
                record = existing;
            }
            else
            {
                record = ToothRecord.Create(
                    patientId:   patientId,
                    branchId:    patient.BranchId,
                    toothNumber: item.ToothNumber,
                    status:      item.Status,
                    recordedBy:  _user.UserId,
                    companyId:   _tenant.CompanyId,
                    surfaces:    normalizedSurfaces,
                    notes:       item.Notes);

                _db.ToothRecords.Add(record);
            }

            historyEntries.Add(ToothConditionHistory.Create(
                patientId:   patientId,
                toothNumber: item.ToothNumber,
                newStatus:   item.Status,
                changedBy:   _user.UserId,
                oldStatus:   oldStatus,
                reason:      request.Reason));

            results.Add(record);
        }

        _db.ToothConditionHistories.AddRange(historyEntries);
        await _db.SaveChangesAsync(cancellationToken);

        return results.Select(DentalChartMappings.ToResponse).ToList();
    }
}
