using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Commands;

/// <summary>
/// Günlük kasa raporunu kapatır (Open → Closed).
/// Rapor yoksa önce oluşturulur.
/// İzin: report:close
/// </summary>
public record CloseDailyCashReportCommand(
    DateOnly Date,
    string?  Notes = null
) : IRequest<DailyCashReportResponse>;

public class CloseDailyCashReportCommandHandler
    : IRequestHandler<CloseDailyCashReportCommand, DailyCashReportResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser   _user;

    public CloseDailyCashReportCommandHandler(AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db     = db;
        _tenant = tenant;
        _user   = user;
    }

    public async Task<DailyCashReportResponse> Handle(
        CloseDailyCashReportCommand request, CancellationToken ct)
    {
        var branchId = _tenant.BranchId
            ?? throw new InvalidOperationException("Şube bağlamı bulunamadı.");

        var report = await _db.DailyCashReports
            .FirstOrDefaultAsync(r => r.BranchId == branchId && r.ReportDate == request.Date, ct);

        if (report is null)
        {
            report = DailyCashReport.Create(branchId, request.Date);
            _db.DailyCashReports.Add(report);
            await _db.SaveChangesAsync(ct);
        }

        if (report.Status == CashReportStatus.Approved)
            throw new InvalidOperationException("Onaylanmış kasa raporu tekrar kapatılamaz.");

        report.Close(_user.UserId, request.Notes);
        await _db.SaveChangesAsync(ct);

        return CashReportMappings.ToResponse(report);
    }
}
