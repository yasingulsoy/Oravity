using Oravity.SharedKernel.Entities;
using NotifEntity = Oravity.SharedKernel.Entities.Notification;

namespace Oravity.Core.Modules.Notification.Application;

public record NotificationResponse(
    Guid PublicId,
    long BranchId,
    long? ToUserId,
    int? ToRole,
    NotificationType Type,
    string TypeLabel,
    string Title,
    string Message,
    bool IsRead,
    bool IsUrgent,
    string? RelatedEntityType,
    long? RelatedEntityId,
    DateTime? ReadAt,
    DateTime CreatedAt
);

public record PagedNotificationResult(
    IReadOnlyList<NotificationResponse> Items,
    int TotalCount,
    int UnreadCount,
    int Page,
    int PageSize
);

public static class NotificationMappings
{
    public static NotificationResponse ToResponse(NotifEntity n) => new(
        n.PublicId, n.BranchId, n.ToUserId, n.ToRole,
        n.Type, TypeLabel(n.Type),
        n.Title, n.Message, n.IsRead, n.IsUrgent,
        n.RelatedEntityType, n.RelatedEntityId,
        n.ReadAt, n.CreatedAt);

    public static string TypeLabel(NotificationType t) => t switch
    {
        NotificationType.AppointmentReminder => "Randevu Hatırlatma",
        NotificationType.PatientArrived      => "Hasta Geldi",
        NotificationType.PatientInRoom       => "Odaya Alındı",
        NotificationType.PatientLeaving      => "Hasta Çıkıyor",
        NotificationType.PaymentReminder     => "Ödeme Hatırlatma",
        NotificationType.GeneralInfo         => "Genel Bilgi",
        NotificationType.Urgent              => "Acil",
        NotificationType.DoctorMessage       => "Hekim Mesajı",
        _ => t.ToString()
    };
}
