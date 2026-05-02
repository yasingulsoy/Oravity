using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.PatientInvoice.Application;

public record PatientInvoiceResponse(
    Guid PublicId,
    long Id,
    long PatientId,
    string? PatientName,
    long BranchId,
    string InvoiceNo,
    string InvoiceType,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    decimal Amount,
    decimal KdvRate,
    decimal KdvAmount,
    decimal TotalAmount,
    string Currency,
    PatientInvoiceStatus Status,
    string StatusLabel,
    decimal PaidAmount,
    decimal RemainingAmount,
    InvoiceRecipientType RecipientType,
    string RecipientName,
    string? RecipientTcNo,
    string? RecipientVkn,
    string? RecipientTaxOffice,
    string? TreatmentItemIdsJson,
    string? Notes,
    string? ExternalUuid,
    string? IntegratorStatus,
    DateTime CreatedAt
);

public static class PatientInvoiceMappings
{
    public static PatientInvoiceResponse ToResponse(
        Oravity.SharedKernel.Entities.PatientInvoice i,
        string? patientName)
        => new(
            i.PublicId, i.Id, i.PatientId, patientName,
            i.BranchId, i.InvoiceNo, i.InvoiceType,
            i.InvoiceDate, i.DueDate,
            i.Amount, i.KdvRate, i.KdvAmount, i.TotalAmount, i.Currency,
            i.Status, StatusLabel(i.Status),
            i.PaidAmount, Math.Max(0, i.TotalAmount - i.PaidAmount),
            i.RecipientType,
            i.RecipientName, i.RecipientTcNo, i.RecipientVkn, i.RecipientTaxOffice,
            i.TreatmentItemIdsJson, i.Notes,
            i.ExternalUuid, i.IntegratorStatus,
            i.CreatedAt);

    public static string StatusLabel(PatientInvoiceStatus s) => s switch
    {
        PatientInvoiceStatus.Issued        => "Kesildi",
        PatientInvoiceStatus.Paid          => "Ödendi",
        PatientInvoiceStatus.PartiallyPaid => "Kısmi Ödendi",
        PatientInvoiceStatus.Cancelled     => "İptal",
        _ => s.ToString()
    };
}
