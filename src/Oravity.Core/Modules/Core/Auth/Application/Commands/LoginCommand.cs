using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.Auth.Application.Commands;

public record LoginCommand(string Email, string Password, string? IpAddress, long? BranchId = null) : IRequest<LoginResponse>;

public record LoginResponse(
    string? AccessToken = null,
    string? RefreshToken = null,
    int ExpiresIn = 0,
    string TokenType = "Bearer",
    bool RequiresBranchSelection = false,
    List<BranchSelectionOption>? Branches = null);

public record BranchSelectionOption(long Id, string Name);

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(AppDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.ToLowerInvariant();

        // Son 30 dakikada 5+ başarısız deneme → 429
        var recentFails = await _db.LoginAttempts
            .CountAsync(a => a.Identifier == email
                          && !a.Success
                          && a.CreatedAt > DateTime.UtcNow.AddMinutes(-30),
                       cancellationToken);

        if (recentFails >= 5)
            throw new TooManyRequestsException("Çok fazla hatalı giriş denemesi. 30 dakika bekleyin.");

        // Kullanıcıyı bul (soft-delete filtresi zaten uygulanır)
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _db.LoginAttempts.Add(LoginAttempt.Create(email, request.IpAddress, false));
            await _db.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("E-posta veya şifre hatalı.");
        }

        if (!user.IsActive)
            throw new UnauthorizedException("Hesabınız aktif değil.");

        // Kullanıcının tüm aktif, süresi dolmamış şube atamalarını çek
        var activeAssignments = await _db.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == user.Id
                        && a.IsActive
                        && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
            .ToListAsync(cancellationToken);

        // Şube ataması olan (BranchId != null) olanları filtrele
        var branchAssignments = activeAssignments
            .Where(a => a.BranchId.HasValue)
            .ToList();

        // Benzersiz şube ID'leri
        var distinctBranchIds = branchAssignments
            .Select(a => a.BranchId!.Value)
            .Distinct()
            .ToList();

        // 2+ şube ataması varsa ve BranchId sağlanmamışsa → şube seçimi iste
        if (distinctBranchIds.Count >= 2 && request.BranchId == null)
        {
            var branchOptions = await _db.Branches
                .AsNoTracking()
                .Where(b => distinctBranchIds.Contains(b.Id))
                .OrderBy(b => b.Name)
                .Select(b => new BranchSelectionOption(b.Id, b.Name))
                .ToListAsync(cancellationToken);

            return new LoginResponse(
                RequiresBranchSelection: true,
                Branches: branchOptions);
        }

        // BranchId sağlandıysa: kullanıcının bu şubeye ataması var mı kontrol et
        UserRoleAssignment? primaryAssignment = null;

        if (request.BranchId.HasValue)
        {
            primaryAssignment = branchAssignments
                .FirstOrDefault(a => a.BranchId == request.BranchId.Value);

            if (primaryAssignment == null)
                throw new UnauthorizedException("Bu şubeye erişim yetkiniz yok.");
        }
        else if (distinctBranchIds.Count == 1)
        {
            // Tek şube ataması varsa → onu kullan
            primaryAssignment = branchAssignments.First();
        }
        else if (activeAssignments.Count > 0)
        {
            // Şube ataması yok ama başka atamalar var (company admin, platform admin)
            primaryAssignment = activeAssignments
                .OrderByDescending(a => a.CreatedAt)
                .First();
        }

        // Context'i çözümle
        var (primaryBranchId, primaryCompanyId, primaryRoleLevel) =
            await ResolveUserContext(user, primaryAssignment, cancellationToken);

        // Başarılı giriş
        _db.LoginAttempts.Add(LoginAttempt.Create(email, request.IpAddress, true));

        var accessToken = _jwtService.GenerateAccessToken(user, primaryBranchId, primaryCompanyId, primaryRoleLevel);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var tokenHash = _jwtService.HashToken(refreshToken);

        _db.RefreshTokens.Add(
            RefreshToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddDays(7), request.IpAddress));
        user.SetLastLoginAt();

        await _db.SaveChangesAsync(cancellationToken);

        return new LoginResponse(accessToken, refreshToken, 15 * 60);
    }

    private async Task<(long? BranchId, long? CompanyId, int? RoleLevel)> ResolveUserContext(
        User user, UserRoleAssignment? assignment, CancellationToken ct)
    {
        long? branchId = null;
        long? companyId = null;
        int? roleLevel = null;

        if (assignment is not null)
        {
            branchId  = assignment.BranchId;
            companyId = assignment.CompanyId;

            if (companyId == null && branchId.HasValue)
            {
                companyId = await _db.Branches.AsNoTracking()
                    .Where(b => b.Id == branchId.Value)
                    .Select(b => (long?)b.CompanyId)
                    .FirstOrDefaultAsync(ct);
            }

            roleLevel = user.IsPlatformAdmin ? 1
                : assignment.BranchId.HasValue ? 4
                : 2;
        }

        // Platform Admin: şirket sayısından bağımsız ilk şirketi kullan
        // Normal kullanıcı: sadece tek şirket varsa kullan
        if (companyId == null)
        {
            if (user.IsPlatformAdmin)
            {
                companyId = await _db.Companies.AsNoTracking()
                    .OrderBy(c => c.Id)
                    .Select(c => (long?)c.Id)
                    .FirstOrDefaultAsync(ct);
            }
            else
            {
                var companies = await _db.Companies.AsNoTracking()
                    .Select(c => c.Id).Take(2).ToListAsync(ct);
                if (companies.Count == 1)
                    companyId = companies[0];
            }

            if (companyId.HasValue)
            {
                if (branchId == null)
                {
                    var branches = await _db.Branches.AsNoTracking()
                        .Where(b => b.CompanyId == companyId.Value)
                        .Select(b => b.Id).Take(2).ToListAsync(ct);
                    if (branches.Count == 1)
                        branchId = branches[0];
                }
                roleLevel ??= user.IsPlatformAdmin ? 1 : 2;
            }
        }

        return (branchId, companyId, roleLevel);
    }
}
