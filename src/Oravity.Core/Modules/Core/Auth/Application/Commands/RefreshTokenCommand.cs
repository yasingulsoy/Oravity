using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Auth.Application.Commands;

public record RefreshTokenCommand(string RefreshToken, string? IpAddress) : IRequest<LoginResponse>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;

    public RefreshTokenCommandHandler(AppDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new UnauthorizedException("Refresh token gerekli.");

        var tokenHash = _jwtService.HashToken(request.RefreshToken);

        var tokenEntity = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (tokenEntity == null || !tokenEntity.IsValid)
            throw new UnauthorizedException("Geçersiz veya süresi dolmuş refresh token.");

        if (tokenEntity.User is null || !tokenEntity.User.IsActive)
            throw new UnauthorizedException("Hesap aktif değil.");

        // Eski token'ı iptal et
        tokenEntity.Revoke();

        var user = tokenEntity.User;

        // Company/branch bağlamını çöz (Platform Admin dahil)
        long? branchId = null;
        long? companyId = null;
        int? roleLevel = null;

        var assignment = await _db.UserRoleAssignments.AsNoTracking()
            .Where(a => a.UserId == user.Id && a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is not null)
        {
            branchId  = assignment.BranchId;
            companyId = assignment.CompanyId;

            if (companyId == null && branchId.HasValue)
                companyId = await _db.Branches.AsNoTracking()
                    .Where(b => b.Id == branchId.Value)
                    .Select(b => (long?)b.CompanyId)
                    .FirstOrDefaultAsync(cancellationToken);

            roleLevel = user.IsPlatformAdmin ? 1
                : assignment.BranchId.HasValue ? 4
                : 2;
        }

        if (companyId == null)
        {
            if (user.IsPlatformAdmin)
            {
                // Platform admin: şirket sayısından bağımsız ilk şirketi kullan
                companyId = await _db.Companies.AsNoTracking()
                    .OrderBy(c => c.Id)
                    .Select(c => (long?)c.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                var companies = await _db.Companies.AsNoTracking()
                    .Select(c => c.Id).Take(2).ToListAsync(cancellationToken);
                if (companies.Count == 1)
                    companyId = companies[0];
            }

            if (companyId.HasValue)
            {
                if (branchId == null)
                {
                    var branches = await _db.Branches.AsNoTracking()
                        .Where(b => b.CompanyId == companyId.Value)
                        .Select(b => b.Id).Take(2).ToListAsync(cancellationToken);
                    if (branches.Count == 1)
                        branchId = branches[0];
                }
                roleLevel ??= user.IsPlatformAdmin ? 1 : 2;
            }
        }

        var newAccessToken = _jwtService.GenerateAccessToken(user, branchId, companyId, roleLevel);
        var newRefreshTokenStr = _jwtService.GenerateRefreshToken();
        var newTokenHash = _jwtService.HashToken(newRefreshTokenStr);

        _db.RefreshTokens.Add(
            RefreshToken.Create(tokenEntity.UserId, newTokenHash, DateTime.UtcNow.AddDays(7), request.IpAddress));

        await _db.SaveChangesAsync(cancellationToken);

        return new LoginResponse(newAccessToken, newRefreshTokenStr, 15 * 60);
    }
}
