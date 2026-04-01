namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Şikayet üzerindeki not/aktivite kaydı.
/// is_internal=true → sadece personel görür.
/// is_internal=false → hasta bilgilendirme mesajı olarak da kullanılabilir.
/// </summary>
public class ComplaintNote
{
    public long Id { get; private set; }

    public long ComplaintId { get; private set; }
    public Complaint Complaint { get; private set; } = default!;

    public string Note { get; private set; } = default!;

    /// <summary>true = iç not (personel görür), false = hastaya da gösterilebilir.</summary>
    public bool IsInternal { get; private set; } = true;

    public long CreatedBy { get; private set; }
    public User Creator { get; private set; } = default!;

    public DateTime CreatedAt { get; private set; }

    private ComplaintNote() { }

    public static ComplaintNote Create(
        long complaintId, string note, long createdBy, bool isInternal = true)
    {
        return new ComplaintNote
        {
            ComplaintId = complaintId,
            Note        = note,
            IsInternal  = isInternal,
            CreatedBy   = createdBy,
            CreatedAt   = DateTime.UtcNow
        };
    }
}
