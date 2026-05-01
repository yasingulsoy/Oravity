using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
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
    string? Notes,
    bool IsEInvoiceTaxpayer = false,
    bool WithholdingApplies = false,
    string? WithholdingCode = null,
    int WithholdingNumerator = 5,
    int WithholdingDenominator = 10
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

        // Aynı tedavi kalemi birden fazla aktif kurum faturasına girilemez
        if (r.TreatmentItemIds is { Count: > 0 })
        {
            var activeStatuses = new[]
            {
                InstitutionInvoiceStatus.Issued,
                InstitutionInvoiceStatus.PartiallyPaid,
                InstitutionInvoiceStatus.Overdue,
                InstitutionInvoiceStatus.InFollowUp
            };

            var existingInvoices = await _db.InstitutionInvoices
                .Where(i => i.PatientId == r.PatientId
                            && i.InstitutionId == r.InstitutionId
                            && activeStatuses.Contains(i.Status)
                            && i.TreatmentItemIdsJson != null)
                .Select(i => new { i.InvoiceNo, i.TreatmentItemIdsJson })
                .ToListAsync(ct);

            foreach (var inv in existingInvoices)
            {
                var billedIds = ParseIds(inv.TreatmentItemIdsJson);
                var duplicate = r.TreatmentItemIds.FirstOrDefault(id => billedIds.Contains(id));
                if (duplicate != default)
                    throw new ConflictException(
                        $"Seçili tedavi kalemleri '{inv.InvoiceNo}' numaralı aktif kurum faturasında zaten mevcut. " +
                        "Önce o faturayı iptal edin.");
            }
        }

        string? itemsJson = r.TreatmentItemIds is { Count: > 0 }
            ? JsonSerializer.Serialize(r.TreatmentItemIds)
            : null;

        var invoice = Oravity.SharedKernel.Entities.InstitutionInvoice.Create(
            r.PatientId, r.InstitutionId, branchId,
            r.InvoiceNo, r.InvoiceDate, r.DueDate,
            r.Amount, r.Currency, itemsJson, r.Notes,
            kdvRate: 0.20m,
            withholdingApplies: r.WithholdingApplies,
            withholdingCode: r.WithholdingCode,
            withholdingNumerator: r.WithholdingNumerator,
            withholdingDenominator: r.WithholdingDenominator);

        if (_user.IsAuthenticated)
            invoice.SetCreatedBy(_user.UserId, _user.TenantId);

        _db.InstitutionInvoices.Add(invoice);
        await _db.SaveChangesAsync(ct);

        return InstitutionInvoiceMappings.ToResponse(
            invoice,
            $"{patient.FirstName} {patient.LastName}".Trim(),
            institution.Name,
            patientTcNumber: null, // create response'unda TC şifre çözümü yok; GET üzerinden okunur
            institutionTaxNumber: institution.TaxNumber,
            institutionTaxOffice: institution.TaxOffice,
            institutionAddress: institution.Address,
            institutionCity: institution.City);
    }

    private static HashSet<long> ParseIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return [.. JsonSerializer.Deserialize<long[]>(json) ?? []]; }
        catch { return []; }
    }
}
