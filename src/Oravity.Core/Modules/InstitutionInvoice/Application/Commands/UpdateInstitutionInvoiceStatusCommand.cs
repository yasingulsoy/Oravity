using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.InstitutionInvoice.Application.Commands;

public record CancelInstitutionInvoiceCommand(Guid PublicId, string Reason) : IRequest<Unit>;

public class CancelInstitutionInvoiceCommandHandler : IRequestHandler<CancelInstitutionInvoiceCommand, Unit>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public CancelInstitutionInvoiceCommandHandler(AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db; _tenant = tenant; _user = user;
    }

    public async Task<Unit> Handle(CancelInstitutionInvoiceCommand r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Reason))
            throw new InvalidOperationException("İptal gerekçesi zorunludur.");

        var invoice = await _db.InstitutionInvoices
            .FirstOrDefaultAsync(i => i.PublicId == r.PublicId, ct)
            ?? throw new NotFoundException($"Fatura bulunamadı: {r.PublicId}");

        if (_tenant.IsBranchLevel && invoice.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu faturaya erişim yetkiniz yok.");

        invoice.Cancel(r.Reason); // entity kuralları uygular (Paid/PartiallyPaid → exception)
        if (_user.IsAuthenticated) invoice.SetUpdatedBy(_user.UserId);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public record RejectInstitutionInvoiceCommand(Guid PublicId, string Reason) : IRequest<Unit>;

public class RejectInstitutionInvoiceCommandHandler : IRequestHandler<RejectInstitutionInvoiceCommand, Unit>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public RejectInstitutionInvoiceCommandHandler(AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db; _tenant = tenant; _user = user;
    }

    public async Task<Unit> Handle(RejectInstitutionInvoiceCommand r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Reason))
            throw new InvalidOperationException("Red gerekçesi zorunludur.");

        var invoice = await _db.InstitutionInvoices
            .FirstOrDefaultAsync(i => i.PublicId == r.PublicId, ct)
            ?? throw new NotFoundException($"Fatura bulunamadı: {r.PublicId}");

        if (_tenant.IsBranchLevel && invoice.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu faturaya erişim yetkiniz yok.");

        invoice.MarkRejected(r.Reason);
        if (_user.IsAuthenticated) invoice.SetUpdatedBy(_user.UserId);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public record StartFollowUpCommand(
    Guid PublicId,
    InstitutionInvoiceFollowUp Level,
    DateOnly OnDate,
    DateOnly? NextDate
) : IRequest<Unit>;

public class StartFollowUpCommandHandler : IRequestHandler<StartFollowUpCommand, Unit>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public StartFollowUpCommandHandler(AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db; _tenant = tenant; _user = user;
    }

    public async Task<Unit> Handle(StartFollowUpCommand r, CancellationToken ct)
    {
        var invoice = await _db.InstitutionInvoices
            .FirstOrDefaultAsync(i => i.PublicId == r.PublicId, ct)
            ?? throw new NotFoundException($"Fatura bulunamadı: {r.PublicId}");

        if (_tenant.IsBranchLevel && invoice.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu faturaya erişim yetkiniz yok.");

        invoice.StartFollowUp(r.Level, r.OnDate, r.NextDate);
        if (_user.IsAuthenticated) invoice.SetUpdatedBy(_user.UserId);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public record UpdateInvoiceNotesCommand(Guid PublicId, string? Notes) : IRequest<Unit>;

public class UpdateInvoiceNotesCommandHandler : IRequestHandler<UpdateInvoiceNotesCommand, Unit>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public UpdateInvoiceNotesCommandHandler(AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db; _tenant = tenant; _user = user;
    }

    public async Task<Unit> Handle(UpdateInvoiceNotesCommand r, CancellationToken ct)
    {
        var invoice = await _db.InstitutionInvoices
            .FirstOrDefaultAsync(i => i.PublicId == r.PublicId, ct)
            ?? throw new NotFoundException($"Fatura bulunamadı: {r.PublicId}");

        if (_tenant.IsBranchLevel && invoice.BranchId != _tenant.BranchId)
            throw new ForbiddenException("Bu faturaya erişim yetkiniz yok.");

        invoice.UpdateNotes(r.Notes);
        if (_user.IsAuthenticated) invoice.SetUpdatedBy(_user.UserId);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
