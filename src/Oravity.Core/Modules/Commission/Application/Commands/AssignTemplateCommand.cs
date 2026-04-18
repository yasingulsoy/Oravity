using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using static Oravity.Core.Modules.Core.Pricing.Application.TenantCompanyResolver;

namespace Oravity.Core.Modules.Commission.Application.Commands;

public record AssignTemplateCommand(
    long DoctorId,
    Guid TemplatePublicId,
    DateOnly EffectiveDate,
    DateOnly? ExpiryDate
) : IRequest<TemplateAssignmentResponse>;

public class AssignTemplateCommandHandler
    : IRequestHandler<AssignTemplateCommand, TemplateAssignmentResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public AssignTemplateCommandHandler(AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    public async Task<TemplateAssignmentResponse> Handle(
        AssignTemplateCommand r, CancellationToken ct)
    {
        var companyId = await ResolveCompanyIdAsync(_tenant, _db, ct)
            ?? throw new ForbiddenException("Şirket bağlamı gereklidir.");

        var template = await _db.DoctorCommissionTemplates
            .FirstOrDefaultAsync(t => t.PublicId == r.TemplatePublicId && t.CompanyId == companyId, ct)
            ?? throw new NotFoundException($"Şablon bulunamadı: {r.TemplatePublicId}");

        var doctor = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == r.DoctorId, ct)
            ?? throw new NotFoundException($"Hekim bulunamadı: {r.DoctorId}");

        // Aynı hekimin aktif ataması varsa expire et
        var current = await _db.DoctorTemplateAssignments
            .Where(a => a.DoctorId == r.DoctorId && a.IsActive)
            .ToListAsync(ct);

        foreach (var c in current)
            c.Expire(DateOnly.FromDateTime(DateTime.UtcNow.Date));

        var assignment = DoctorTemplateAssignment.Create(
            r.DoctorId, template.Id, r.EffectiveDate, r.ExpiryDate);

        if (_user.IsAuthenticated)
            assignment.SetCreatedBy(_user.UserId, _user.TenantId);

        _db.DoctorTemplateAssignments.Add(assignment);
        await _db.SaveChangesAsync(ct);

        return CommissionMappings.ToResponse(assignment, doctor.FullName, template.Name);
    }
}

public record UnassignTemplateCommand(Guid AssignmentPublicId) : IRequest<Unit>;

public class UnassignTemplateCommandHandler : IRequestHandler<UnassignTemplateCommand, Unit>
{
    private readonly AppDbContext _db;

    public UnassignTemplateCommandHandler(AppDbContext db) { _db = db; }

    public async Task<Unit> Handle(UnassignTemplateCommand r, CancellationToken ct)
    {
        var a = await _db.DoctorTemplateAssignments
            .FirstOrDefaultAsync(x => x.PublicId == r.AssignmentPublicId, ct)
            ?? throw new NotFoundException($"Atama bulunamadı: {r.AssignmentPublicId}");

        a.Expire(DateOnly.FromDateTime(DateTime.UtcNow.Date));
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
