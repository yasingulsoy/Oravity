using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// FDI numaralama sistemine göre diş durumu (SPEC §DİŞ ŞEMASI).
/// Her diş için patient bazında tek aktif kayıt tutulur.
/// Durum değişikliği ToothConditionHistory ile tarih sıralı izlenir.
/// </summary>
public enum ToothStatus
{
    Healthy            = 1,  // Sağlıklı
    Decayed            = 2,  // Çürük
    Filled             = 3,  // Dolgulu
    Extracted          = 4,  // Çekilmiş
    Implant            = 5,  // İmplant
    Crown              = 6,  // Kron
    Bridge             = 7,  // Köprü
    RootCanal          = 8,  // Kanal Tedavili
    CongenitallyMissing = 9  // Eksik Doğumsal
}

public class ToothRecord : AuditableEntity
{
    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public long? CompanyId { get; private set; }

    /// <summary>FDI diş numarası: "11"–"48".</summary>
    public string ToothNumber { get; private set; } = default!;

    public ToothStatus Status { get; private set; } = ToothStatus.Healthy;

    /// <summary>Etkilenen yüzeyler: M, D, O, V, L (virgülsüz birleştirme: "MOD").</summary>
    public string? Surfaces { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>Kaydı giren kullanıcı (hekim).</summary>
    public long RecordedBy { get; private set; }
    public User Recorder { get; private set; } = default!;

    public DateTime RecordedAt { get; private set; }

    private ToothRecord() { }

    public static ToothRecord Create(
        long patientId,
        long branchId,
        string toothNumber,
        ToothStatus status,
        long recordedBy,
        long? companyId = null,
        string? surfaces = null,
        string? notes = null)
    {
        return new ToothRecord
        {
            PatientId   = patientId,
            BranchId    = branchId,
            CompanyId   = companyId,
            ToothNumber = toothNumber,
            Status      = status,
            Surfaces    = surfaces,
            Notes       = notes,
            RecordedBy  = recordedBy,
            RecordedAt  = DateTime.UtcNow
        };
    }

    /// <summary>Durumu günceller, eski durumu döndürür (history kaydı için).</summary>
    public ToothStatus UpdateStatus(
        ToothStatus newStatus,
        long updatedBy,
        string? surfaces = null,
        string? notes = null)
    {
        var previous = Status;
        Status     = newStatus;
        Surfaces   = surfaces ?? Surfaces;
        Notes      = notes ?? Notes;
        RecordedBy = updatedBy;
        RecordedAt = DateTime.UtcNow;
        MarkUpdated();
        return previous;
    }
}
