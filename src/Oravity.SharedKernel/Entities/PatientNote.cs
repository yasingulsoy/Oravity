using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum NoteType
{
    General    = 1,  // Genel Not
    Clinical   = 2,  // Klinik Not
    Hidden     = 3,  // Gizli Not (sadece yetkili görür)
    Plan       = 4,  // Plan Notu
    Treatment  = 5,  // Tedavi Notu
    Orthodontic = 6  // Ortodonti Notu
}

/// <summary>
/// Hasta notu (SPEC §NOTLAR SEKMESİ).
/// Gizli notlar (NoteType.Hidden) için ayrı yetki gerekir.
/// Silinmiş notlar deleted_at ile soft-delete edilir (IsDeleted değil — tarih önemli).
/// </summary>
public class PatientNote : BaseEntity
{
    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public NoteType Type { get; private set; }
    public string? Title { get; private set; }
    public string Content { get; private set; } = default!;

    /// <summary>Listenin üstünde sabit gösterilir.</summary>
    public bool IsPinned { get; private set; }

    /// <summary>Gizli not — sadece izinli kullanıcı görebilir.</summary>
    public bool IsHidden { get; private set; }

    /// <summary>İlgili randevu (opsiyonel).</summary>
    public long? AppointmentId { get; private set; }

    public long CreatedBy { get; private set; }
    public User CreatedByUser { get; private set; } = default!;
    public new DateTime CreatedAt { get; private set; }

    public long? UpdatedBy { get; private set; }
    public User? UpdatedByUser { get; private set; }
    public DateTime? NoteUpdatedAt { get; private set; }

    /// <summary>Soft-delete tarihi. Null ise aktif.</summary>
    public DateTime? DeletedAt { get; private set; }

    private PatientNote() { }

    public static PatientNote Create(
        long patientId,
        long branchId,
        NoteType type,
        string content,
        long createdBy,
        string? title = null,
        bool isPinned = false,
        bool isHidden = false,
        long? appointmentId = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Not içeriği boş olamaz.", nameof(content));

        return new PatientNote
        {
            PatientId     = patientId,
            BranchId      = branchId,
            Type          = type,
            Title         = title,
            Content       = content,
            IsPinned      = isPinned,
            IsHidden      = isHidden || type == NoteType.Hidden,
            AppointmentId = appointmentId,
            CreatedBy     = createdBy,
            CreatedAt     = DateTime.UtcNow
        };
    }

    public new void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Pin()   { IsPinned = true;  MarkUpdated(); }
    public void Unpin() { IsPinned = false; MarkUpdated(); }
}
