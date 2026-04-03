using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;

namespace Oravity.Core.Modules.Appointment.OnlineBooking.Application.Queries;

public record GetBranchBySlugQuery(string Slug) : IRequest<BranchBySlugResult?>;

public record BranchBySlugResult(long BranchId, bool IsEnabled);

public class GetBranchBySlugQueryHandler : IRequestHandler<GetBranchBySlugQuery, BranchBySlugResult?>
{
    private readonly AppDbContext _db;

    public GetBranchBySlugQueryHandler(AppDbContext db) => _db = db;

    public async Task<BranchBySlugResult?> Handle(
        GetBranchBySlugQuery request, CancellationToken cancellationToken)
    {
        var settings = await _db.BranchOnlineBookingSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.WidgetSlug == request.Slug, cancellationToken);

        if (settings is null) return null;
        return new BranchBySlugResult(settings.BranchId, settings.IsEnabled);
    }
}
