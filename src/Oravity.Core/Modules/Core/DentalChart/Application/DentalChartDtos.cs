using Oravity.Core.Modules.Core.DentalChart.Domain.Services;
using Oravity.SharedKernel.Entities;
using ToothRecordEntity = Oravity.SharedKernel.Entities.ToothRecord;
using HistoryEntity = Oravity.SharedKernel.Entities.ToothConditionHistory;

namespace Oravity.Core.Modules.Core.DentalChart.Application;

public record ToothRecordResponse(
    Guid PublicId,
    string ToothNumber,
    string QuadrantLabel,
    string ToothType,
    ToothStatus Status,
    string StatusLabel,
    string? Surfaces,
    string? Notes,
    long RecordedBy,
    DateTime RecordedAt,
    DateTime CreatedAt
);

public record ToothHistoryResponse(
    long Id,
    string ToothNumber,
    ToothStatus? OldStatus,
    string? OldStatusLabel,
    ToothStatus NewStatus,
    string NewStatusLabel,
    long ChangedBy,
    DateTime ChangedAt,
    string? Reason
);

/// <summary>
/// 32 dişin tam FDI haritası. Kayıt olmayan dişler Sağlıklı (default) olarak döner.
/// Quadrant sırasıyla gruplanan response — UI diş şemasını buradan render eder.
/// </summary>
public record DentalChartResponse(
    long PatientId,
    IReadOnlyList<ToothRecordResponse> Teeth,
    int TotalRecorded,
    int TotalHealthy
);

public static class DentalChartMappings
{
    private static readonly IFdiChartService _fdi = new FdiChartService();

    public static ToothRecordResponse ToResponse(ToothRecordEntity r) => new(
        r.PublicId,
        r.ToothNumber,
        _fdi.GetQuadrantLabel(r.ToothNumber),
        _fdi.GetToothType(r.ToothNumber),
        r.Status,
        StatusLabel(r.Status),
        r.Surfaces,
        r.Notes,
        r.RecordedBy,
        r.RecordedAt,
        r.CreatedAt
    );

    public static ToothHistoryResponse ToHistoryResponse(HistoryEntity h) => new(
        h.Id,
        h.ToothNumber,
        h.OldStatus,
        h.OldStatus.HasValue ? StatusLabel(h.OldStatus.Value) : null,
        h.NewStatus,
        StatusLabel(h.NewStatus),
        h.ChangedBy,
        h.ChangedAt,
        h.Reason
    );

    /// <summary>
    /// Kayıt olmayan dişler için default Sağlıklı response üretir.
    /// GetPatientDentalChartQuery'de 32 diş garantisi için kullanılır.
    /// </summary>
    public static ToothRecordResponse DefaultHealthy(string toothNumber) => new(
        Guid.Empty,
        toothNumber,
        _fdi.GetQuadrantLabel(toothNumber),
        _fdi.GetToothType(toothNumber),
        ToothStatus.Healthy,
        StatusLabel(ToothStatus.Healthy),
        null, null, 0,
        DateTime.MinValue, DateTime.MinValue
    );

    public static string StatusLabel(ToothStatus s) => s switch
    {
        ToothStatus.Healthy             => "Sağlıklı",
        ToothStatus.Decayed             => "Çürük",
        ToothStatus.Filled              => "Dolgulu",
        ToothStatus.Extracted           => "Çekilmiş",
        ToothStatus.Implant             => "İmplant",
        ToothStatus.Crown               => "Kron",
        ToothStatus.Bridge              => "Köprü",
        ToothStatus.RootCanal           => "Kanal Tedavili",
        ToothStatus.CongenitallyMissing => "Eksik Doğumsal",
        _                               => s.ToString()
    };
}
