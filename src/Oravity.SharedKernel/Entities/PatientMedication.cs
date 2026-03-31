namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hasta ilacı kaydı (SPEC §SAĞLIK BİLGİLERİ — Kullanılan İlaçlar).
/// Anamnez formunun alt listesi. BaseEntity türemez — public_id gerekmez.
/// </summary>
public class PatientMedication
{
    public long Id { get; private set; }

    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    public string DrugName { get; private set; } = default!;
    public string? Dose { get; private set; }
    public string? Frequency { get; private set; }
    public string? Reason { get; private set; }

    /// <summary>Hâlâ kullanıyor mu.</summary>
    public bool IsActive { get; private set; } = true;

    public long AddedBy { get; private set; }
    public User AddedByUser { get; private set; } = default!;
    public DateTime AddedAt { get; private set; }

    private PatientMedication() { }

    public static PatientMedication Create(
        long patientId,
        string drugName,
        long addedBy,
        string? dose = null,
        string? frequency = null,
        string? reason = null)
    {
        return new PatientMedication
        {
            PatientId = patientId,
            DrugName  = drugName,
            Dose      = dose,
            Frequency = frequency,
            Reason    = reason,
            IsActive  = true,
            AddedBy   = addedBy,
            AddedAt   = DateTime.UtcNow
        };
    }

    public void Update(string drugName, string? dose, string? frequency, string? reason)
    {
        DrugName  = drugName;
        Dose      = dose;
        Frequency = frequency;
        Reason    = reason;
    }

    public void Deactivate() => IsActive = false;
    public void Activate()   => IsActive = true;
}
