using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Reporting.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Reporting.Application.Queries;

/// <summary>
/// Hekim performans raporu: tamamlanan randevu, tedavi kalemi, ciro, hakediş.
/// v_doctor_commissions view verisine karşılık gelir.
/// </summary>
public record DoctorPerformanceReportQuery(
    DateTime StartDate,
    DateTime EndDate,
    long?    DoctorId = null) : IRequest<DoctorPerformanceReport>;

public class DoctorPerformanceReportQueryHandler : IRequestHandler<DoctorPerformanceReportQuery, DoctorPerformanceReport>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public DoctorPerformanceReportQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<DoctorPerformanceReport> Handle(DoctorPerformanceReportQuery request, CancellationToken ct)
    {
        var startUtc = request.StartDate.Date.ToUniversalTime();
        var endUtc   = request.EndDate.Date.AddDays(1).ToUniversalTime();

        // Tamamlanan randevular — hekim bazında
        var apptQuery = _db.Appointments
            .Where(a => a.Branch.CompanyId == _tenant.CompanyId
                     && a.Status == AppointmentStatus.Completed
                     && a.StartTime >= startUtc
                     && a.StartTime < endUtc);

        if (request.DoctorId.HasValue)
            apptQuery = apptQuery.Where(a => a.DoctorId == request.DoctorId.Value);

        var completedAppts = await apptQuery
            .GroupBy(a => a.DoctorId)
            .Select(g => new { DoctorId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Tamamlanan tedavi kalemleri — plan üzerinden CompanyId filtresi
        var itemQuery = _db.TreatmentPlanItems
            .Where(i => i.Plan.Branch.CompanyId == _tenant.CompanyId
                     && i.Status == TreatmentItemStatus.Completed
                     && i.CompletedAt >= startUtc
                     && i.CompletedAt < endUtc);

        if (request.DoctorId.HasValue)
            itemQuery = itemQuery.Where(i => i.DoctorId == request.DoctorId.Value);

        var completedItems = await itemQuery
            .GroupBy(i => i.DoctorId)
            .Select(g => new
            {
                DoctorId = g.Key,
                ItemCount = g.Count(),
                TotalRevenue = g.Sum(i => i.FinalPrice)
            })
            .ToListAsync(ct);

        // Komisyon özeti — hekim bazında
        var commQuery = _db.DoctorCommissions
            .Include(c => c.Doctor)
            .Where(c => c.Branch.CompanyId == _tenant.CompanyId
                     && c.CreatedAt >= startUtc
                     && c.CreatedAt < endUtc);

        if (request.DoctorId.HasValue)
            commQuery = commQuery.Where(c => c.DoctorId == request.DoctorId.Value);

        var commissions = await commQuery
            .GroupBy(c => new { c.DoctorId, c.Doctor.FullName })
            .Select(g => new
            {
                g.Key.DoctorId,
                g.Key.FullName,
                TotalCommission = g.Sum(c => c.CommissionAmount),
                CommissionRate  = g.Average(c => c.CommissionRate)
            })
            .ToListAsync(ct);

        // Tüm doktor ID'lerini birleştir
        var allDoctorIds = completedAppts.Select(a => a.DoctorId)
            .Union(completedItems.Select(i => i.DoctorId ?? 0))
            .Union(commissions.Select(c => c.DoctorId))
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        // Doktor adlarını al (komisyon tablosunda henüz kaydı olmayabilir)
        var doctorNames = await _db.Users
            .Where(u => allDoctorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var lines = allDoctorIds.Select(doctorId =>
        {
            var appt = completedAppts.FirstOrDefault(a => a.DoctorId == doctorId);
            var item = completedItems.FirstOrDefault(i => i.DoctorId == doctorId);
            var comm = commissions.FirstOrDefault(c => c.DoctorId == doctorId);

            return new DoctorPerformanceLine(
                DoctorId:               doctorId,
                DoctorName:             comm?.FullName ?? doctorNames.GetValueOrDefault(doctorId, "Bilinmiyor"),
                CompletedAppointments:  appt?.Count ?? 0,
                CompletedTreatmentItems:item?.ItemCount ?? 0,
                TotalRevenue:           item?.TotalRevenue ?? 0m,
                TotalCommission:        comm?.TotalCommission ?? 0m,
                CommissionRate:         comm?.CommissionRate ?? 0m);
        })
        .OrderByDescending(l => l.TotalRevenue)
        .ToList();

        return new DoctorPerformanceReport(request.StartDate, request.EndDate, lines);
    }
}
