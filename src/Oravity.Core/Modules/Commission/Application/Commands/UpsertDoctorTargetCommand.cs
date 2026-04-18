using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Commission.Application.Commands;

public record UpsertDoctorTargetCommand(
    long DoctorId,
    long BranchId,
    int Year,
    int Month,
    decimal TargetAmount
) : IRequest<DoctorTargetResponse>;

public class UpsertDoctorTargetCommandHandler
    : IRequestHandler<UpsertDoctorTargetCommand, DoctorTargetResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public UpsertDoctorTargetCommandHandler(
        AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    public async Task<DoctorTargetResponse> Handle(
        UpsertDoctorTargetCommand r, CancellationToken ct)
    {
        if (_tenant.IsBranchLevel && _tenant.BranchId != r.BranchId)
            throw new ForbiddenException("Bu şubeye erişim yetkiniz bulunmuyor.");

        var doctor = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == r.DoctorId, ct)
            ?? throw new NotFoundException($"Hekim bulunamadı: {r.DoctorId}");

        var existing = await _db.DoctorTargets
            .FirstOrDefaultAsync(t =>
                t.DoctorId == r.DoctorId && t.BranchId == r.BranchId &&
                t.Year == r.Year && t.Month == r.Month, ct);

        DoctorTarget entity;
        if (existing != null)
        {
            existing.SetAmount(r.TargetAmount);
            if (_user.IsAuthenticated) existing.SetUpdatedBy(_user.UserId);
            entity = existing;
        }
        else
        {
            entity = DoctorTarget.Create(r.DoctorId, r.BranchId, r.Year, r.Month, r.TargetAmount);
            if (_user.IsAuthenticated) entity.SetCreatedBy(_user.UserId, _user.TenantId);
            _db.DoctorTargets.Add(entity);
        }

        await _db.SaveChangesAsync(ct);
        return CommissionMappings.ToResponse(entity, doctor.FullName);
    }
}

public record UpsertBranchTargetCommand(
    long BranchId,
    int Year,
    int Month,
    decimal TargetAmount
) : IRequest<BranchTargetResponse>;

public class UpsertBranchTargetCommandHandler
    : IRequestHandler<UpsertBranchTargetCommand, BranchTargetResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public UpsertBranchTargetCommandHandler(
        AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    public async Task<BranchTargetResponse> Handle(
        UpsertBranchTargetCommand r, CancellationToken ct)
    {
        if (_tenant.IsBranchLevel && _tenant.BranchId != r.BranchId)
            throw new ForbiddenException("Bu şubeye erişim yetkiniz bulunmuyor.");

        var existing = await _db.BranchTargets
            .FirstOrDefaultAsync(t =>
                t.BranchId == r.BranchId && t.Year == r.Year && t.Month == r.Month, ct);

        BranchTarget entity;
        if (existing != null)
        {
            existing.SetAmount(r.TargetAmount);
            if (_user.IsAuthenticated) existing.SetUpdatedBy(_user.UserId);
            entity = existing;
        }
        else
        {
            entity = BranchTarget.Create(r.BranchId, r.Year, r.Month, r.TargetAmount);
            if (_user.IsAuthenticated) entity.SetCreatedBy(_user.UserId, _user.TenantId);
            _db.BranchTargets.Add(entity);
        }

        await _db.SaveChangesAsync(ct);
        return CommissionMappings.ToResponse(entity);
    }
}
