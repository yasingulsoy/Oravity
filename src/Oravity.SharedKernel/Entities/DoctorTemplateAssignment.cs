using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hekim ↔ hakediş şablonu ataması. Bir hekimin aynı anda en fazla bir aktif
/// ataması olabilir; tarih aralığı ile geçmiş atamalar saklanır.
/// </summary>
public class DoctorTemplateAssignment : AuditableEntity
{
    public long DoctorId { get; private set; }
    public User Doctor { get; private set; } = default!;

    public long TemplateId { get; private set; }
    public DoctorCommissionTemplate Template { get; private set; } = default!;

    public DateOnly EffectiveDate { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }

    public bool IsActive { get; private set; } = true;

    private DoctorTemplateAssignment() { }

    public static DoctorTemplateAssignment Create(
        long doctorId,
        long templateId,
        DateOnly effectiveDate,
        DateOnly? expiryDate = null)
    {
        if (expiryDate.HasValue && expiryDate.Value < effectiveDate)
            throw new ArgumentException("Bitiş tarihi başlangıçtan önce olamaz.", nameof(expiryDate));

        return new DoctorTemplateAssignment
        {
            DoctorId      = doctorId,
            TemplateId    = templateId,
            EffectiveDate = effectiveDate,
            ExpiryDate    = expiryDate,
            IsActive      = true
        };
    }

    public void Expire(DateOnly onDate)
    {
        ExpiryDate = onDate;
        IsActive   = false;
        MarkUpdated();
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        MarkUpdated();
    }
}
