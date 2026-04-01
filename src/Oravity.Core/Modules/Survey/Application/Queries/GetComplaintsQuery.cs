using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Survey.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Survey.Application.Queries;

public record GetComplaintsQuery(
    ComplaintStatus? Status = null,
    ComplaintPriority? Priority = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<GetComplaintsResult>;

public record GetComplaintsResult(
    IReadOnlyList<ComplaintResponse> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public class GetComplaintsQueryHandler
    : IRequestHandler<GetComplaintsQuery, GetComplaintsResult>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly ITenantContext _tenant;

    public GetComplaintsQueryHandler(AppDbContext db, ICurrentUser user, ITenantContext tenant)
    {
        _db     = db;
        _user   = user;
        _tenant = tenant;
    }

    public async Task<GetComplaintsResult> Handle(
        GetComplaintsQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenant.CompanyId
            ?? throw new UnauthorizedAccessException("Şirket bağlamı bulunamadı.");

        var query = _db.Complaints
            .Where(c => c.CompanyId == companyId);

        if (request.Status.HasValue)   query = query.Where(c => c.Status   == request.Status.Value);
        if (request.Priority.HasValue) query = query.Where(c => c.Priority == request.Priority.Value);
        if (request.From.HasValue)     query = query.Where(c => c.CreatedAt >= request.From.Value);
        if (request.To.HasValue)       query = query.Where(c => c.CreatedAt <= request.To.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new GetComplaintsResult(
            items.Select(SurveyMappings.ToResponse).ToList(),
            total, request.Page, request.PageSize);
    }
}
