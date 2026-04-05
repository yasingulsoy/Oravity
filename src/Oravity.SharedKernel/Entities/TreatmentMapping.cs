using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// İç tedavi kataloğu ile referans fiyat listesi arasındaki eşleştirme.
/// </summary>
public class TreatmentMapping : BaseEntity
{
    public long InternalTreatmentId { get; private set; }
    public Treatment InternalTreatment { get; private set; } = default!;

    public long ReferenceListId { get; private set; }
    public ReferencePriceList ReferenceList { get; private set; } = default!;

    public string ReferenceCode { get; private set; } = default!;

    /// <summary>Eşleştirme kalitesi: "exact" | "partial" | "approximate"</summary>
    public string? MappingQuality { get; private set; }

    public string? Notes { get; private set; }

    private TreatmentMapping() { }

    public static TreatmentMapping Create(
        long internalTreatmentId,
        long referenceListId,
        string referenceCode,
        string? mappingQuality,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(referenceCode))
            throw new ArgumentException("Referans kodu boş olamaz.", nameof(referenceCode));

        return new TreatmentMapping
        {
            InternalTreatmentId = internalTreatmentId,
            ReferenceListId     = referenceListId,
            ReferenceCode       = referenceCode.Trim().ToUpperInvariant(),
            MappingQuality      = mappingQuality,
            Notes               = notes
        };
    }
}
