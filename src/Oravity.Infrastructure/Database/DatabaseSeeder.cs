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
        await SeedInstitutionsAsync(ct);

        await SeedSpecializationsAsync(ct);
        await SeedAppointmentStatusesAsync(ct);
        await SeedAppointmentTypesAsync(ct);

        if (_env.IsDevelopment())
        {
            await SeedPlatformAdminAsync(ct);
            await SeedTestPatientsAsync(ct);
            await SeedDoctorSchedulesAsync(ct);
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

            // Institution
            ("institution",      "view",             false),
            ("institution",      "manage",           false),
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

    // ─── Kurumlar (platform geneli) ───────────────────────────────────────
    private async Task SeedInstitutionsAsync(CancellationToken ct)
    {
        var definitions = new[]
        {
            ("SGK",                "SGK",      "sigorta"),
            ("Acıbadem Sigorta",   "ACIBADEM", "sigorta"),
            ("Allianz Sigorta",    "ALLIANZ",  "sigorta"),
            ("AXA Sigorta",        "AXA",      "sigorta"),
            ("Güneş Sigorta",      "GUNES",    "sigorta"),
            ("Mapfre Sigorta",     "MAPFRE",   "sigorta"),
            ("Türkiye İş Bankası", "ISBANK",   "kurumsal"),
            ("Ziraat Bankası",     "ZIRAAT",   "kurumsal"),
            ("Garanti BBVA",       "GARANTI",  "kurumsal"),
        };

        var existingCodes = await _db.Institutions
            .Where(i => i.CompanyId == null)
            .Select(i => i.Code)
            .ToListAsync(ct);

        var toAdd = definitions
            .Where(d => !existingCodes.Contains(d.Item2))
            .Select(d => Institution.Create(d.Item1, d.Item2, d.Item3, null))
            .ToList();

        if (toAdd.Count == 0)
        {
            _logger.LogDebug("Tüm global kurumlar zaten mevcut, atlanıyor.");
            return;
        }

        await _db.Institutions.AddRangeAsync(toAdd, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("{Count} kurum eklendi.", toAdd.Count);
    }

    // ─── Uzmanlık Alanları ────────────────────────────────────────────────
    private async Task SeedSpecializationsAsync(CancellationToken ct)
    {
        if (await _db.Specializations.AnyAsync(ct))
        {
            _logger.LogDebug("Uzmanlık alanları zaten mevcut, atlanıyor.");
            return;
        }

        var items = new[]
        {
            Specialization.Create("Genel Diş Hekimliği",   "GENERAL",       0),
            Specialization.Create("Ortodonti",              "ORTHODONTICS",  1),
            Specialization.Create("Endodonti",              "ENDODONTICS",   2),
            Specialization.Create("Periodontoloji",         "PERIODONTICS",  3),
            Specialization.Create("Pedodonti",              "PEDODONTICS",   4),
            Specialization.Create("Oral Cerrahi",           "ORAL_SURGERY",  5),
            Specialization.Create("Protez",                 "PROSTHETICS",   6),
            Specialization.Create("Restoratif Diş",         "RESTORATIVE",   7),
            Specialization.Create("Ağız Radyolojisi",       "RADIOLOGY",     8),
            Specialization.Create("Implantoloji",           "IMPLANTOLOGY",  9),
        };

        await _db.Specializations.AddRangeAsync(items, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("{Count} uzmanlık alanı eklendi.", items.Length);
    }

    // ─── Randevu Durumları ────────────────────────────────────────────────
    private async Task SeedAppointmentStatusesAsync(CancellationToken ct)
    {
        if (await _db.AppointmentStatuses.AnyAsync(ct))
        {
            _logger.LogDebug("Randevu durumları zaten mevcut, atlanıyor.");
            return;
        }

        // WellKnownIds sırası: 1-7 ardışık seed, NoShow sonradan ID=8 alır.
        // AppDbContext filter "NOT IN (4, 6, 8)" ile uyumlu olması için
        // LEFT=4, CANCELLED=6, NO_SHOW=8 olacak şekilde 8 kayıt ekliyoruz.
        var items = new[]
        {
            //                  name              code          titleColor  containerColor borderColor  textColor    className      isPatient sort
            MakeStatus("Planlandı",       "PLANNED",     "#3598DC", "#EBF5FB",   "#2980B9",  "#1a1a1a", "cl-blue",    true,  1),
            MakeStatus("Onaylandı",       "CONFIRMED",   "#27AE60", "#EAFAF1",   "#1E8449",  "#1a1a1a", "cl-green",   true,  2),
            MakeStatus("Geldi",           "ARRIVED",     "#16A085", "#E8F8F5",   "#117A65",  "#1a1a1a", "cl-teal",    true,  3),
            MakeStatus("Ayrıldı",         "LEFT",        "#8E44AD", "#F5EEF8",   "#6C3483",  "#1a1a1a", "cl-purple",  true,  4),  // terminal
            MakeStatus("Odada",           "IN_ROOM",     "#F39C12", "#FEF9E7",   "#D68910",  "#1a1a1a", "cl-yellow",  true,  5),
            MakeStatus("İptal",           "CANCELLED",   "#E74C3C", "#FDEDEC",   "#CB4335",  "#ffffff", "cl-red",     true,  6),  // terminal
            MakeStatus("Tamamlandı",      "COMPLETED",   "#7F8C8D", "#F2F3F4",   "#626567",  "#1a1a1a", "cl-gray",    true,  7),
            MakeStatus("Gelmedi",         "NO_SHOW",     "#E67E22", "#FEF5E7",   "#CA6F1E",  "#1a1a1a", "cl-orange",  true,  8),  // terminal
        };

        await _db.AppointmentStatuses.AddRangeAsync(items, ct);
        await _db.SaveChangesAsync(ct);

        // Geçiş kuralları (AllowedNextStatusIds JSON)
        var saved = await _db.AppointmentStatuses.ToListAsync(ct);
        var byCode = saved.ToDictionary(s => s.Code);

        // PLANNED → CONFIRMED, ARRIVED, CANCELLED, NO_SHOW
        byCode["PLANNED"].SetAllowedNextStatusIds(
            $"[{byCode["CONFIRMED"].Id},{byCode["ARRIVED"].Id},{byCode["CANCELLED"].Id},{byCode["NO_SHOW"].Id}]");
        // CONFIRMED → ARRIVED, CANCELLED, NO_SHOW
        byCode["CONFIRMED"].SetAllowedNextStatusIds(
            $"[{byCode["ARRIVED"].Id},{byCode["CANCELLED"].Id},{byCode["NO_SHOW"].Id}]");
        // ARRIVED → IN_ROOM, CANCELLED, NO_SHOW
        byCode["ARRIVED"].SetAllowedNextStatusIds(
            $"[{byCode["IN_ROOM"].Id},{byCode["CANCELLED"].Id},{byCode["NO_SHOW"].Id}]");
        // IN_ROOM → COMPLETED, LEFT
        byCode["IN_ROOM"].SetAllowedNextStatusIds(
            $"[{byCode["COMPLETED"].Id},{byCode["LEFT"].Id}]");
        // Terminal durumlar: geçiş yok

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("{Count} randevu durumu eklendi.", items.Length);

        static AppointmentStatus MakeStatus(
            string name, string code,
            string titleColor, string containerColor, string borderColor, string textColor,
            string className, bool isPatient, int sort)
        {
            var s = AppointmentStatus.Create(name, code, titleColor, containerColor, borderColor, textColor, className, isPatient, sort);
            return s;
        }
    }

    // ─── Randevu Tipleri ──────────────────────────────────────────────────
    private async Task SeedAppointmentTypesAsync(CancellationToken ct)
    {
        if (await _db.AppointmentTypes.AnyAsync(ct))
        {
            _logger.LogDebug("Randevu tipleri zaten mevcut, atlanıyor.");
            return;
        }

        var items = new[]
        {
            // Hasta randevuları
            AppointmentType.Create("Yeni Hasta",      "NEW_PATIENT",    "#3598DC", isPatientAppointment: true,  defaultDurationMinutes: 60, sortOrder: 0),
            AppointmentType.Create("Klinik Hastası",  "RETURNING",      "#27AE60", isPatientAppointment: true,  defaultDurationMinutes: 30, sortOrder: 1),
            AppointmentType.Create("Online Randevu",  "ONLINE",         "#9B59B6", isPatientAppointment: true,  defaultDurationMinutes: 30, sortOrder: 2),
            AppointmentType.Create("Kontrol",         "CHECKUP",        "#16A085", isPatientAppointment: true,  defaultDurationMinutes: 20, sortOrder: 3),
            AppointmentType.Create("Acil",            "EMERGENCY",      "#E74C3C", isPatientAppointment: true,  defaultDurationMinutes: 45, sortOrder: 4),
            // Hekim blokları (hasta randevusu değil)
            AppointmentType.Create("Toplantı",        "MEETING",        "#95A5A6", isPatientAppointment: false, defaultDurationMinutes: 60, sortOrder: 10),
            AppointmentType.Create("Öğle Molası",     "LUNCH_BREAK",    "#BDC3C7", isPatientAppointment: false, defaultDurationMinutes: 60, sortOrder: 11),
            AppointmentType.Create("İzin",            "LEAVE",          "#7F8C8D", isPatientAppointment: false, defaultDurationMinutes: 480, sortOrder: 12),
        };

        await _db.AppointmentTypes.AddRangeAsync(items, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("{Count} randevu tipi eklendi.", items.Length);
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

    // ─── Hekim Takvim Verileri (sadece Development) ───────────────────────
    private async Task SeedDoctorSchedulesAsync(CancellationToken ct)
    {
        // Zaten hekim programı varsa atla
        if (await _db.DoctorSchedules.AnyAsync(ct))
        {
            _logger.LogDebug("Hekim programları zaten mevcut, atlanıyor.");
            return;
        }

        // Demo şirketi bul
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Name == "Demo Diş Kliniği", ct);
        if (company is null)
        {
            _logger.LogWarning("Demo Diş Kliniği bulunamadı, hekim seed atlanıyor.");
            return;
        }

        // Uzmanlık ID'lerini al
        var specs = await _db.Specializations
            .ToDictionaryAsync(s => s.Code, s => s.Id, ct);

        // DOCTOR rol şablonu
        var doctorRole = await _db.RoleTemplates.FirstOrDefaultAsync(r => r.Code == "DOCTOR", ct);
        if (doctorRole is null) return;

        var pw = BCrypt.Net.BCrypt.HashPassword("Doktor123!", workFactor: 12);

        // ── Şubeler ──────────────────────────────────────────────────────────
        var branches = await _db.Branches
            .Where(b => b.CompanyId == company.Id && b.IsActive)
            .ToListAsync(ct);

        Branch GetOrAdd(string name)
        {
            var b = branches.FirstOrDefault(x => x.Name == name);
            if (b is not null) return b;
            b = Branch.Create(name, company.Id);
            _db.Branches.Add(b);
            branches.Add(b);
            return b;
        }

        var anaSube       = GetOrAdd("Ana Şube");
        var kadikoySube   = GetOrAdd("Kadıköy Şube");
        var besiktasSube  = GetOrAdd("Beşiktaş Şube");
        await _db.SaveChangesAsync(ct);

        // ── Doktor kullanıcıları ──────────────────────────────────────────────
        async Task<User> GetOrCreateDoctor(
            string email, string fullName, string title,
            string specCode, string calendarColor, int duration)
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);
            if (u is null)
            {
                u = User.Create(email, fullName, pw);
                await _db.Users.AddAsync(u, ct);
                await _db.SaveChangesAsync(ct);
            }
            u.UpdateDoctorProfile(
                title,
                specs.TryGetValue(specCode, out var sid) ? (int?)sid : null,
                calendarColor,
                duration);
            await _db.SaveChangesAsync(ct);
            return u;
        }

        var aylin  = await GetOrCreateDoctor("aylin.sahin@demo.com",   "Aylin Şahin",    "Dt.",       "GENERAL",      "#4CAF50", 30);
        var kerem  = await GetOrCreateDoctor("kerem.ozdemir@demo.com", "Kerem Özdemir",  "Dt.",       "ORTHODONTICS", "#2196F3", 45);
        var seda   = await GetOrCreateDoctor("seda.yildirim@demo.com", "Seda Yıldırım",  "Uzm. Dt.",  "ENDODONTICS",  "#9C27B0", 60);
        var murat  = await GetOrCreateDoctor("murat.demirkol@demo.com","Murat Demirkol", "Op. Dr.",   "ORAL_SURGERY", "#FF5722", 60);
        var ceren  = await GetOrCreateDoctor("ceren.atak@demo.com",    "Ceren Atak",     "Dt.",       "PEDODONTICS",  "#00BCD4", 30);
        var hakan  = await GetOrCreateDoctor("hakan.arslan@demo.com",  "Hakan Arslan",   "Prof. Dr.", "IMPLANTOLOGY", "#795548", 90);

        // ── UserRoleAssignment ────────────────────────────────────────────────
        var existingAssignments = await _db.UserRoleAssignments
            .Where(a => a.RoleTemplateId == doctorRole.Id)
            .Select(a => new { a.UserId, a.BranchId })
            .ToListAsync(ct);

        void AddAssignment(long userId, long branchId)
        {
            if (existingAssignments.Any(a => a.UserId == userId && a.BranchId == branchId)) return;
            _db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, doctorRole.Id, company.Id, branchId));
        }

        // Aylin: Ana + Kadıköy
        AddAssignment(aylin.Id, anaSube.Id);
        AddAssignment(aylin.Id, kadikoySube.Id);
        // Kerem: Ana Şube
        AddAssignment(kerem.Id, anaSube.Id);
        // Seda: Kadıköy + Beşiktaş
        AddAssignment(seda.Id, kadikoySube.Id);
        AddAssignment(seda.Id, besiktasSube.Id);
        // Murat: Ana + Beşiktaş
        AddAssignment(murat.Id, anaSube.Id);
        AddAssignment(murat.Id, besiktasSube.Id);
        // Ceren: Kadıköy
        AddAssignment(ceren.Id, kadikoySube.Id);
        // Hakan: Ana Şube (yarı zamanlı)
        AddAssignment(hakan.Id, anaSube.Id);

        await _db.SaveChangesAsync(ct);

        // ── DoctorSchedule (1=Pzt…7=Paz) ─────────────────────────────────────
        var schedules = new List<DoctorSchedule>();

        void AddSchedule(User doctor, Branch branch, int day,
            TimeOnly start, TimeOnly end,
            TimeOnly? breakStart = null, TimeOnly? breakEnd = null)
        {
            var s = DoctorSchedule.Create(doctor.Id, branch.Id, day);
            s.Update(true, start, end, breakStart, breakEnd);
            schedules.Add(s);
        }

        var t = (int h, int m = 0) => new TimeOnly(h, m);

        // Aylin Şahin — Ana Şube: Pzt-Cum 09-18, öğle 13-14
        foreach (var d in new[] { 1, 2, 3, 4, 5 })
            AddSchedule(aylin, anaSube, d, t(9), t(18), t(13), t(14));
        // Aylin — Kadıköy: Salı + Perşembe 09-14 (öğle yok)
        AddSchedule(aylin, kadikoySube, 2, t(9), t(14));
        AddSchedule(aylin, kadikoySube, 4, t(9), t(14));

        // Kerem Özdemir — Ana: Pzt, Çar, Cum 09-17, öğle 12:30-13:30
        foreach (var d in new[] { 1, 3, 5 })
            AddSchedule(kerem, anaSube, d, t(9), t(17), t(12, 30), t(13, 30));
        // Kerem — Ana: Salı, Perş 14-19 (akşam seferi)
        AddSchedule(kerem, anaSube, 2, t(14), t(19));
        AddSchedule(kerem, anaSube, 4, t(14), t(19));

        // Seda Yıldırım — Kadıköy: Pzt-Cum 10-19, öğle 13-14
        foreach (var d in new[] { 1, 2, 3, 4, 5 })
            AddSchedule(seda, kadikoySube, d, t(10), t(19), t(13), t(14));
        // Seda — Beşiktaş: Pzt, Çar, Cum 09-15 (sabah mesaisi)
        foreach (var d in new[] { 1, 3, 5 })
            AddSchedule(seda, besiktasSube, d, t(9), t(15));

        // Murat Demirkol — Ana: Pzt, Salı, Perş 09-16
        foreach (var d in new[] { 1, 2, 4 })
            AddSchedule(murat, anaSube, d, t(9), t(16), t(12), t(13));
        // Murat — Beşiktaş: Çar, Cum 09-17
        foreach (var d in new[] { 3, 5 })
            AddSchedule(murat, besiktasSube, d, t(9), t(17), t(12, 30), t(13, 30));
        // Murat — Beşiktaş: Cumartesi 10-14 (nöbet)
        AddSchedule(murat, besiktasSube, 6, t(10), t(14));

        // Ceren Atak — Kadıköy: Pzt-Cum 08-15 (çocuk hekimi, sabahçı, ara yok)
        foreach (var d in new[] { 1, 2, 3, 4, 5 })
            AddSchedule(ceren, kadikoySube, d, t(8), t(15));

        // Prof. Hakan Arslan — Ana: Salı + Perşembe 10-15 (yarı zamanlı uzman)
        AddSchedule(hakan, anaSube, 2, t(10), t(15));
        AddSchedule(hakan, anaSube, 4, t(10), t(15));

        await _db.DoctorSchedules.AddRangeAsync(schedules, ct);
        await _db.SaveChangesAsync(ct);

        // ── DoctorSpecialDay ──────────────────────────────────────────────────
        var today    = DateOnly.FromDateTime(DateTime.UtcNow);

        // Gelecek Pazartesi: Aylin Ana'da kongre (tam izin)
        var nextMonday = today.AddDays((8 - (int)today.DayOfWeek) % 7 + 1);
        _db.DoctorSpecialDays.Add(DoctorSpecialDay.Create(
            aylin.Id, anaSube.Id, nextMonday,
            DoctorSpecialDayType.DayOff, null, null, "Ulusal Diş Hekimliği Kongresi"));

        // Gelecek Cuma: Kerem Ana'da erken çıkış 09-12 (saat değişikliği)
        var nextFriday = today.AddDays((12 - (int)today.DayOfWeek + 7) % 7 + 1);
        _db.DoctorSpecialDays.Add(DoctorSpecialDay.Create(
            kerem.Id, anaSube.Id, nextFriday,
            DoctorSpecialDayType.HourChange, t(9), t(12), "Öğleden sonra sertifika sınavı"));

        // Bu hafta Çarşamba: Hakan Ana'da ekstra mesai (normalde Çar. yok)
        var nextWed = today.AddDays((10 - (int)today.DayOfWeek + 7) % 7 + 1);
        _db.DoctorSpecialDays.Add(DoctorSpecialDay.Create(
            hakan.Id, anaSube.Id, nextWed,
            DoctorSpecialDayType.ExtraWork, t(10), t(17), "Ekstra implant ameliyatı günü"));

        // Gelecek Salı: Seda Beşiktaş'ta tam gün (normalde 09-15, bugün 09-18)
        var nextTue = today.AddDays((9 - (int)today.DayOfWeek + 7) % 7 + 1);
        _db.DoctorSpecialDays.Add(DoctorSpecialDay.Create(
            seda.Id, besiktasSube.Id, nextTue,
            DoctorSpecialDayType.HourChange, t(9), t(18), "Hasta yoğunluğu nedeniyle uzatıldı"));

        await _db.SaveChangesAsync(ct);

        // ── DoctorOnCallSettings ──────────────────────────────────────────────
        // Murat: Cumartesi + Pazar nöbetçi (Beşiktaş)
        var muratOnCall = DoctorOnCallSettings.Create(murat.Id, besiktasSube.Id);
        muratOnCall.Update(false, false, false, false, false, saturday: true, sunday: true,
            OnCallPeriodType.Monthly, today.AddDays(1), today.AddDays(30));
        await _db.DoctorOnCallSettings.AddAsync(muratOnCall, ct);

        // Aylin: Pazartesi nöbetçi (Ana Şube)
        var aylinOnCall = DoctorOnCallSettings.Create(aylin.Id, anaSube.Id);
        aylinOnCall.Update(monday: true, false, false, false, false, false, false,
            OnCallPeriodType.Weekly, today, today.AddDays(7));
        await _db.DoctorOnCallSettings.AddAsync(aylinOnCall, ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Hekim seed tamamlandı: 3 şube, 6 hekim, {Sched} program, {Spec} özel gün, 2 nöbet ayarı.",
            schedules.Count, 4);
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
