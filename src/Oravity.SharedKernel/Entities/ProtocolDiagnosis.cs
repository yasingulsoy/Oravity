using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Protokol-ICD bağlantısı — bir protokolde birden fazla tanı kodu bulunabilir.
/// İlk eklenen veya IsPrimary=true olan ana tanıdır.
/// </summary>
public class ProtocolDiagnosis : BaseEntity
{
    public long     ProtocolId { get; private set; }
    public Protocol Protocol   { get; private set; } = default!;

    public long    IcdCodeId { get; private set; }
    public IcdCode IcdCode   { get; private set; } = default!;

    /// <summary>Ana tanı mı? Protokol başına en fazla 1 tane true olmalı.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>Hekim notu — ör. "Sağ alt 6. diş"</summary>
    public string? Note { get; private set; }

    private ProtocolDiagnosis() { }

    public static ProtocolDiagnosis Create(long protocolId, long icdCodeId, bool isPrimary, string? note)
        => new()
        {
            ProtocolId = protocolId,
            IcdCodeId  = icdCodeId,
            IsPrimary  = isPrimary,
            Note       = note?.Trim(),
        };

    public void SetPrimary(bool value) => IsPrimary = value;

    public void UpdateNote(string? note) => Note = note?.Trim();
}
