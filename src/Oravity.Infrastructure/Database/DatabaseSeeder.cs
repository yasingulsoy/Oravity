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
        await SeedTranslationsAsync(ct);

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

            // E-Fatura
            ("invoice",          "view",             false),
            ("invoice",          "create",           false),
            ("invoice",          "cancel",           true),

            // Lokalizasyon
            ("translations",     "manage",           false),

            // Audit
            ("audit",            "view",             false),

            // Survey & Complaint
            ("survey",           "manage",           false),
            ("survey",           "view_results",     false),
            ("complaint",        "view",             false),
            ("complaint",        "create",           false),
            ("complaint",        "manage",           false),

            // Commission
            ("commission",       "view",             false),
            ("commission",       "distribute",       true),

            // Report
            ("report",           "view_daily",       false),

            // Notes
            ("note",             "write_patient",    false),
            ("note",             "delete_patient",   false),
            ("patient",          "write_hidden_note",false),
            ("patient",          "upload_document",  false),
            ("patient",          "edit_basic",       false),
            ("anamnesis",        "edit",             false),
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

    // ─── Translations Seed ───────────────────────────────────────────────────
    private async Task SeedTranslationsAsync(CancellationToken ct)
    {
        if (await _db.TranslationKeys.AnyAsync(ct))
        {
            _logger.LogDebug("Çeviri anahtarları zaten mevcut, atlanıyor.");
            return;
        }

        // (key, category, description, tr, en)
        var definitions = new (string Key, string Cat, string Desc, string Tr, string En)[]
        {
            // common
            ("common.save",                         "common",       "Kaydet butonu",                    "Kaydet",       "Save"),
            ("common.cancel",                       "common",       "İptal butonu",                     "İptal",        "Cancel"),
            ("common.delete",                       "common",       "Sil butonu",                       "Sil",          "Delete"),
            ("common.loading",                      "common",       "Yükleniyor göstergesi",            "Yükleniyor...", "Loading..."),
            ("common.error",                        "common",       "Hata mesajı",                      "Hata",         "Error"),
            ("common.success",                      "common",       "Başarı mesajı",                    "Başarılı",     "Success"),
            // patient
            ("patient.title",                       "patient",      "Hasta listesi başlığı",            "Hastalar",     "Patients"),
            ("patient.new",                         "patient",      "Yeni hasta butonu",                "Yeni Hasta",   "New Patient"),
            ("patient.search",                      "patient",      "Hasta arama placeholder",          "Hasta Ara...", "Search Patient..."),
            // appointment
            ("appointment.title",                   "appointment",  "Randevu listesi başlığı",          "Randevular",   "Appointments"),
            ("appointment.status.confirmed",        "appointment",  "Onaylandı durumu",                 "Onaylandı",    "Confirmed"),
            ("appointment.status.cancelled",        "appointment",  "İptal durumu",                     "İptal",        "Cancelled"),
            ("appointment.status.completed",        "appointment",  "Tamamlandı durumu",                "Tamamlandı",   "Completed"),
            // payment
            ("payment.title",                       "payment",      "Ödeme listesi başlığı",            "Ödemeler",     "Payments"),
            ("payment.balance",                     "payment",      "Hasta bakiyesi etiketi",           "Bakiye",       "Balance"),
            // treatment_plan
            ("treatment_plan.title",                "treatment",    "Tedavi planı listesi başlığı",     "Tedavi Planları","Treatment Plans"),
            ("treatment_plan.status.planned",       "treatment",    "Planlandı durumu",                 "Planlandı",    "Planned"),
        };

        // Dil ID'lerini al (tr + en)
        var trLang = await _db.Languages.FirstOrDefaultAsync(l => l.Code == "tr", ct);
        var enLang = await _db.Languages.FirstOrDefaultAsync(l => l.Code == "en", ct);

        if (trLang is null || enLang is null)
        {
            _logger.LogWarning("TR/EN dilleri bulunamadı, çeviri seed atlanıyor.");
            return;
        }

        foreach (var (key, cat, desc, tr, en) in definitions)
        {
            var tKey = TranslationKey.Create(key, cat, desc);
            _db.TranslationKeys.Add(tKey);
            await _db.SaveChangesAsync(ct); // ID alıyoruz

            _db.Translations.Add(Translation.Create(tKey.Id, trLang.Id, tr, isReviewed: true));
            _db.Translations.Add(Translation.Create(tKey.Id, enLang.Id, en, isReviewed: true));
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("{Count} çeviri anahtarı eklendi (TR+EN).", definitions.Length);
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
