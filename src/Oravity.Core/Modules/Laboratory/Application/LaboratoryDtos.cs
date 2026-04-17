using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Laboratory.Application;

// ─── Laboratory ──────────────────────────────────────────────────────────
public record LaboratoryResponse(
    Guid     PublicId,
    string   Name,
    string?  Code,
    string?  Phone,
    string?  Email,
    string?  Website,
    string?  Country,
    string?  City,
    string?  District,
    string?  Address,
    string?  ContactPerson,
    string?  ContactPhone,
    string?  WorkingDays,
    string?  WorkingHours,
    string?  PaymentTerms,
    int      PaymentDays,
    string?  Notes,
    bool     IsActive,
    int      AssignedBranchCount,
    int      ActiveWorkCount,
    DateTime CreatedAt
);

public record LaboratoryListItemResponse(
    Guid    PublicId,
    string  Name,
    string? Code,
    string? City,
    string? Phone,
    bool    IsActive
);

public record BranchAssignmentResponse(
    Guid   PublicId,
    Guid   BranchPublicId,
    string BranchName,
    int    Priority,
    bool   IsActive
);

public record LaboratoryPriceItemResponse(
    Guid     PublicId,
    string   ItemName,
    string?  ItemCode,
    string?  Description,
    decimal  Price,
    string   Currency,
    string?  PricingType,
    int?     EstimatedDeliveryDays,
    string?  Category,
    DateOnly? ValidFrom,
    DateOnly? ValidUntil,
    bool     IsActive
);

// ─── LaboratoryWork ───────────────────────────────────────────────────────
public record LaboratoryWorkListItemResponse(
    Guid     PublicId,
    string   WorkNo,
    Guid     PatientPublicId,
    string   PatientFullName,
    Guid     DoctorPublicId,
    string   DoctorFullName,
    Guid     LaboratoryPublicId,
    string   LaboratoryName,
    Guid     BranchPublicId,
    string   BranchName,
    string   WorkType,
    string   DeliveryType,
    string?  ToothNumbers,
    string?  ShadeColor,
    string   Status,
    DateTime CreatedAt,
    DateTime? SentToLabAt,
    DateOnly? EstimatedDeliveryDate,
    DateTime? ReceivedFromLabAt,
    DateTime? CompletedAt,
    decimal? TotalCost,
    string?  Currency
);

public record LaboratoryWorkItemResponse(
    Guid    PublicId,
    Guid?   LabPriceItemPublicId,
    string  ItemName,
    int     Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string  Currency,
    string? Notes
);

public record LaboratoryWorkHistoryEntry(
    DateTime  ChangedAt,
    string?   OldStatus,
    string    NewStatus,
    string?   Notes,
    long      ChangedByUserId
);

public record LaboratoryWorkDetailResponse(
    Guid     PublicId,
    string   WorkNo,
    Guid     PatientPublicId,
    string   PatientFullName,
    Guid     DoctorPublicId,
    string   DoctorFullName,
    Guid     LaboratoryPublicId,
    string   LaboratoryName,
    Guid     BranchPublicId,
    string   BranchName,
    Guid?    TreatmentPlanItemPublicId,
    string   WorkType,
    string   DeliveryType,
    string?  ToothNumbers,
    string?  ShadeColor,
    string   Status,
    DateTime? SentToLabAt,
    DateOnly? EstimatedDeliveryDate,
    DateTime? ReceivedFromLabAt,
    DateTime? FittedToPatientAt,
    DateTime? CompletedAt,
    DateTime? ApprovedAt,
    long?    ApprovedByUserId,
    decimal? TotalCost,
    string?  Currency,
    string?  DoctorNotes,
    string?  LabNotes,
    string?  ApprovalNotes,
    string?  Attachments,
    IReadOnlyList<LaboratoryWorkItemResponse>  Items,
    IReadOnlyList<LaboratoryWorkHistoryEntry>  History,
    DateTime CreatedAt
);

public record LabWorkItemInputDto(
    Guid?   LabPriceItemPublicId,   // opsiyonel — null ise freehand
    string  ItemName,
    int     Quantity,
    decimal UnitPrice,
    string  Currency,
    string? Notes
);

public record ApprovalAuthorityResponse(
    Guid    PublicId,
    Guid    UserPublicId,
    string  UserFullName,
    Guid?   BranchPublicId,
    string? BranchName,
    bool    CanApprove,
    bool    CanReject,
    bool    NotificationEnabled
);

internal static class LaboratoryMappings
{
    public static LaboratoryPriceItemResponse ToResponse(LaboratoryPriceItem p)
        => new(p.PublicId, p.ItemName, p.ItemCode, p.Description, p.Price, p.Currency,
               p.PricingType, p.EstimatedDeliveryDays, p.Category,
               p.ValidFrom, p.ValidUntil, p.IsActive);
}
