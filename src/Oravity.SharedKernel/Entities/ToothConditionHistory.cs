namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Diş durum değişikliği geçmiş kaydı (SPEC §DİŞ ŞEMASI — tarih izleme).
/// Append-only: güncellenmez ve soft-delete uygulanmaz.
/// BaseEntity türemez — public_id / is_deleted gerekmez.
/// </summary>
public class ToothConditionHistory
{
    public long Id { get; private set; }

    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    /// <summary>FDI diş numarası: "11"–"48".</summary>
    public string ToothNumber { get; private set; } = default!;

    public ToothStatus? OldStatus { get; private set; }
    public ToothStatus NewStatus { get; private set; }

    public long ChangedBy { get; private set; }
    public User Changer { get; private set; } = default!;

    public DateTime ChangedAt { get; private set; }

    public string? Reason { get; private set; }

    private ToothConditionHistory() { }

    public static ToothConditionHistory Create(
        long patientId,
        string toothNumber,
        ToothStatus newStatus,
        long changedBy,
        ToothStatus? oldStatus = null,
        string? reason = null)
    {
        return new ToothConditionHistory
        {
            PatientId   = patientId,
            ToothNumber = toothNumber,
            OldStatus   = oldStatus,
            NewStatus   = newStatus,
            ChangedBy   = changedBy,
            ChangedAt   = DateTime.UtcNow,
            Reason      = reason
        };
    }
}
