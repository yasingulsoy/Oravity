using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Commands;

/// <summary>
/// Kasa raporunu yeniden açar (Closed/Approved → Open).
/// İzin: report:reopen
/// </summary>
public record ReopenDailyCashReportCommand(Guid PublicId) : IRequest<DailyCashReportResponse>;

public class ReopenDailyCashReportCommandHandler
    : IRequestHandler<ReopenDailyCashReportCommand, DailyCashReportResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;

    public ReopenDailyCashReportCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db     = db;
        _tenant = tenant;
    }

    public async Task<DailyCashReportResponse> Handle(
        ReopenDailyCashReportCommand request, CancellationToken ct)
    {
        var report = await _db.DailyCashReports
            .FirstOrDefaultAsync(r => r.PublicId == request.PublicId, ct)
            ?? throw new NotFoundException($"Kasa raporu bulunamadı: {request.PublicId}");

        if (_tenant.IsBranchLevel && report.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu rapora erişim yetkiniz yok.");

        report.Reopen();
        await _db.SaveChangesAsync(ct);

        return CashReportMappings.ToResponse(report);
    }
}
