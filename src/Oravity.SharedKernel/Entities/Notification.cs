using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum NotificationType
{
    AppointmentReminder = 1,  // Randevu Hatırlatma
    PatientArrived      = 2,  // Hasta Geldi
    PatientInRoom       = 3,  // Odaya Alındı
    PatientLeaving      = 4,  // Hasta Çıkıyor
    PaymentReminder     = 5,  // Ödeme Hatırlatma
    GeneralInfo         = 6,  // Genel Bilgi
    Urgent              = 7,  // Acil
    DoctorMessage       = 8   // Hekim Mesajı
}

/// <summary>
/// Klinik içi uygulama bildirimi (SPEC §KLİNİK İÇİ BİLDİRİM SİSTEMİ).
/// SignalR üzerinden anlık iletilir ve DB'de kalıcı tutulur.
/// Soft-delete uygulanmaz — bildirim arşivi kalıcıdır.
/// </summary>
public class Notification : BaseEntity
{
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public long? CompanyId { get; private set; }

    /// <summary>Belirli bir kullanıcıya gönderiliyorsa dolu.</summary>
    public long? ToUserId { get; private set; }
    public User? ToUser { get; private set; }

    /// <summary>Rol bazlı dağıtım (1=Resepsiyon, 2=Hekim, 3=Yönetici, 4=Tüm Klinik).</summary>
    public int? ToRole { get; private set; }

    public NotificationType Type { get; private set; }

    public string Title { get; private set; } = default!;
    public string Message { get; private set; } = default!;

    public bool IsRead { get; private set; }
    public bool IsUrgent { get; private set; }

    /// <summary>İlgili kayıt tipi (örn. "Appointment", "Patient", "Payment").</summary>
    public string? RelatedEntityType { get; private set; }
    /// <summary>İlgili kayıt ID'si.</summary>
    public long? RelatedEntityId { get; private set; }

    public DateTime? ReadAt { get; private set; }

    private Notification() { }

    public static Notification Create(
        long branchId,
        NotificationType type,
        string title,
        string message,
        long? toUserId = null,
        int? toRole = null,
        long? companyId = null,
        bool isUrgent = false,
        string? relatedEntityType = null,
        long? relatedEntityId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Bildirim başlığı boş olamaz.", nameof(title));

        return new Notification
        {
            BranchId          = branchId,
            CompanyId         = companyId,
            ToUserId          = toUserId,
            ToRole            = toRole,
            Type              = type,
            Title             = title,
            Message           = message,
            IsRead            = false,
            IsUrgent          = isUrgent,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId   = relatedEntityId
        };
    }

    /// <summary>Bildirimi okundu olarak işaretle.</summary>
    public void MarkRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = DateTime.UtcNow;
        MarkUpdated();
    }
}
