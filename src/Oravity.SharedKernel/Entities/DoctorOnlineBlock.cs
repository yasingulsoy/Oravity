namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hekim için bloke tarih aralıkları (izin, tatil, kongre) (SPEC §ONLİNE RANDEVU SİSTEMİ §2.1).
/// Bu aralıklarda online slot üretilmez.
/// </summary>
public class DoctorOnlineBlock
{
    public long Id { get; private set; }

    public long DoctorId { get; private set; }
    public User Doctor { get; private set; } = default!;

    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    public DateTime StartDatetime { get; private set; }
    public DateTime EndDatetime { get; private set; }

    /// <summary>Örn: "Yıllık İzin", "Kongre", "Kapalı Gün"</summary>
    public string? Reason { get; private set; }

    public long CreatedBy { get; private set; }
    public User Creator { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private DoctorOnlineBlock() { }

    public static DoctorOnlineBlock Create(
        long doctorId, long branchId,
        DateTime startDatetime, DateTime endDatetime,
        long createdBy, string? reason = null)
    {
        if (endDatetime <= startDatetime)
            throw new ArgumentException("Bitiş zamanı başlangıç zamanından sonra olmalıdır.");

        return new DoctorOnlineBlock
        {
            DoctorId      = doctorId,
            BranchId      = branchId,
            StartDatetime = startDatetime,
            EndDatetime   = endDatetime,
            Reason        = reason,
            CreatedBy     = createdBy,
            CreatedAt     = DateTime.UtcNow
        };
    }
}
