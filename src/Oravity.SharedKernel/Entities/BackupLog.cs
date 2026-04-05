using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Yedekleme işlemi kaydı.
/// </summary>
public class BackupLog : BaseEntity
{
    /// <summary>Null ise sistem geneli yedek, dolu ise şirkete özel.</summary>
    public long? CompanyId { get; private set; }

    /// <summary>Yedek tipi: "full" | "incremental" | "schema"</summary>
    public string BackupType { get; private set; } = default!;

    public string? FileName { get; private set; }
    public decimal? FileSizeMb { get; private set; }
    public string? StorageLocation { get; private set; }
    public string? Checksum { get; private set; }

    /// <summary>Durum: "started" | "completed" | "failed"</summary>
    public string Status { get; private set; } = "started";

    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int? DurationSeconds { get; private set; }
    public string? ErrorMessage { get; private set; }

    public DateTime? RestoreTestedAt { get; private set; }
    public bool? RestoreSuccess { get; private set; }

    private BackupLog() { }

    public static BackupLog Start(long? companyId, string backupType)
    {
        if (string.IsNullOrWhiteSpace(backupType))
            throw new ArgumentException("Yedek tipi boş olamaz.", nameof(backupType));

        return new BackupLog
        {
            CompanyId  = companyId,
            BackupType = backupType,
            Status     = "started",
            StartedAt  = DateTime.UtcNow
        };
    }

    public void Complete(string fileName, decimal sizeMb, string storageLocation, string? checksum)
    {
        FileName        = fileName;
        FileSizeMb      = sizeMb;
        StorageLocation = storageLocation;
        Checksum        = checksum;
        Status          = "completed";
        CompletedAt     = DateTime.UtcNow;
        DurationSeconds = (int)(CompletedAt.Value - StartedAt).TotalSeconds;
        MarkUpdated();
    }

    public void Fail(string errorMessage)
    {
        Status          = "failed";
        ErrorMessage    = errorMessage;
        CompletedAt     = DateTime.UtcNow;
        DurationSeconds = (int)(CompletedAt.Value - StartedAt).TotalSeconds;
        MarkUpdated();
    }

    public void RecordRestoreTest(bool success)
    {
        RestoreTestedAt = DateTime.UtcNow;
        RestoreSuccess  = success;
        MarkUpdated();
    }
}
