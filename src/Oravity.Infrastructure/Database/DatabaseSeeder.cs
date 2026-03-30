using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oravity.SharedKernel.Entities;

namespace Oravity.Infrastructure.Database;

public class DatabaseSeeder
{
    private readonly AppDbContext _db;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly IHostEnvironment _env;

    public DatabaseSeeder(AppDbContext db, ILogger<DatabaseSeeder> logger, IHostEnvironment env)
    {
        _db = db;
        _logger = logger;
        _env = env;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Database seed başlatılıyor...");

        await SeedLanguagesAsync(ct);
        await SeedVerticalsAsync(ct);
        await SeedRoleTemplatesAsync(ct);
        await SeedPermissionsAsync(ct);

        if (_env.IsDevelopment())
            await SeedPlatformAdminAsync(ct);

        _logger.LogInformation("Database seed tamamlandı.");
    }

    // ─── Languages ────────────────────────────────────────────────────────
    private async Task SeedLanguagesAsync(CancellationToken ct)
    {
        if (await _db.Languages.AnyAsync(ct))
        {
            _logger.LogDebug("Languages zaten mevcut, atlanıyor.");
            return;
        }

        var languages = new[]
        {
            Language.Create("tr", "Türkçe",   "Türkçe",    "ltr", "🇹🇷", isDefault: true,  sortOrder: 0),
            Language.Create("en", "İngilizce","English",   "ltr", "🇬🇧", isDefault: false, sortOrder: 1),
            Language.Create("ar", "Arapça",   "العربية",   "rtl", "🇸🇦", isDefault: false, sortOrder: 2),
            Language.Create("ru", "Rusça",    "Русский",   "ltr", "🇷🇺", isDefault: false, sortOrder: 3),
            Language.Create("de", "Almanca",  "Deutsch",   "ltr", "🇩🇪", isDefault: false, sortOrder: 4),
        };

        await _db.Languages.AddRangeAsync(languages, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("{Count} dil eklendi.", languages.Length);
    }

    // ─── Verticals ────────────────────────────────────────────────────────
    private async Task SeedVerticalsAsync(CancellationToken ct)
    {
        if (await _db.Verticals.AnyAsync(v => v.Code == "DENTAL", ct))
        {
            _logger.LogDebug("DENTAL vertical zaten mevcut, atlanıyor.");
            return;
        }

        var dental = Vertical.Create(
            code: "DENTAL",
            name: "Diş Hekimliği",
            hasBodyChart: true,
            bodyChartType: "DENTAL_FDI",
            defaultModules: ["CORE", "FINANCE", "APPOINTMENT", "TREATMENT"],
            providerLabel: "Hekim",
            patientLabel: "Hasta",
            treatmentLabel: "Tedavi",
            requiresKts: true,
            isActive: true,
            sortOrder: 0
        );

        await _db.Verticals.AddAsync(dental, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DENTAL vertical eklendi.");
    }

    // ─── Role Templates ───────────────────────────────────────────────────
    private async Task SeedRoleTemplatesAsync(CancellationToken ct)
    {
        var definitions = new[]
        {
            ("BRANCH_MANAGER", "Şube Yöneticisi",  "Şube içindeki tüm işlemleri yönetir"),
            ("DOCTOR",         "Hekim",             "Klinik muayene ve tedavi işlemlerini yürütür"),
            ("ASSISTANT",      "Diş Asistanı",      "Hekime yardımcı klinik personel"),
            ("RECEPTIONIST",   "Resepsiyonist",     "Randevu ve hasta kayıt işlemlerini yönetir"),
            ("ACCOUNTANT",     "Muhasebeci",        "Mali işlemler ve raporlama"),
            ("READONLY",       "Salt Okunur",       "Yalnızca görüntüleme yetkisi"),
        };

        var existingCodes = await _db.RoleTemplates
            .Select(r => r.Code)
            .ToListAsync(ct);

        var toAdd = definitions
            .Where(d => !existingCodes.Contains(d.Item1))
            .Select(d => RoleTemplate.Create(d.Item1, d.Item2, d.Item3))
            .ToList();

        if (toAdd.Count == 0)
        {
            _logger.LogDebug("Tüm rol şablonları zaten mevcut, atlanıyor.");
            return;
        }

        await _db.RoleTemplates.AddRangeAsync(toAdd, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("{Count} rol şablonu eklendi.", toAdd.Count);
    }

    // ─── Permissions ──────────────────────────────────────────────────────
    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        var definitions = new (string Resource, string Action, bool IsDangerous)[]
        {
            // Patient
            ("patient",          "view",             false),
            ("patient",          "create",           false),
            ("patient",          "edit",             false),
            ("patient",          "delete",           true),

            // Appointment
            ("appointment",      "view",             false),
            ("appointment",      "create",           false),
            ("appointment",      "edit",             false),
            ("appointment",      "cancel",           false),
            ("appointment",      "delete",           true),

            // Treatment Plan
            ("treatment_plan",   "view",             false),
            ("treatment_plan",   "create",           false),
            ("treatment_plan",   "edit",             false),
            ("treatment_plan",   "complete",         false),
            ("treatment_plan",   "delete_planned",   true),
            ("treatment_plan",   "delete_completed", true),

            // Payment
            ("payment",          "view",             false),
            ("payment",          "create",           false),
            ("payment",          "delete",           true),
            ("payment",          "refund",           true),

            // Report
            ("report",           "view",             false),
            ("report",           "export",           false),

            // Settings
            ("settings",         "view",             false),
            ("settings",         "edit_general",     false),
        };

        var existingCodes = await _db.Permissions
            .Select(p => p.Code)
            .ToListAsync(ct);

        var toAdd = definitions
            .Where(d => !existingCodes.Contains($"{d.Resource}.{d.Action}"))
            .Select(d => Permission.Create(d.Resource, d.Action, d.IsDangerous))
            .ToList();

        if (toAdd.Count == 0)
        {
            _logger.LogDebug("Tüm izinler zaten mevcut, atlanıyor.");
            return;
        }

        await _db.Permissions.AddRangeAsync(toAdd, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("{Count} izin eklendi.", toAdd.Count);
    }

    // ─── Platform Admin (sadece Development) ──────────────────────────────
    private async Task SeedPlatformAdminAsync(CancellationToken ct)
    {
        const string adminEmail = "admin@oravity.com";

        if (await _db.Users.AnyAsync(u => u.Email == adminEmail, ct))
        {
            _logger.LogDebug("Platform admin zaten mevcut, atlanıyor.");
            return;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!", workFactor: 12);

        var admin = User.Create(
            email: adminEmail,
            fullName: "Platform Admin",
            passwordHash: passwordHash,
            isPlatformAdmin: true
        );

        await _db.Users.AddAsync(admin, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Platform admin kullanıcısı oluşturuldu: {Email}", adminEmail);
    }
}

// ─── Extension ────────────────────────────────────────────────────────────────
public static class DatabaseSeederExtensions
{
    public static void AddDatabaseSeeder(this IServiceCollection services)
    {
        services.AddScoped<DatabaseSeeder>();
    }

    public static async Task SeedDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }
}
