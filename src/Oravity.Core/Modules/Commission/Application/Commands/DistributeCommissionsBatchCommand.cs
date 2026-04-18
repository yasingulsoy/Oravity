using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Commission.Application.Commands;

/// <summary>
/// Belirli hakediş kayıtlarını (ID listesi) toplu dağıtır.
/// Her birini Distributed işaretler; dağıtım tarihi atılır.
/// </summary>
public record DistributeCommissionsBatchCommand(
    IReadOnlyList<long> CommissionIds
) : IRequest<BatchDistributionResult>;

public record BatchDistributionResult(
    int Distributed,
    int Skipped,
    decimal TotalAmount,
    IReadOnlyList<string> Warnings
);

public class DistributeCommissionsBatchCommandHandler
    : IRequestHandler<DistributeCommissionsBatchCommand, BatchDistributionResult>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public DistributeCommissionsBatchCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<BatchDistributionResult> Handle(
        DistributeCommissionsBatchCommand r, CancellationToken ct)
    {
        if (r.CommissionIds.Count == 0)
            throw new InvalidOperationException("En az bir hakediş seçmelisiniz.");

        var commissions = await _db.DoctorCommissions
            .Where(c => r.CommissionIds.Contains(c.Id))
            .ToListAsync(ct);

        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            commissions = commissions.Where(c => c.BranchId == _tenant.BranchId.Value).ToList();

        var distributed = 0;
        var skipped = 0;
        var total = 0m;
        var warnings = new List<string>();

        foreach (var c in commissions)
        {
            try
            {
                c.Distribute();
                distributed++;
                total += c.NetCommissionAmount;
            }
            catch (InvalidOperationException ex)
            {
                skipped++;
                warnings.Add($"#{c.Id}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(ct);
        return new BatchDistributionResult(distributed, skipped, total, warnings);
    }
}
