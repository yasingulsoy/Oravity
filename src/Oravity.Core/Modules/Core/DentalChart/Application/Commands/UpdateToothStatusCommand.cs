using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.DentalChart.Application;
using Oravity.Core.Modules.Core.DentalChart.Domain.Services;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.DentalChart.Application.Commands;

public record UpdateToothStatusCommand(
    Guid PatientPublicId,
    string ToothNumber,
    ToothStatus Status,
    string? Surfaces = null,
    string? Notes = null,
    string? Reason = null
) : IRequest<ToothRecordResponse>;

public class UpdateToothStatusCommandHandler
    : IRequestHandler<UpdateToothStatusCommand, ToothRecordResponse>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly ITenantContext _tenant;
    private readonly IFdiChartService _fdi;

    public UpdateToothStatusCommandHandler(
        AppDbContext db, ICurrentUser user,
        ITenantContext tenant, IFdiChartService fdi)
    {
        _db = db;
        _user = user;
        _tenant = tenant;
        _fdi = fdi;
    }

    public async Task<ToothRecordResponse> Handle(
        UpdateToothStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (!_fdi.IsValidToothNumber(request.ToothNumber))
            throw new ArgumentException($"Geçersiz FDI diş numarası: {request.ToothNumber}");

        var patient = await _db.Patients
            .Where(p => p.PublicId == request.PatientPublicId && !p.IsDeleted)
            .Select(p => new { p.Id, p.BranchId })
            .FirstOrDefaultAsync(cancellationToken);

        if (patient is null)
            throw new ArgumentException("Hasta bulunamadı.");

        var patientId = patient.Id;

        // Yüzey kodlarını normalize et
        var normalizedSurfaces = request.Surfaces is not null
            ? string.Concat(_fdi.ParseSurfaces(request.Surfaces))
            : null;

        var existing = await _db.ToothRecords
            .FirstOrDefaultAsync(
                t => t.PatientId == patientId &&
                     t.ToothNumber == request.ToothNumber,
                cancellationToken);

        ToothStatus? oldStatus = null;

        if (existing is null)
        {
            var record = ToothRecord.Create(
                patientId:   patientId,
                branchId:    patient.BranchId,
                toothNumber: request.ToothNumber,
                status:      request.Status,
                recordedBy:  _user.UserId,
                companyId:   _tenant.CompanyId,
                surfaces:    normalizedSurfaces,
                notes:       request.Notes);

            _db.ToothRecords.Add(record);
            existing = record;
        }
        else
        {
            oldStatus = existing.UpdateStatus(
                request.Status, _user.UserId,
                normalizedSurfaces, request.Notes);
        }

        // Her durumda geçmiş kaydı ekle
        var history = ToothConditionHistory.Create(
            patientId:   patientId,
            toothNumber: request.ToothNumber,
            newStatus:   request.Status,
            changedBy:   _user.UserId,
            oldStatus:   oldStatus,
            reason:      request.Reason);

        _db.ToothConditionHistories.Add(history);
        await _db.SaveChangesAsync(cancellationToken);

        return DentalChartMappings.ToResponse(existing);
    }
}
