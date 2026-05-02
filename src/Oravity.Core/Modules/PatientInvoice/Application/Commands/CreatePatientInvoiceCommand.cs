using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using PatientInvoiceStatus = Oravity.SharedKernel.Entities.PatientInvoiceStatus;

namespace Oravity.Core.Modules.PatientInvoice.Application.Commands;

public record CreatePatientInvoiceCommand(
    long PatientId,
    string InvoiceNo,
    string InvoiceType,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    decimal Amount,
    decimal KdvRate,
    string Currency,
    InvoiceRecipientType RecipientType,
    string RecipientName,
    string? RecipientTcNo,
    string? RecipientVkn,
    string? RecipientTaxOffice,
    IReadOnlyList<long>? TreatmentItemIds,
    string? Notes
) : IRequest<PatientInvoiceResponse>;

public class CreatePatientInvoiceCommandHandler
    : IRequestHandler<CreatePatientInvoiceCommand, PatientInvoiceResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public CreatePatientInvoiceCommandHandler(AppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    public async Task<PatientInvoiceResponse> Handle(
        CreatePatientInvoiceCommand r, CancellationToken ct)
    {
        var branchId = _tenant.BranchId
            ?? throw new ForbiddenException("Fatura için şube bağlamı gereklidir.");

        var patient = await _db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == r.PatientId, ct)
            ?? throw new NotFoundException($"Hasta bulunamadı: {r.PatientId}");

        var duplicateNo = await _db.PatientInvoices.AsNoTracking()
            .AnyAsync(x => x.BranchId == branchId && x.InvoiceNo == r.InvoiceNo.Trim(), ct);
        if (duplicateNo)
            throw new ConflictException($"'{r.InvoiceNo}' numaralı fatura zaten mevcut.");

        // Aynı tedavi kalemi birden fazla aktif faturaya girilemez
        if (r.TreatmentItemIds is { Count: > 0 })
        {
            var existingInvoices = await _db.PatientInvoices
                .Where(i => i.PatientId == r.PatientId
                            && i.Status != PatientInvoiceStatus.Cancelled
                            && i.TreatmentItemIdsJson != null)
                .Select(i => new { i.InvoiceNo, i.TreatmentItemIdsJson })
                .ToListAsync(ct);

            foreach (var inv in existingInvoices)
            {
                var billedIds = ParseIds(inv.TreatmentItemIdsJson);
                var duplicate = r.TreatmentItemIds.FirstOrDefault(id => billedIds.Contains(id));
                if (duplicate != default)
                    throw new ConflictException(
                        $"Seçili tedavi kalemleri '{inv.InvoiceNo}' numaralı aktif faturada zaten mevcut. " +
                        "Önce o faturayı iptal edin.");
            }
        }

        string? itemsJson = r.TreatmentItemIds is { Count: > 0 }
            ? JsonSerializer.Serialize(r.TreatmentItemIds)
            : null;

        var invoice = Oravity.SharedKernel.Entities.PatientInvoice.Create(
            r.PatientId, branchId,
            r.InvoiceNo, r.InvoiceType,
            r.InvoiceDate, r.DueDate,
            r.Amount, r.KdvRate, r.Currency,
            r.RecipientType, r.RecipientName,
            r.RecipientTcNo, r.RecipientVkn, r.RecipientTaxOffice,
            itemsJson, r.Notes);

        if (_user.IsAuthenticated)
            invoice.SetCreatedBy(_user.UserId, _user.TenantId);

        _db.PatientInvoices.Add(invoice);
        await _db.SaveChangesAsync(ct);

        return PatientInvoiceMappings.ToResponse(
            invoice,
            $"{patient.FirstName} {patient.LastName}".Trim());
    }

    private static HashSet<long> ParseIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return [.. JsonSerializer.Deserialize<long[]>(json) ?? []]; }
        catch { return []; }
    }
}
