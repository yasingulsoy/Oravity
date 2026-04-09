using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

public enum TreatmentPlanStatus
{
    Draft     = 1,  // Taslak — hekim hazırlıyor
    Approved  = 2,  // Onaylandı — hasta onayladı
    Completed = 3,  // Tamamlandı
    Cancelled = 4   // İptal
}

/// <summary>
/// Hasta tedavi planı (SPEC §TEDAVİ PLANI MODÜLÜ).
/// Bir plana birden fazla TreatmentPlanItem eklenebilir.
/// </summary>
public class TreatmentPlan : AuditableEntity
{
    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    /// <summary>Planı oluşturan / onaylayan hekim. Hakediş buraya değil yapan hekime gider.</summary>
    public long DoctorId { get; private set; }
    public User Doctor { get; private set; } = default!;

    public string Name { get; private set; } = default!;
    public TreatmentPlanStatus Status { get; private set; } = TreatmentPlanStatus.Draft;
    public string? Notes { get; private set; }

    /// <summary>Bağlı protokol (opsiyonel). Null = protokolsüz plan.</summary>
    public long?     ProtocolId { get; private set; }
    public Protocol? Protocol   { get; private set; }

    public ICollection<TreatmentPlanItem> Items { get; private set; } = [];

    private TreatmentPlan() { }

    public static TreatmentPlan Create(
        long patientId,
        long branchId,
        long doctorId,
        string name,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Plan adı boş olamaz.", nameof(name));

        return new TreatmentPlan
        {
            PatientId = patientId,
            BranchId  = branchId,
            DoctorId  = doctorId,
            Name      = name,
            Notes     = notes,
            Status    = TreatmentPlanStatus.Draft
        };
    }

    /// <summary>
    /// Planı ve tüm item'larını Onaylandı (2) yapar.
    /// Sadece Taslak planlar onaylanabilir.
    /// </summary>
    public void Approve()
    {
        if (Status != TreatmentPlanStatus.Draft)
            throw new InvalidOperationException(
                $"Sadece taslak planlar onaylanabilir. Mevcut durum: {Status}");

        Status = TreatmentPlanStatus.Approved;
        foreach (var item in Items.Where(i => i.Status == TreatmentItemStatus.Planned))
            item.SetApproved();

        MarkUpdated();
    }

    /// <summary>Planı Tamamlandı (3) yapar. Tüm item'ların tamamlanmış olması beklenir.</summary>
    public void Complete()
    {
        if (Status is TreatmentPlanStatus.Completed or TreatmentPlanStatus.Cancelled)
            throw new InvalidOperationException("Bu plan zaten sonlandırılmış.");

        Status = TreatmentPlanStatus.Completed;
        MarkUpdated();
    }

    public void Cancel()
    {
        if (Status is TreatmentPlanStatus.Completed or TreatmentPlanStatus.Cancelled)
            throw new InvalidOperationException("Bu plan zaten sonlandırılmış.");

        Status = TreatmentPlanStatus.Cancelled;
        MarkUpdated();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        MarkUpdated();
    }
}
