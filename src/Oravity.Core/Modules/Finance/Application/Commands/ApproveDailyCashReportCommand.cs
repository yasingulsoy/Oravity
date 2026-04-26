using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Finance.Application.Commands;

/// <summary>
/// Kapatılmış kasa raporunu onaylar (Closed → Approved).
/// İzin: report:approve
/// </summary>
public record ApproveDailyCashReportCommand(
    Guid    PublicId,
    string? Notes = null
) : IRequest<DailyCashReportResponse>;

public class ApproveDailyCashReportCommandHandler
    : IRequestHandler<ApproveDailyCashReportCommand, DailyCashReportResponse>
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser   _user;

    public ApproveDailyCashReportCommandHandler(AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db     = db;
        _tenant = tenant;
        _user   = user;
    }

    public async Task<DailyCashReportResponse> Handle(
        ApproveDailyCashReportCommand request, CancellationToken ct)
    {
        var report = await _db.DailyCashReports
            .FirstOrDefaultAsync(r => r.PublicId == request.PublicId, ct)
            ?? throw new NotFoundException($"Kasa raporu bulunamadı: {request.PublicId}");

        if (_tenant.IsBranchLevel && report.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu rapora erişim yetkiniz yok.");

        report.Approve(_user.UserId, request.Notes);
        await _db.SaveChangesAsync(ct);

        return CashReportMappings.ToResponse(report);
    }
}
