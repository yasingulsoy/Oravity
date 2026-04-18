using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.InstitutionInvoice.Application.Commands;

public record CreateInstitutionInvoiceCommand(
    long PatientId,
    long InstitutionId,
    string InvoiceNo,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    decimal Amount,
    string Currency,
    IReadOnlyList<long>? TreatmentItemIds,
    string? Notes
) : IRequest<InstitutionInvoiceResponse>;

public class CreateInstitutionInvoiceCommandHandler
    : IRequestHandler<CreateInstitutionInvoiceCommand, InstitutionInvoiceResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public CreateInstitutionInvoiceCommandHandler(
        AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    public async Task<InstitutionInvoiceResponse> Handle(
        CreateInstitutionInvoiceCommand r, CancellationToken ct)
    {
        var branchId = _tenant.BranchId
            ?? throw new ForbiddenException("Fatura için şube bağlamı gereklidir.");

        var patient = await _db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == r.PatientId, ct)
            ?? throw new NotFoundException($"Hasta bulunamadı: {r.PatientId}");

        var institution = await _db.Institutions.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == r.InstitutionId, ct)
            ?? throw new NotFoundException($"Kurum bulunamadı: {r.InstitutionId}");

        var duplicateNo = await _db.InstitutionInvoices.AsNoTracking()
            .AnyAsync(x => x.BranchId == branchId && x.InvoiceNo == r.InvoiceNo.Trim(), ct);
        if (duplicateNo)
            throw new ConflictException($"'{r.InvoiceNo}' numaralı fatura zaten var.");

        string? itemsJson = r.TreatmentItemIds is { Count: > 0 }
            ? JsonSerializer.Serialize(r.TreatmentItemIds)
            : null;

        var invoice = Oravity.SharedKernel.Entities.InstitutionInvoice.Create(
            r.PatientId, r.InstitutionId, branchId,
            r.InvoiceNo, r.InvoiceDate, r.DueDate,
            r.Amount, r.Currency, itemsJson, r.Notes);

        if (_user.IsAuthenticated)
            invoice.SetCreatedBy(_user.UserId, _user.TenantId);

        _db.InstitutionInvoices.Add(invoice);
        await _db.SaveChangesAsync(ct);

        return InstitutionInvoiceMappings.ToResponse(
            invoice,
            $"{patient.FirstName} {patient.LastName}".Trim(),
            institution.Name);
    }
}
