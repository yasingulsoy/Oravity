using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.InstitutionInvoice.Application;

public record InstitutionInvoiceResponse(
    Guid PublicId,
    long Id,
    long PatientId,
    string? PatientName,
    string? PatientTcNumber,   // Çözülmüş TC Kimlik No
    long InstitutionId,
    string InstitutionName,
    string? InstitutionTaxNumber,
    string? InstitutionTaxOffice,
    string? InstitutionAddress,
    string? InstitutionCity,
    long BranchId,
    string InvoiceNo,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    decimal Amount,           // Matrah (KDV hariç)
    decimal KdvAmount,
    decimal NetPayableAmount, // Kurumun ödeyeceği net tutar (brüt - tevkifat)
    decimal WithholdingAmount,
    string Currency,
    InstitutionInvoiceStatus Status,
    string StatusLabel,
    decimal PaidAmount,
    decimal RemainingAmount,
    DateOnly? PaymentDate,
    string? PaymentReferenceNo,
    string? TreatmentItemIdsJson,
    InstitutionInvoiceFollowUp FollowUpStatus,
    string FollowUpStatusLabel,
    DateOnly? LastFollowUpDate,
    DateOnly? NextFollowUpDate,
    string? Notes,
    DateTime CreatedAt
);

public record InstitutionPaymentResponse(
    Guid PublicId,
    long Id,
    long InvoiceId,
    long PatientId,
    long InstitutionId,
    decimal Amount,
    string Currency,
    DateOnly PaymentDate,
    InstitutionPaymentMethod Method,
    string MethodLabel,
    string? ReferenceNo,
    string? BankAccountPublicId,
    string? Notes,
    bool IsCancelled,
    DateTime CreatedAt
);

public static class InstitutionInvoiceMappings
{
    public static InstitutionInvoiceResponse ToResponse(
        Oravity.SharedKernel.Entities.InstitutionInvoice i,
        string? patientName,
        string institutionName,
        string? patientTcNumber = null,
        string? institutionTaxNumber = null,
        string? institutionTaxOffice = null,
        string? institutionAddress = null,
        string? institutionCity = null)
        => new(
            i.PublicId, i.Id, i.PatientId, patientName, patientTcNumber,
            i.InstitutionId, institutionName,
            institutionTaxNumber, institutionTaxOffice, institutionAddress, institutionCity,
            i.BranchId,
            i.InvoiceNo, i.InvoiceDate, i.DueDate,
            i.Amount, i.KdvAmount, i.NetPayableAmount, i.WithholdingAmount, i.Currency,
            i.Status, StatusLabel(i.Status),
            i.PaidAmount, Math.Max(0, i.NetPayableAmount - i.PaidAmount),
            i.PaymentDate, i.PaymentReferenceNo,
            i.TreatmentItemIdsJson,
            i.FollowUpStatus, FollowUpLabel(i.FollowUpStatus),
            i.LastFollowUpDate, i.NextFollowUpDate,
            i.Notes, i.CreatedAt);

    public static InstitutionPaymentResponse ToResponse(
        Oravity.SharedKernel.Entities.InstitutionPayment p)
        => new(p.PublicId, p.Id, p.InvoiceId, p.PatientId, p.InstitutionId,
               p.Amount, p.Currency, p.PaymentDate,
               p.Method, MethodLabel(p.Method),
               p.ReferenceNo, p.BankAccountPublicId, p.Notes, p.IsCancelled, p.CreatedAt);

    public static string StatusLabel(InstitutionInvoiceStatus s) => s switch
    {
        InstitutionInvoiceStatus.Issued        => "Kesildi",
        InstitutionInvoiceStatus.Paid          => "Ödendi",
        InstitutionInvoiceStatus.PartiallyPaid => "Kısmi Ödendi",
        InstitutionInvoiceStatus.Rejected      => "Reddedildi",
        InstitutionInvoiceStatus.Overdue       => "Vadesi Geçti",
        InstitutionInvoiceStatus.InFollowUp    => "Takipte",
        InstitutionInvoiceStatus.Cancelled     => "İptal Edildi",
        _ => s.ToString()
    };

    public static string FollowUpLabel(InstitutionInvoiceFollowUp s) => s switch
    {
        InstitutionInvoiceFollowUp.None           => "Yok",
        InstitutionInvoiceFollowUp.FirstReminder  => "1. Hatırlatma",
        InstitutionInvoiceFollowUp.SecondReminder => "2. Hatırlatma",
        InstitutionInvoiceFollowUp.Legal          => "Yasal Takip",
        _ => s.ToString()
    };

    public static string MethodLabel(InstitutionPaymentMethod m) => m switch
    {
        InstitutionPaymentMethod.BankTransfer => "Havale/EFT",
        InstitutionPaymentMethod.Check        => "Çek",
        InstitutionPaymentMethod.Other        => "Diğer",
        _ => m.ToString()
    };
}
