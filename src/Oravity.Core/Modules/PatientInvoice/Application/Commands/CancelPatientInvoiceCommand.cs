using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.PatientInvoice.Application.Commands;

public record CancelPatientInvoiceCommand(Guid PublicId, string? Reason) : IRequest;

public class CancelPatientInvoiceCommandHandler : IRequestHandler<CancelPatientInvoiceCommand>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public CancelPatientInvoiceCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task Handle(CancelPatientInvoiceCommand r, CancellationToken ct)
    {
        var q = _db.PatientInvoices.Where(i => i.PublicId == r.PublicId);
        if (_tenant.IsBranchLevel && _tenant.BranchId.HasValue)
            q = q.Where(i => i.BranchId == _tenant.BranchId.Value);

        var invoice = await q.FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Hasta faturası bulunamadı: {r.PublicId}");

        invoice.Cancel(r.Reason);
        await _db.SaveChangesAsync(ct);
    }
}
