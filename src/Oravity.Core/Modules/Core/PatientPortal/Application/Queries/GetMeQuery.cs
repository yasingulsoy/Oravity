using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Core.PatientPortal.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Core.PatientPortal.Application.Queries;

public record GetMeQuery(long AccountId) : IRequest<PortalAccountResponse>;

public class GetMeQueryHandler : IRequestHandler<GetMeQuery, PortalAccountResponse>
{
    private readonly AppDbContext _db;

    public GetMeQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PortalAccountResponse> Handle(
        GetMeQuery request,
        CancellationToken cancellationToken)
    {
        var account = await _db.PatientPortalAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken)
            ?? throw new NotFoundException("Portal hesabı bulunamadı.");

        return PatientPortalMappings.ToResponse(account);
    }
}
