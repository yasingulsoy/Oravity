using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;

namespace Oravity.Backend.Jobs;

/// <summary>
/// Veritabanı yedekleme job'ı.
/// Hangfire üzerinden çalıştırılır.
/// </summary>
public class BackupJob
{
    private readonly IServiceScopeFactory       _scopeFactory;
    private readonly ILogger<BackupJob>         _logger;

    public BackupJob(IServiceScopeFactory scopeFactory, ILogger<BackupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    /// <summary>Hangfire tarafından çağrılan ana metot.</summary>
    public async Task Execute(string backupType = "full", long? companyId = null)
    {
        _logger.LogInformation("BackupJob başlatıldı. Type={BackupType} CompanyId={CompanyId}",
            backupType, companyId);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var log = BackupLog.Start(companyId, backupType);
        db.BackupLogs.Add(log);
        await db.SaveChangesAsync();

        try
        {
            // TODO: Gerçek yedekleme mantığı burada implement edilecek.
            // Örn: pg_dump, MinIO'ya yükleme, checksum hesaplama vb.
            _logger.LogWarning("BackupJob: yedekleme henüz implement edilmedi (stub).");

            await Task.Delay(100); // Placeholder

            log.Complete(
                fileName:        $"backup_{backupType}_{DateTime.UtcNow:yyyyMMddHHmmss}.sql",
                sizeMb:          0,
                storageLocation: "not-implemented",
                checksum:        null);

            await db.SaveChangesAsync();
            _logger.LogInformation("BackupJob tamamlandı (stub). LogId={LogId}", log.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BackupJob hata ile tamamlandı.");
            log.Fail(ex.Message);
            await db.SaveChangesAsync();
            throw;
        }
    }
}
