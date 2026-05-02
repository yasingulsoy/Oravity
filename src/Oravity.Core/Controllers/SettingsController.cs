using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Core.Pricing.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
[Produces("application/json")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public SettingsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    /// <summary>Platform admin'in companyId'si yoksa ilk şirketi döndürür.</summary>
    private async Task<long?> ResolveCompanyIdWithFallbackAsync(CancellationToken ct)
    {
        var companyId = await TenantCompanyResolver.ResolveCompanyIdAsync(_tenant, _db, ct);
        if (companyId is null && _tenant.IsPlatformAdmin)
            companyId = await _db.Companies.AsNoTracking()
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Id)
                .Select(c => (long?)c.Id)
                .FirstOrDefaultAsync(ct);
        return companyId;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ŞİRKET BİLGİLERİ
    // ═══════════════════════════════════════════════════════════════════════════

    [HttpGet("company")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> GetCompany(CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdWithFallbackAsync(ct);
        if (companyId is null) return NotFound("Şirket bulunamadı.");

        var c = await _db.Companies.AsNoTracking()
            .Where(x => x.Id == companyId)
            .Select(x => new CompanyResponse(
                x.PublicId, x.Name, x.DefaultLanguageCode, x.IsActive,
                x.SubscriptionEndsAt, x.Vertical.Name))
            .FirstOrDefaultAsync(ct);

        if (c is null) return NotFound();

        return Ok(c);
    }

    [HttpPut("company")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> UpdateCompany([FromBody] UpdateCompanyRequest req, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdWithFallbackAsync(ct);
        if (companyId is null) return NotFound("Şirket bulunamadı.");

        var c = await _db.Companies.FirstOrDefaultAsync(x => x.Id == companyId, ct);
        if (c is null) return NotFound();

        if (req.Name is not null && req.Name != c.Name)
            c.SetName(req.Name);
        if (req.DefaultLanguageCode is not null)
            c.SetLanguage(req.DefaultLanguageCode);

        c.MarkUpdated();
        await _db.SaveChangesAsync(ct);

        var verticalName = await _db.Verticals.AsNoTracking()
            .Where(v => v.Id == c.VerticalId)
            .Select(v => v.Name)
            .FirstOrDefaultAsync(ct) ?? "";

        return Ok(new CompanyResponse(
            c.PublicId, c.Name, c.DefaultLanguageCode, c.IsActive,
            c.SubscriptionEndsAt, verticalName));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ŞUBE YÖNETİMİ
    // ═══════════════════════════════════════════════════════════════════════════

    [HttpGet("branches")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> ListBranches(CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdWithFallbackAsync(ct);
        if (companyId is null) return Ok(Array.Empty<object>());

        var branches = await _db.Branches.AsNoTracking()
            .Where(b => b.CompanyId == companyId && !b.IsDeleted)
            .OrderBy(b => b.Name)
            .Select(b => new BranchResponse(
                b.PublicId, b.Name, b.DefaultLanguageCode, b.IsActive,
                b.PricingMultiplier,
                b.UserRoleAssignments.Count(a => a.IsActive),
                b.CreatedAt,
                b.Company.Name))
            .ToListAsync(ct);

        return Ok(branches);
    }

    [HttpGet("branches/{publicId:guid}")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> GetBranch(Guid publicId, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdWithFallbackAsync(ct);
        if (companyId is null) return NotFound();

        var branch = await _db.Branches.AsNoTracking()
            .Where(b => b.PublicId == publicId && b.CompanyId == companyId && !b.IsDeleted)
            .Select(b => new BranchDetailResponse(
                b.PublicId,
                b.Name,
                b.DefaultLanguageCode,
                b.IsActive,
                b.PricingMultiplier,
                b.VerticalId,
                b.Vertical != null ? b.Vertical.Name : null,
                b.CreatedAt,
                b.UpdatedAt,
                b.UserRoleAssignments.Count(a => a.IsActive),
                b.UserRoleAssignments
                    .Where(a => a.IsActive)
                    .Select(a => new BranchUserInfo(
                        a.User.PublicId, a.User.FullName, a.User.Email,
                        a.User.IsActive, a.User.Title,
                        a.RoleTemplate.Name, a.RoleTemplate.Code))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

        if (branch is null) return NotFound();
        return Ok(branch);
    }

    [HttpGet("branches/{publicId:guid}/users")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> ListBranchUsers(Guid publicId, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdWithFallbackAsync(ct);
        if (companyId is null) return NotFound();

        var branch = await _db.Branches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.PublicId == publicId && b.CompanyId == companyId, ct);
        if (branch is null) return NotFound();

        var users = await _db.UserRoleAssignments.AsNoTracking()
            .Where(a => a.BranchId == branch.Id && a.IsActive)
            .Select(a => new BranchUserInfo(
                a.User.PublicId, a.User.FullName, a.User.Email,
                a.User.IsActive, a.User.Title,
                a.RoleTemplate.Name, a.RoleTemplate.Code))
            .ToListAsync(ct);

        return Ok(users);
    }

    [HttpPost("branches")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest req, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdWithFallbackAsync(ct);
        if (companyId is null) return BadRequest("Şirket bilgisi bulunamadı.");

        var branch = Branch.Create(req.Name, companyId.Value, defaultLanguageCode: req.DefaultLanguageCode ?? "tr");
        _db.Branches.Add(branch);
        await _db.SaveChangesAsync(ct);

        var companyName = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId.Value)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct);

        return Ok(new BranchResponse(
            branch.PublicId, branch.Name, branch.DefaultLanguageCode, branch.IsActive,
            branch.PricingMultiplier, 0, branch.CreatedAt, companyName));
    }

    [HttpPut("branches/{publicId:guid}")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> UpdateBranch(Guid publicId, [FromBody] UpdateBranchRequest req, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdWithFallbackAsync(ct);
        if (companyId is null) return BadRequest("Şirket bilgisi bulunamadı.");

        var branch = await _db.Branches.FirstOrDefaultAsync(
            b => b.PublicId == publicId && b.CompanyId == companyId, ct);
        if (branch is null) return NotFound();

        if (req.Name is not null)
            branch.SetName(req.Name);
        if (req.DefaultLanguageCode is not null)
            branch.SetLanguage(req.DefaultLanguageCode);
        if (req.IsActive.HasValue)
            branch.SetActive(req.IsActive.Value);
        if (req.PricingMultiplier.HasValue)
            branch.SetPricingMultiplier(req.PricingMultiplier.Value);

        branch.MarkUpdated();
        await _db.SaveChangesAsync(ct);

        var userCount = await _db.UserRoleAssignments
            .CountAsync(a => a.BranchId == branch.Id && a.IsActive, ct);

        var companyName = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == branch.CompanyId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct);

        return Ok(new BranchResponse(
            branch.PublicId, branch.Name, branch.DefaultLanguageCode, branch.IsActive,
            branch.PricingMultiplier, userCount, branch.CreatedAt, companyName));
    }

    [HttpDelete("branches/{publicId:guid}")]
    [RequirePermission("branch:delete")]
    public async Task<IActionResult> DeleteBranch(Guid publicId, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdWithFallbackAsync(ct);
        if (companyId is null) return BadRequest("Şirket bilgisi bulunamadı.");

        var branch = await _db.Branches.FirstOrDefaultAsync(
            b => b.PublicId == publicId && b.CompanyId == companyId, ct);
        if (branch is null) return NotFound();

        var activeUserCount = await _db.UserRoleAssignments
            .CountAsync(a => a.BranchId == branch.Id && a.IsActive, ct);
        if (activeUserCount > 0)
            return BadRequest($"Bu şubede {activeUserCount} aktif kullanıcı var. Önce kullanıcıları başka şubeye taşıyın.");

        branch.SoftDelete();
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // KULLANICI YÖNETİMİ
    // ═══════════════════════════════════════════════════════════════════════════

    [HttpGet("users")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> ListUsers(CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdWithFallbackAsync(ct);
        if (companyId is null) return Ok(Array.Empty<object>());

        var users = await _db.UserRoleAssignments.AsNoTracking()
            .Where(a => a.CompanyId == companyId
                || (a.BranchId != null && _db.Branches.Any(b => b.Id == a.BranchId && b.CompanyId == companyId)))
            .Select(a => a.User)
            .Distinct()
            .OrderBy(u => u.FullName)
            .Select(u => new UserListResponse(
                u.PublicId,
                u.FullName,
                u.Email,
                u.IsActive,
                u.IsPlatformAdmin,
                u.Title,
                u.LastLoginAt,
                u.RoleAssignments
                    .Where(a => a.IsActive)
                    .Select(a => new UserRoleInfo(
                        a.RoleTemplate.Name,
                        a.RoleTemplate.Code,
                        a.Branch != null ? a.Branch.Name : null))
                    .ToList()))
            .ToListAsync(ct);

        return Ok(users);
    }

    [HttpGet("users/{publicId:guid}")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> GetUser(Guid publicId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking()
            .Include(u => u.RoleAssignments.Where(a => a.IsActive))
                .ThenInclude(a => a.RoleTemplate)
            .Include(u => u.RoleAssignments.Where(a => a.IsActive))
                .ThenInclude(a => a.Branch)
            .Include(u => u.Specialization)
            .FirstOrDefaultAsync(u => u.PublicId == publicId, ct);

        if (user is null) return NotFound();

        return Ok(new UserDetailResponse(
            user.PublicId, user.FullName, user.Email, user.IsActive,
            user.IsPlatformAdmin, user.Title,
            user.SpecializationId, user.Specialization?.Name, user.CalendarColor,
            user.DefaultAppointmentDuration, user.IsChiefPhysician,
            user.PreferredLanguageCode, user.LastLoginAt,
            user.RoleAssignments.Select(a => new UserRoleAssignmentResponse(
                a.PublicId, a.RoleTemplate.Code, a.RoleTemplate.Name,
                a.BranchId, a.Branch?.Name, a.CompanyId,
                a.IsActive, a.AssignedAt, a.ExpiresAt)).ToList()));
    }

    [HttpPost("users")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdWithFallbackAsync(ct);
        if (companyId is null) return BadRequest("Şirket bilgisi bulunamadı.");

        var emailNorm = req.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == emailNorm, ct))
            return Conflict("Bu e-posta adresi zaten kayıtlı.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 12);
        var user = Oravity.SharedKernel.Entities.User.Create(emailNorm, req.FullName.Trim(), passwordHash);

        if (req.Title is not null)
            user.UpdateDoctorProfile(req.Title, null, req.CalendarColor, req.DefaultAppointmentDuration);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        if (req.RoleCode is not null)
        {
            var role = await _db.RoleTemplates.FirstOrDefaultAsync(
                r => r.Code == req.RoleCode.ToUpperInvariant(), ct);
            if (role is not null)
            {
                long? branchId = null;
                if (req.BranchPublicId.HasValue)
                    branchId = await _db.Branches.Where(b => b.PublicId == req.BranchPublicId.Value)
                        .Select(b => (long?)b.Id).FirstOrDefaultAsync(ct);

                var assignment = UserRoleAssignment.Create(user.Id, role.Id, companyId, branchId);
                _db.UserRoleAssignments.Add(assignment);
                await _db.SaveChangesAsync(ct);
            }
        }

        return Ok(new { user.PublicId, user.Email, user.FullName });
    }

    [HttpPut("users/{publicId:guid}")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> UpdateUser(Guid publicId, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.PublicId == publicId, ct);
        if (user is null) return NotFound();

        if (req.FullName is not null)
            user.SetFullName(req.FullName.Trim());
        if (req.IsActive.HasValue)
            user.SetActive(req.IsActive.Value);
        if (req.Title is not null || req.CalendarColor is not null || req.DefaultAppointmentDuration.HasValue || req.SpecializationId.HasValue)
            user.UpdateDoctorProfile(
                req.Title ?? user.Title,
                req.SpecializationId.HasValue
                    ? (req.SpecializationId.Value == 0 ? null : (int?)req.SpecializationId.Value)
                    : user.SpecializationId,
                req.CalendarColor ?? user.CalendarColor,
                req.DefaultAppointmentDuration ?? user.DefaultAppointmentDuration);
        if (req.PreferredLanguageCode is not null)
            user.SetPreferredLanguage(req.PreferredLanguageCode);

        user.MarkUpdated();
        await _db.SaveChangesAsync(ct);

        return Ok(new { user.PublicId, user.Email, user.FullName, user.IsActive });
    }

    [HttpPost("users/{publicId:guid}/roles")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> AssignRole(Guid publicId, [FromBody] AssignRoleRequest req, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdWithFallbackAsync(ct);
        if (companyId is null) return BadRequest("Şirket bilgisi bulunamadı.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.PublicId == publicId, ct);
        if (user is null) return NotFound();

        var role = await _db.RoleTemplates.FirstOrDefaultAsync(
            r => r.Code == req.RoleCode.ToUpperInvariant(), ct);
        if (role is null) return BadRequest("Rol bulunamadı.");

        long? branchId = null;
        if (req.BranchPublicId.HasValue)
            branchId = await _db.Branches.Where(b => b.PublicId == req.BranchPublicId.Value)
                .Select(b => (long?)b.Id).FirstOrDefaultAsync(ct);

        var assignment = UserRoleAssignment.Create(user.Id, role.Id, companyId, branchId);
        _db.UserRoleAssignments.Add(assignment);
        await _db.SaveChangesAsync(ct);

        return Ok(new { assignment.PublicId, RoleName = role.Name, role.Code });
    }

    [HttpDelete("users/{userPublicId:guid}/roles/{assignmentPublicId:guid}")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> RevokeRole(Guid userPublicId, Guid assignmentPublicId, CancellationToken ct)
    {
        var assignment = await _db.UserRoleAssignments
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.PublicId == assignmentPublicId && a.User.PublicId == userPublicId, ct);

        if (assignment is null) return NotFound();

        assignment.Revoke();
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("users/{publicId:guid}")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> DeleteUser(Guid publicId, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.RoleAssignments.Where(a => a.IsActive))
            .FirstOrDefaultAsync(u => u.PublicId == publicId, ct);
        if (user is null) return NotFound();

        if (user.IsPlatformAdmin)
            return BadRequest("Platform admin kullanıcıları silinemez.");

        foreach (var assignment in user.RoleAssignments)
            assignment.Revoke();

        user.SetActive(false);
        user.SoftDelete();
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ROLLER & İZİNLER
    // ═══════════════════════════════════════════════════════════════════════════

    [HttpGet("roles")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> ListRoles(CancellationToken ct)
    {
        var roles = await _db.RoleTemplates.AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Code)
            .Select(r => new RoleResponse(
                r.PublicId, r.Code, r.Name, r.Description,
                r.RoleTemplatePermissions.Select(p => p.Permission.Code).ToList(),
                r.UserRoleAssignments.Count(a => a.IsActive)))
            .ToListAsync(ct);

        return Ok(roles);
    }

    [HttpGet("permissions")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> ListPermissions(CancellationToken ct)
    {
        var perms = await _db.Permissions.AsNoTracking()
            .OrderBy(p => p.Resource).ThenBy(p => p.Action)
            .Select(p => new PermissionResponse(p.PublicId, p.Code, p.Resource, p.Action, p.IsDangerous))
            .ToListAsync(ct);

        return Ok(perms);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ŞUBE GÜVENLİK POLİTİKASI
    // ═══════════════════════════════════════════════════════════════════════════

    [HttpGet("branches/{publicId:guid}/security-policy")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> GetSecurityPolicy(Guid publicId, CancellationToken ct)
    {
        var branch = await _db.Branches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.PublicId == publicId, ct);
        if (branch is null) return NotFound();

        var policy = await _db.BranchSecurityPolicies.AsNoTracking()
            .FirstOrDefaultAsync(p => p.BranchId == branch.Id, ct);

        if (policy is null)
            return Ok(SecurityPolicyResponse.Default(publicId));

        return Ok(new SecurityPolicyResponse(
            publicId, policy.TwoFaRequired, policy.TwoFaSkipInternalIp,
            policy.AllowedIpRanges, policy.SessionTimeoutMinutes,
            policy.MaxFailedAttempts, policy.LockoutMinutes));
    }

    [HttpPut("branches/{publicId:guid}/security-policy")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> UpdateSecurityPolicy(
        Guid publicId, [FromBody] UpdateSecurityPolicyRequest req, CancellationToken ct)
    {
        var branch = await _db.Branches.FirstOrDefaultAsync(b => b.PublicId == publicId, ct);
        if (branch is null) return NotFound();

        var policy = await _db.BranchSecurityPolicies.FirstOrDefaultAsync(p => p.BranchId == branch.Id, ct);
        if (policy is null)
        {
            policy = BranchSecurityPolicy.CreateDefault(branch.Id);
            _db.BranchSecurityPolicies.Add(policy);
        }

        policy.Update(
            req.TwoFaRequired,
            req.TwoFaSkipInternalIp,
            req.AllowedIpRanges,
            req.SessionTimeoutMinutes,
            req.MaxFailedAttempts,
            req.LockoutMinutes);

        await _db.SaveChangesAsync(ct);

        return Ok(new SecurityPolicyResponse(
            publicId, policy.TwoFaRequired, policy.TwoFaSkipInternalIp,
            policy.AllowedIpRanges, policy.SessionTimeoutMinutes,
            policy.MaxFailedAttempts, policy.LockoutMinutes));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UZMANLIK ALANLARI (hekim oluşturma için)
    // ═══════════════════════════════════════════════════════════════════════════

    [HttpGet("specializations")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> ListSpecializations(CancellationToken ct)
    {
        var items = await _db.Specializations.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .Select(s => new { s.Id, s.Name, s.Code })
            .ToListAsync(ct);

        return Ok(items);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // POS CİHAZLARI
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task<long?> ResolveBranchIdAsync(Guid? branchPublicId, CancellationToken ct)
    {
        if (_tenant.BranchId is not null) return _tenant.BranchId;
        if (branchPublicId is null) return null;

        var companyId = await TenantCompanyResolver.ResolveCompanyIdAsync(_tenant, _db, ct);
        return await _db.Branches.AsNoTracking()
            .Where(b => b.PublicId == branchPublicId.Value && !b.IsDeleted &&
                        (companyId == null || b.CompanyId == companyId))
            .Select(b => (long?)b.Id)
            .FirstOrDefaultAsync(ct);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BANKALAR (global referans)
    // ═══════════════════════════════════════════════════════════════════════════

    [HttpGet("banks")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> ListBanks(CancellationToken ct)
    {
        var items = await _db.Banks.AsNoTracking()
            .Where(b => b.IsActive && !b.IsDeleted)
            .OrderBy(b => b.ShortName)
            .Select(b => new BankResponse(b.PublicId, b.Name, b.ShortName, b.BicCode, b.IsActive))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("banks")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> CreateBank([FromBody] CreateBankRequest req, CancellationToken ct)
    {
        var bank = Bank.Create(req.Name, req.ShortName, req.BicCode);
        _db.Banks.Add(bank);
        await _db.SaveChangesAsync(ct);
        return Ok(new BankResponse(bank.PublicId, bank.Name, bank.ShortName, bank.BicCode, bank.IsActive));
    }

    [HttpPut("banks/{publicId:guid}")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> UpdateBank(Guid publicId, [FromBody] CreateBankRequest req, CancellationToken ct)
    {
        var bank = await _db.Banks.FirstOrDefaultAsync(b => b.PublicId == publicId && !b.IsDeleted, ct);
        if (bank is null) return NotFound();
        bank.Update(req.Name, req.ShortName, req.BicCode);
        await _db.SaveChangesAsync(ct);
        return Ok(new BankResponse(bank.PublicId, bank.Name, bank.ShortName, bank.BicCode, bank.IsActive));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ÖDEME SAĞLAYICILARI (global referans)
    // ═══════════════════════════════════════════════════════════════════════════

    [HttpGet("payment-providers")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> ListPaymentProviders(CancellationToken ct)
    {
        var items = await _db.PaymentProviders.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Select(p => new PaymentProviderResponse(p.PublicId, p.Name, p.ShortName, p.Website, p.IsActive))
            .ToListAsync(ct);
        return Ok(items);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // POS CİHAZLARI
    // ═══════════════════════════════════════════════════════════════════════════

    [HttpGet("pos-terminals")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> ListPosTerminals([FromQuery] Guid? branchId, CancellationToken ct)
    {
        var bid = await ResolveBranchIdAsync(branchId, ct);
        if (bid is null) return BadRequest("Şube bilgisi gereklidir.");

        var items = await _db.PosTerminals.AsNoTracking()
            .Include(p => p.Bank)
            .Where(p => p.BranchId == bid && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Select(p => new PosTerminalResponse(
                p.PublicId, p.Name,
                p.BankId, p.Bank != null ? p.Bank.PublicId : (Guid?)null,
                p.Bank != null ? p.Bank.ShortName : null,
                p.TerminalId, p.IsActive))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("pos-terminals")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> CreatePosTerminal(
        [FromQuery] Guid? branchId, [FromBody] CreatePosTerminalRequest req, CancellationToken ct)
    {
        var bid = await ResolveBranchIdAsync(branchId, ct);
        if (bid is null) return BadRequest("Şube bilgisi gereklidir.");

        long? bankId = null;
        if (req.BankPublicId.HasValue)
            bankId = await _db.Banks.Where(b => b.PublicId == req.BankPublicId.Value && !b.IsDeleted)
                .Select(b => (long?)b.Id).FirstOrDefaultAsync(ct);

        var terminal = PosTerminal.Create(bid.Value, req.Name, bankId, req.TerminalId);
        _db.PosTerminals.Add(terminal);
        await _db.SaveChangesAsync(ct);

        var bank = bankId.HasValue
            ? await _db.Banks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bankId, ct)
            : null;

        return Ok(new PosTerminalResponse(
            terminal.PublicId, terminal.Name,
            bankId, bank?.PublicId, bank?.ShortName,
            terminal.TerminalId, terminal.IsActive));
    }

    [HttpPut("pos-terminals/{publicId:guid}")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> UpdatePosTerminal(
        Guid publicId, [FromBody] CreatePosTerminalRequest req, CancellationToken ct)
    {
        var terminal = await _db.PosTerminals.FirstOrDefaultAsync(p => p.PublicId == publicId && !p.IsDeleted, ct);
        if (terminal is null) return NotFound();

        long? bankId = null;
        if (req.BankPublicId.HasValue)
            bankId = await _db.Banks.Where(b => b.PublicId == req.BankPublicId.Value && !b.IsDeleted)
                .Select(b => (long?)b.Id).FirstOrDefaultAsync(ct);

        terminal.Update(req.Name, bankId, req.TerminalId);
        await _db.SaveChangesAsync(ct);

        var bank = bankId.HasValue
            ? await _db.Banks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bankId, ct)
            : null;

        return Ok(new PosTerminalResponse(
            terminal.PublicId, terminal.Name,
            bankId, bank?.PublicId, bank?.ShortName,
            terminal.TerminalId, terminal.IsActive));
    }

    [HttpDelete("pos-terminals/{publicId:guid}")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> DeletePosTerminal(Guid publicId, CancellationToken ct)
    {
        var terminal = await _db.PosTerminals.FirstOrDefaultAsync(p => p.PublicId == publicId && !p.IsDeleted, ct);
        if (terminal is null) return NotFound();

        terminal.Deactivate();
        terminal.SoftDelete();
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BANKA HESAPLARI
    // ═══════════════════════════════════════════════════════════════════════════

    [HttpGet("bank-accounts")]
    [RequirePermission("settings:view")]
    public async Task<IActionResult> ListBankAccounts([FromQuery] Guid? branchId, CancellationToken ct)
    {
        var bid = await ResolveBranchIdAsync(branchId, ct);
        if (bid is null) return BadRequest("Şube bilgisi gereklidir.");

        var items = await _db.BankAccounts.AsNoTracking()
            .Include(b => b.Bank)
            .Where(b => b.BranchId == bid && !b.IsDeleted)
            .OrderBy(b => b.Bank != null ? b.Bank.ShortName : "").ThenBy(b => b.AccountName)
            .Select(b => new BankAccountResponse(
                b.PublicId,
                b.BankId, b.Bank != null ? b.Bank.PublicId : (Guid?)null,
                b.Bank != null ? b.Bank.ShortName : null,
                b.AccountName, b.Iban, b.Currency, b.IsActive))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("bank-accounts")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> CreateBankAccount(
        [FromQuery] Guid? branchId, [FromBody] CreateBankAccountRequest req, CancellationToken ct)
    {
        var bid = await ResolveBranchIdAsync(branchId, ct);
        if (bid is null) return BadRequest("Şube bilgisi gereklidir.");

        long? bankId = null;
        if (req.BankPublicId.HasValue)
            bankId = await _db.Banks.Where(b => b.PublicId == req.BankPublicId.Value && !b.IsDeleted)
                .Select(b => (long?)b.Id).FirstOrDefaultAsync(ct);

        var account = BankAccount.Create(bid.Value, bankId, req.AccountName, req.Iban, req.Currency ?? "TRY");
        _db.BankAccounts.Add(account);
        await _db.SaveChangesAsync(ct);

        var bank = bankId.HasValue
            ? await _db.Banks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bankId, ct)
            : null;

        return Ok(new BankAccountResponse(
            account.PublicId,
            bankId, bank?.PublicId, bank?.ShortName,
            account.AccountName, account.Iban, account.Currency, account.IsActive));
    }

    [HttpPut("bank-accounts/{publicId:guid}")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> UpdateBankAccount(
        Guid publicId, [FromBody] CreateBankAccountRequest req, CancellationToken ct)
    {
        var account = await _db.BankAccounts.FirstOrDefaultAsync(b => b.PublicId == publicId && !b.IsDeleted, ct);
        if (account is null) return NotFound();

        long? bankId = null;
        if (req.BankPublicId.HasValue)
            bankId = await _db.Banks.Where(b => b.PublicId == req.BankPublicId.Value && !b.IsDeleted)
                .Select(b => (long?)b.Id).FirstOrDefaultAsync(ct);

        account.Update(bankId, req.AccountName, req.Iban, req.Currency ?? "TRY");
        await _db.SaveChangesAsync(ct);

        var bank = bankId.HasValue
            ? await _db.Banks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bankId, ct)
            : null;

        return Ok(new BankAccountResponse(
            account.PublicId,
            bankId, bank?.PublicId, bank?.ShortName,
            account.AccountName, account.Iban, account.Currency, account.IsActive));
    }

    [HttpDelete("bank-accounts/{publicId:guid}")]
    [RequirePermission("settings:edit_general")]
    public async Task<IActionResult> DeleteBankAccount(Guid publicId, CancellationToken ct)
    {
        var account = await _db.BankAccounts.FirstOrDefaultAsync(b => b.PublicId == publicId && !b.IsDeleted, ct);
        if (account is null) return NotFound();

        account.Deactivate();
        account.SoftDelete();
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DTO'lar
// ═══════════════════════════════════════════════════════════════════════════════

public record CompanyResponse(
    Guid PublicId, string Name, string DefaultLanguageCode, bool IsActive,
    DateTime? SubscriptionEndsAt, string VerticalName);

public record UpdateCompanyRequest(string? Name, string? DefaultLanguageCode);

public record BranchResponse(
    Guid PublicId, string Name, string DefaultLanguageCode, bool IsActive,
    decimal PricingMultiplier, int ActiveUserCount, DateTime CreatedAt, string? CompanyName = null);

public record BranchDetailResponse(
    Guid PublicId, string Name, string DefaultLanguageCode, bool IsActive,
    decimal PricingMultiplier, long? VerticalId, string? VerticalName,
    DateTime CreatedAt, DateTime? UpdatedAt, int ActiveUserCount,
    List<BranchUserInfo> Users);

public record BranchUserInfo(
    Guid PublicId, string FullName, string Email, bool IsActive,
    string? Title, string RoleName, string RoleCode);

public record CreateBranchRequest(string Name, string? DefaultLanguageCode);

public record UpdateBranchRequest(
    string? Name, string? DefaultLanguageCode, bool? IsActive, decimal? PricingMultiplier);

public record UserListResponse(
    Guid PublicId, string FullName, string Email, bool IsActive, bool IsPlatformAdmin,
    string? Title, DateTime? LastLoginAt, List<UserRoleInfo> Roles);

public record UserRoleInfo(string RoleName, string RoleCode, string? BranchName);

public record UserDetailResponse(
    Guid PublicId, string FullName, string Email, bool IsActive, bool IsPlatformAdmin,
    string? Title, int? SpecializationId, string? SpecializationName, string? CalendarColor,
    int? DefaultAppointmentDuration, bool IsChiefPhysician,
    string? PreferredLanguageCode, DateTime? LastLoginAt,
    List<UserRoleAssignmentResponse> RoleAssignments);

public record UserRoleAssignmentResponse(
    Guid PublicId, string RoleCode, string RoleName,
    long? BranchId, string? BranchName, long? CompanyId,
    bool IsActive, DateTime AssignedAt, DateTime? ExpiresAt);

public record CreateUserRequest(
    string Email, string FullName, string Password,
    string? RoleCode, Guid? BranchPublicId,
    string? Title, string? CalendarColor, int? DefaultAppointmentDuration);

public class UpdateUserRequest
{
    public string? FullName { get; init; }
    public bool? IsActive { get; init; }
    public string? Title { get; init; }
    public int? SpecializationId { get; init; } // null=no change, 0=clear, >0=set
    public string? CalendarColor { get; init; }
    public int? DefaultAppointmentDuration { get; init; }
    public string? PreferredLanguageCode { get; init; }
}

public record AssignRoleRequest(string RoleCode, Guid? BranchPublicId);

public record RoleResponse(
    Guid PublicId, string Code, string Name, string? Description,
    List<string> Permissions, int ActiveUserCount);

public record PermissionResponse(Guid PublicId, string Code, string Resource, string Action, bool IsDangerous);

public record SecurityPolicyResponse(
    Guid BranchPublicId, bool TwoFaRequired, bool TwoFaSkipInternalIp,
    string? AllowedIpRanges, int SessionTimeoutMinutes,
    int MaxFailedAttempts, int LockoutMinutes)
{
    public static SecurityPolicyResponse Default(Guid branchPublicId) => new(
        branchPublicId, false, true, null, 480, 5, 30);
}

public record UpdateSecurityPolicyRequest(
    bool TwoFaRequired, bool TwoFaSkipInternalIp,
    string? AllowedIpRanges, int SessionTimeoutMinutes,
    int MaxFailedAttempts, int LockoutMinutes);

public record BankResponse(Guid PublicId, string Name, string ShortName, string? BicCode, bool IsActive);
public record CreateBankRequest(string Name, string ShortName, string? BicCode);

public record PaymentProviderResponse(Guid PublicId, string Name, string? ShortName, string? Website, bool IsActive);

public record PosTerminalResponse(
    Guid PublicId, string Name,
    long? BankId, Guid? BankPublicId, string? BankShortName,
    string? TerminalId, bool IsActive);

public record CreatePosTerminalRequest(string Name, Guid? BankPublicId, string? TerminalId);

public record BankAccountResponse(
    Guid PublicId,
    long? BankId, Guid? BankPublicId, string? BankShortName,
    string AccountName, string? Iban, string Currency, bool IsActive);

public record CreateBankAccountRequest(Guid? BankPublicId, string AccountName, string? Iban, string? Currency);
