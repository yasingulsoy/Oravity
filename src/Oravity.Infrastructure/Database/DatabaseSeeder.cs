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

        await SeedCitizenshipTypesAsync(ct);
        await SeedReferralSourcesAsync(ct);

        if (_env.IsDevelopment())
        {
            await SeedPlatformAdminAsync(ct);
            await SeedTestPatientsAsync(ct);
        }

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

    // ─── Vatandaşlık Tipleri ──────────────────────────────────────────────
    private async Task SeedCitizenshipTypesAsync(CancellationToken ct)
    {
        if (await _db.CitizenshipTypes.AnyAsync(ct))
        {
            _logger.LogDebug("Vatandaşlık tipleri zaten mevcut, atlanıyor.");
            return;
        }

        var items = new[]
        {
            CitizenshipType.Create("Yurtiçi Türk Hasta",    "DOMESTIC_TURKISH",  0),
            CitizenshipType.Create("Yurtiçi Yabancı Hasta", "DOMESTIC_FOREIGN",  1),
            CitizenshipType.Create("Yurtdışı Hasta",         "INTERNATIONAL",     2),
        };

        await _db.CitizenshipTypes.AddRangeAsync(items, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("{Count} vatandaşlık tipi eklendi.", items.Length);
    }

    // ─── Geliş Şekilleri ──────────────────────────────────────────────────
    private async Task SeedReferralSourcesAsync(CancellationToken ct)
    {
        if (await _db.ReferralSources.AnyAsync(ct))
        {
            _logger.LogDebug("Geliş şekilleri zaten mevcut, atlanıyor.");
            return;
        }

        var items = new[]
        {
            ReferralSource.Create("Anlaşmalı Kurum",  "INSTITUTION",   0),
            ReferralSource.Create("Tavsiye",           "REFERRAL",      1),
            ReferralSource.Create("İnternet",          "INTERNET",      2),
            ReferralSource.Create("Sosyal Medya",      "SOCIAL_MEDIA",  3),
            ReferralSource.Create("TV Reklamı",        "TV",            4),
            ReferralSource.Create("Tekrar Gelen",      "RETURNING",     5),
            ReferralSource.Create("Diğer",             "OTHER",         6),
        };

        await _db.ReferralSources.AddRangeAsync(items, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("{Count} geliş şekli eklendi.", items.Length);
    }

    // ─── Test Hastaları (sadece Development) ─────────────────────────────
    private async Task SeedTestPatientsAsync(CancellationToken ct)
    {
        const string demoCompanyName = "Demo Diş Kliniği";

        // Demo şirket — yoksa oluştur
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Name == demoCompanyName, ct);
        if (company is null)
        {
            var dental = await _db.Verticals.FirstOrDefaultAsync(v => v.Code == "DENTAL", ct);
            if (dental is null)
            {
                _logger.LogWarning("DENTAL vertical bulunamadı, test hastaları atlanıyor.");
                return;
            }

            company = Company.Create(demoCompanyName, dental.Id);
            company.SetSubscription(DateTime.UtcNow.AddYears(1));
            await _db.Companies.AddAsync(company, ct);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Demo şirket oluşturuldu: {Name}", demoCompanyName);
        }

        // Demo şube — yoksa oluştur
        var branch = await _db.Branches.FirstOrDefaultAsync(b => b.CompanyId == company.Id, ct);
        if (branch is null)
        {
            branch = Branch.Create("Ana Şube", company.Id);
            await _db.Branches.AddAsync(branch, ct);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Demo şube oluşturuldu.");
        }

        // Zaten hasta varsa atla
        if (await _db.Patients.AnyAsync(p => p.BranchId == branch.Id, ct))
        {
            _logger.LogDebug("Test hastaları zaten mevcut, atlanıyor.");
            return;
        }

        var patients = new[]
        {
            Patient.Create(branch.Id, "Ayşe",    "Kaya",        "5321234567",  "ayse.kaya@example.com",      new DateOnly(1985, 3, 15), "female", address: "Kadıköy, İstanbul"),
            Patient.Create(branch.Id, "Mehmet",  "Yılmaz",      "5337654321",  "mehmet.yilmaz@example.com",  new DateOnly(1978, 7, 22), "male",   address: "Çankaya, Ankara"),
            Patient.Create(branch.Id, "Fatma",   "Demir",       "5441122334",  "fatma.demir@example.com",    new DateOnly(1992, 11, 5), "female", bloodType: "A+"),
            Patient.Create(branch.Id, "Ali",     "Çelik",       "5069988776",  null,                         new DateOnly(1965, 1, 30), "male",   bloodType: "0+"),
            Patient.Create(branch.Id, "Zeynep",  "Şahin",       "5554433221",  "zeynep.sahin@example.com",   new DateOnly(1999, 6, 18), "female"),
            Patient.Create(branch.Id, "Mustafa", "Arslan",      "5323344556",  "mustafa.arslan@example.com", new DateOnly(1970, 9, 3),  "male",   address: "Bornova, İzmir"),
            Patient.Create(branch.Id, "Emine",   "Doğan",       "5067788990",  null,                         new DateOnly(1988, 4, 25), "female", bloodType: "B+"),
            Patient.Create(branch.Id, "Hasan",   "Kurt",        "5449900112",  "hasan.kurt@example.com",     new DateOnly(1955, 12, 10),"male"),
            Patient.Create(branch.Id, "Hatice",  "Öztürk",      "5322211334",  "hatice.ozturk@example.com",  new DateOnly(2001, 2, 14), "female", bloodType: "AB+"),
            Patient.Create(branch.Id, "İbrahim", "Aydın",       "5558877665",  null,                         new DateOnly(1982, 8, 7),  "male",   address: "Nilüfer, Bursa"),
            Patient.Create(branch.Id, "Merve",   "Koç",         "5071234000",  "merve.koc@example.com",      new DateOnly(1995, 5, 21), "female", bloodType: "A-"),
            Patient.Create(branch.Id, "Ömer",    "Yıldız",      "5335550011",  "omer.yildiz@example.com",    new DateOnly(1973, 10, 16),"male"),
            Patient.Create(branch.Id, "Selin",   "Güneş",       "5463322110",  "selin.gunes@example.com",    new DateOnly(2003, 3, 8),  "female"),
            Patient.Create(branch.Id, "Burak",   "Aksoy",       "5076543210",  null,                         new DateOnly(1990, 7, 29), "male",   bloodType: "B-"),
            Patient.Create(branch.Id, "Büşra",   "Polat",       "5327890123",  "busra.polat@example.com",    new DateOnly(1997, 1, 11), "female", address: "Şişli, İstanbul"),
        };

        await _db.Patients.AddRangeAsync(patients, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("{Count} test hastası eklendi (şube: {Branch}).", patients.Length, branch.Name);
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
