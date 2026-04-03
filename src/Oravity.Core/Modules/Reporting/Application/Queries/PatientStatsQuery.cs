using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Reporting.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Reporting.Application.Queries;

/// <summary>
/// Hasta istatistikleri: yeni hasta sayısı, toplam aktif hasta, en çok tedavi görenler.
/// </summary>
public record PatientStatsQuery(
    DateTime StartDate,
    DateTime EndDate,
    int      TopPatientCount = 10) : IRequest<PatientStatsReport>;

public class PatientStatsQueryHandler : IRequestHandler<PatientStatsQuery, PatientStatsReport>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public PatientStatsQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<PatientStatsReport> Handle(PatientStatsQuery request, CancellationToken ct)
    {
        var startUtc = request.StartDate.Date.ToUniversalTime();
        var endUtc   = request.EndDate.Date.AddDays(1).ToUniversalTime();

        // Yeni hastalar — belirtilen tarih aralığında oluşturulanlar
        var newPatients = await _db.Patients
            .CountAsync(p => p.Branch.CompanyId == _tenant.CompanyId
                          && p.CreatedAt >= startUtc
                          && p.CreatedAt < endUtc, ct);

        // Toplam aktif hasta
        var totalActive = await _db.Patients
            .CountAsync(p => p.Branch.CompanyId == _tenant.CompanyId
                          && p.IsActive, ct);

        // En çok tedavi gören hastalar (tamamlanan kalem sayısı + toplam ödeme)
        var topPatients = await _db.TreatmentPlanItems
            .Where(i => i.Plan.Branch.CompanyId == _tenant.CompanyId
                     && i.Status == TreatmentItemStatus.Completed
                     && i.CompletedAt >= startUtc
                     && i.CompletedAt < endUtc)
            .GroupBy(i => i.Plan.PatientId)
            .Select(g => new { PatientId = g.Key, ItemCount = g.Count(), TotalRevenue = g.Sum(i => i.FinalPrice) })
            .OrderByDescending(x => x.ItemCount)
            .Take(request.TopPatientCount)
            .ToListAsync(ct);

        var patientIds = topPatients.Select(t => t.PatientId).ToList();

        var patientMap = await _db.Patients
            .Where(p => patientIds.Contains(p.Id))
            .Select(p => new { p.Id, p.PublicId, p.FirstName, p.LastName })
            .ToDictionaryAsync(p => p.Id, ct);

        // Toplam ödeme tutarları
        var startDate = DateOnly.FromDateTime(request.StartDate.Date);
        var endDate   = DateOnly.FromDateTime(request.EndDate.Date);
        var paidMap = await _db.Payments
            .Where(p => patientIds.Contains(p.PatientId)
                     && !p.IsRefunded
                     && p.PaymentDate >= startDate
                     && p.PaymentDate <= endDate)
            .GroupBy(p => p.PatientId)
            .Select(g => new { PatientId = g.Key, TotalPaid = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(p => p.PatientId, p => p.TotalPaid, ct);

        var topPatientLines = topPatients.Select(t =>
        {
            var patient = patientMap.GetValueOrDefault(t.PatientId);
            return new TopPatientLine(
                t.PatientId,
                patient?.PublicId ?? Guid.Empty,
                patient is not null ? $"{patient.FirstName} {patient.LastName}".Trim() : "Bilinmiyor",
                t.ItemCount,
                paidMap.GetValueOrDefault(t.PatientId, 0m));
        }).ToList();

        return new PatientStatsReport(
            request.StartDate, request.EndDate,
            newPatients, totalActive, topPatientLines);
    }
}
