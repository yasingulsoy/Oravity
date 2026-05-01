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
        await SeedRoleTemplatePermissionsAsync(ct);
        await SeedTranslationsAsync(ct);

        await SeedCitizenshipTypesAsync(ct);
        await SeedReferralSourcesAsync(ct);
        await SeedInstitutionsAsync(ct);

        await SeedSpecializationsAsync(ct);
        await SeedAppointmentStatusesAsync(ct);
        await SeedAppointmentTypesAsync(ct);
        await SeedProtocolTypesAsync(ct);
        await SeedTreatmentCatalogAsync(ct);
        await SeedBanksAsync(ct);
        await SeedPaymentProvidersAsync(ct);

        if (_env.IsDevelopment())
        {
            await SeedPlatformAdminAsync(ct);
            await SeedTestPatientsAsync(ct);
            await SeedDoctorSchedulesAsync(ct);
            await SeedDemoPricingDataAsync(ct);
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

    // ─── Role Template Permissions ────────────────────────────────────────
    /// <summary>
    /// Her rol şablonuna temel izinleri atar (idempotent — mevcut atamalar atlanır).
    /// </summary>
    private async Task SeedRoleTemplatePermissionsAsync(CancellationToken ct)
    {
        // Rol kodu → atanacak permission kodları
        var matrix = new Dictionary<string, string[]>
        {
            ["DOCTOR"] =
            [
                "patient.view", "patient.create", "patient.edit", "patient.edit_basic",
                "patient.view_contact",
                "appointment.view", "appointment.create", "appointment.edit", "appointment.cancel",
                "visit.view", "visit.create", "visit.update",
                "protocol.view", "protocol.create", "protocol.update",
                "treatment.view",
                "treatment_plan.view", "treatment_plan.create", "treatment_plan.edit", "treatment_plan.complete",
                "treatment_plan.revert_completed",
                "note.write_patient", "note.delete_patient",
                "anamnesis.edit",
                "complaint.view", "complaint.create",
                "report.view",
                "pricing.view",
                "laboratory.view", "laboratory.work_create",
                "laboratory.work_receive", "laboratory.work_fit", "laboratory.work_complete",
                "consent_form.view",
            ],
            ["ASSISTANT"] =
            [
                "patient.view", "patient.create", "patient.edit_basic",
                "patient.view_contact",
                "appointment.view", "appointment.create", "appointment.edit", "appointment.cancel",
                "visit.view", "visit.create",
                "protocol.view",
                "treatment_plan.view",
                "note.write_patient",
                "complaint.view",
                "laboratory.view", "laboratory.work_create", "laboratory.work_send",
            ],
            ["RECEPTIONIST"] =
            [
                "patient.view", "patient.create", "patient.edit_basic",
                "patient.view_contact",
                "appointment.view", "appointment.create", "appointment.edit", "appointment.cancel",
                "visit.view",
                "protocol.view",
                "payment.view", "payment.create",
                "complaint.view", "complaint.create",
                "report.view_daily", "report.close",
            ],
            ["ACCOUNTANT"] =
            [
                "payment.view", "payment.create", "payment.backdate",
                "invoice.view", "invoice.create",
                "report.view", "report.export", "report.close", "report.approve",
                "commission.view", "commission.distribute",
                "commission.manage",
                "allocation.view", "allocation.request",
                "institution_invoice.view", "institution_invoice.create",
                "institution_invoice.payment", "institution_invoice.follow_up",
                "patient_invoice.view", "patient_invoice.create",
                "patient.view", "patient.view_contact",
                "pricing.view",
                "institution.view",
            ],
            ["READONLY"] =
            [
                "patient.view",
                "appointment.view",
                "visit.view",
                "protocol.view",
                "treatment_plan.view",
                "report.view",
            ],
            ["BRANCH_MANAGER"] =
            [
                "patient.view", "patient.create", "patient.edit", "patient.edit_basic",
                "patient.delete", "patient.upload_document", "patient.write_hidden_note",
                "patient.view_contact",
                "appointment.view", "appointment.create", "appointment.edit",
                "appointment.cancel", "appointment.delete", "appointment.create_overlap",
                "visit.view", "visit.create", "visit.update",
                "protocol.view", "protocol.create", "protocol.update",
                "treatment.view",
                "treatment_plan.view", "treatment_plan.create", "treatment_plan.edit",
                "treatment_plan.complete", "treatment_plan.complete_without_consent",
                "treatment_plan.revert_completed",
                "treatment_plan.delete_planned", "treatment_plan.delete_completed",
                "payment.view", "payment.create", "payment.delete", "payment.refund", "payment.backdate",
                "invoice.view", "invoice.create",
                "report.view", "report.export", "report.close", "report.approve", "report.reopen",
                "settings.view", "settings.edit_general",
                "note.write_patient", "note.delete_patient",
                "anamnesis.edit",
                "complaint.view", "complaint.create", "complaint.manage",
                "commission.view", "commission.distribute",
                "commission.manage", "commission.approve_dist",
                "allocation.view", "allocation.request", "allocation.approve",
                "institution_invoice.view", "institution_invoice.create",
                "institution_invoice.payment", "institution_invoice.follow_up",
                "institution_invoice.cancel",
                "patient_invoice.view", "patient_invoice.create", "patient_invoice.cancel",
                "survey.manage", "survey.view_results",
                "audit.view",
                "report.view_daily",
                "institution.view", "institution.manage",
                "pricing.view", "pricing.create", "pricing.edit", "pricing.delete",
                "laboratory.view", "laboratory.manage",
                "laboratory.work_create", "laboratory.work_send", "laboratory.work_receive",
                "laboratory.work_fit", "laboratory.work_complete",
                "laboratory.work_approve", "laboratory.work_cancel",
                "consent_form.view", "consent_form.manage",
            ],
        };

        var roles = await _db.RoleTemplates
            .Include(r => r.RoleTemplatePermissions)
            .Where(r => matrix.Keys.Contains(r.Code))
            .ToListAsync(ct);

        var allPermCodes = matrix.Values.SelectMany(v => v).Distinct().ToList();
        var permissions = await _db.Permissions
            .Where(p => allPermCodes.Contains(p.Code))
            .ToDictionaryAsync(p => p.Code, ct);

        var added = 0;
        foreach (var role in roles)
        {
            if (!matrix.TryGetValue(role.Code, out var codes)) continue;

            var existingPermIds = role.RoleTemplatePermissions
                .Select(rtp => rtp.PermissionId)
                .ToHashSet();

            foreach (var code in codes)
            {
                if (!permissions.TryGetValue(code, out var perm)) continue;
                if (existingPermIds.Contains(perm.Id)) continue;

                _db.RoleTemplatePermissions.Add(
                    RoleTemplatePermission.Create(role.Id, perm.Id));
                added++;
            }
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("{Count} rol-izin ataması eklendi.", added);
        }
        else
        {
            _logger.LogDebug("Tüm rol-izin atamaları zaten mevcut.");
        }
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
            ("appointment",      "create_overlap",   false),
            ("appointment",      "edit",             false),
            ("appointment",      "cancel",           false),
            ("appointment",      "delete",           true),

            // Visit & Protocol
            ("visit",            "view",             false),
            ("visit",            "create",           false),
            ("visit",            "update",           false),
            ("protocol",         "view",             false),
            ("protocol",         "create",           false),
            ("protocol",         "update",           false),

            // Treatment Catalog
            ("treatment",        "view",             false),
            ("treatment",        "manage",           false),

            // Treatment Plan
            ("treatment_plan",   "view",             false),
            ("treatment_plan",   "create",           false),
            ("treatment_plan",   "edit",             false),
            ("treatment_plan",   "complete",                   false),
            ("treatment_plan",   "complete_without_consent",  true),
            ("treatment_plan",   "revert_completed",           true),
            ("treatment_plan",   "delete_planned",             true),
            ("treatment_plan",   "delete_completed",           true),

            // Payment
            ("payment",          "view",             false),
            ("payment",          "create",           false),
            ("payment",          "delete",           true),
            ("payment",          "refund",           true),
            ("payment",          "backdate",         true),

            // Report
            ("report",           "view",             false),
            ("report",           "export",           false),
            ("report",           "close",            false),
            ("report",           "approve",          true),
            ("report",           "reopen",           true),

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

            // Pricing
            ("pricing",          "view",             false),
            ("pricing",          "create",           false),
            ("pricing",          "edit",             false),
            ("pricing",          "delete",           true),

            // Laboratory
            ("laboratory",       "view",             false),
            ("laboratory",       "manage",           false),   // lab + fiyat + şube atama CRUD
            ("laboratory",       "work_create",      false),
            ("laboratory",       "work_send",        false),
            ("laboratory",       "work_receive",     false),
            ("laboratory",       "work_fit",         false),
            ("laboratory",       "work_complete",    false),
            ("laboratory",       "work_approve",     true),    // hakediş açar → dangerous
            ("laboratory",       "work_cancel",      true),

            // Commission / Hakediş
            ("commission",       "manage",           false),   // şablon + atama + hedef
            ("commission",       "approve_dist",     true),    // hakediş dağıtım onayı
            ("commission",       "view_doctor_self", false),   // hekim kendi hakedişini görür

            // Allocation / Ödeme Dağıtım
            ("allocation",       "view",             false),
            ("allocation",       "request",          false),   // manuel dağıtım talebi
            ("allocation",       "approve",          true),    // manuel dağıtım onayı

            // Institution Invoice / Kurum Fatura
            ("institution_invoice", "view",          false),
            ("institution_invoice", "create",        false),
            ("institution_invoice", "payment",       false),   // ödeme kaydı
            ("institution_invoice", "follow_up",     false),   // takip / hatırlatma
            ("institution_invoice", "cancel",        true),

            // Patient Invoice / Hasta Fatura
            ("patient_invoice",  "view",             false),
            ("patient_invoice",  "create",           false),
            ("patient_invoice",  "cancel",           true),

            // Consent Form / Onam Formu
            ("consent_form",     "view",             false),   // şablon listesi
            ("consent_form",     "manage",           false),   // şablon CRUD

            // Patient contact visibility
            ("patient",          "view_contact",     false),   // telefon, e-posta, adres, doğum tarihi
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
        var existing = await _db.AppointmentStatuses.ToListAsync(ct);

        if (existing.Count == 0)
        {

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
            existing = await _db.AppointmentStatuses.ToListAsync(ct);
            _logger.LogInformation("{Count} randevu durumu eklendi.", items.Length);
        }

        // Geçiş kurallarını her zaman güncelle (sonradan eklenen kurallara da uyum sağlar)
        var byCode = existing.ToDictionary(s => s.Code);

        if (byCode.ContainsKey("PLANNED") && byCode.ContainsKey("CONFIRMED"))
        {
            byCode["PLANNED"].SetAllowedNextStatusIds(
                $"[{byCode["CONFIRMED"].Id},{byCode["ARRIVED"].Id},{byCode["CANCELLED"].Id},{byCode["NO_SHOW"].Id}]");
            byCode["CONFIRMED"].SetAllowedNextStatusIds(
                $"[{byCode["ARRIVED"].Id},{byCode["CANCELLED"].Id},{byCode["NO_SHOW"].Id}]");
            byCode["ARRIVED"].SetAllowedNextStatusIds(
                $"[{byCode["IN_ROOM"].Id},{byCode["CANCELLED"].Id},{byCode["NO_SHOW"].Id}]");
            byCode["IN_ROOM"].SetAllowedNextStatusIds(
                $"[{byCode["COMPLETED"].Id},{byCode["LEFT"].Id}]");
            // Terminal: LEFT, CANCELLED, COMPLETED, NO_SHOW → geçiş yok

            await _db.SaveChangesAsync(ct);
            _logger.LogDebug("Randevu durumu geçiş kuralları güncellendi.");
        }

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

        // Aylin Şahin — Ana Şube: Pzt, Çar, Cum 09-18, öğle 13-14
        // (Salı+Perş Kadıköy'de, çakışma önlendi)
        foreach (var d in new[] { 1, 3, 5 })
            AddSchedule(aylin, anaSube, d, t(9), t(18), t(13), t(14));
        // Aylin — Kadıköy: Salı + Perşembe 09-17, öğle 12:30-13:30
        AddSchedule(aylin, kadikoySube, 2, t(9), t(17), t(12, 30), t(13, 30));
        AddSchedule(aylin, kadikoySube, 4, t(9), t(17), t(12, 30), t(13, 30));

        // Kerem Özdemir — Ana: Pzt, Çar, Cum 09-17, öğle 12:30-13:30
        foreach (var d in new[] { 1, 3, 5 })
            AddSchedule(kerem, anaSube, d, t(9), t(17), t(12, 30), t(13, 30));
        // Kerem — Ana: Salı, Perş 14-19 (akşam seferi)
        AddSchedule(kerem, anaSube, 2, t(14), t(19));
        AddSchedule(kerem, anaSube, 4, t(14), t(19));

        // Seda Yıldırım — Kadıköy: Pzt, Salı, Perş 10-19, öğle 13-14
        // (Çar+Cum Beşiktaş'ta, çakışma önlendi)
        foreach (var d in new[] { 1, 2, 4 })
            AddSchedule(seda, kadikoySube, d, t(10), t(19), t(13), t(14));
        // Seda — Beşiktaş: Çar, Cum 09-16
        foreach (var d in new[] { 3, 5 })
            AddSchedule(seda, besiktasSube, d, t(9), t(16), t(12), t(13));

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
        var admins = new[]
        {
            ("admin@oravity.com",  "Platform Admin"),
            ("cadmin@oravity.com", "Company Admin (Platform)"),
        };

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!", workFactor: 12);

        foreach (var (email, name) in admins)
        {
            if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            {
                _logger.LogDebug("Platform admin {Email} zaten mevcut, atlanıyor.", email);
                continue;
            }

            var admin = User.Create(
                email: email,
                fullName: name,
                passwordHash: passwordHash,
                isPlatformAdmin: true
            );

            await _db.Users.AddAsync(admin, ct);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Platform admin kullanıcısı oluşturuldu: {Email}", email);
        }
    }

    private async Task SeedProtocolTypesAsync(CancellationToken ct)
    {
        var seeds = new[]
        {
            ProtocolTypeSetting.Create(1, "Muayene",      "EXAMINATION",  "#6366f1", 1, "İlk veya rutin muayene"),
            ProtocolTypeSetting.Create(2, "Tedavi",        "TREATMENT",    "#0ea5e9", 2, "Tedavi seansı"),
            ProtocolTypeSetting.Create(3, "Konsültasyon",  "CONSULTATION", "#8b5cf6", 3, "Uzman görüşü"),
            ProtocolTypeSetting.Create(4, "Kontrol",       "FOLLOW_UP",    "#10b981", 4, "Kontrol muayenesi"),
            ProtocolTypeSetting.Create(5, "Acil",          "EMERGENCY",    "#ef4444", 5, "Acil tedavi"),
        };

        foreach (var seed in seeds)
        {
            var existing = await _db.ProtocolTypes.FindAsync([seed.Id], ct);
            if (existing is null)
                await _db.ProtocolTypes.AddAsync(seed, ct);
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Protokol tipleri seed edildi.");
    }

    // ── Treatment Catalog (Global) ─────────────────────────────────────────────
    private async Task SeedTreatmentCatalogAsync(CancellationToken ct)
    {
        var catalog = GetDentalTreatmentCatalog();

        // Kategori + tedavi seed (ilk kurulumda)
        if (!await _db.TreatmentCategories.AnyAsync(c => c.CompanyId == null, ct))
        {
            foreach (var cat in catalog)
            {
                var category = TreatmentCategory.Create(null, cat.Name, null, cat.SortOrder);
                await _db.TreatmentCategories.AddAsync(category, ct);
                await _db.SaveChangesAsync(ct);

                foreach (var t in cat.Treatments)
                {
                    var treatment = Treatment.Create(
                        companyId:                null,
                        code:                     t.Code,
                        name:                     t.Name,
                        categoryId:               category.Id,
                        kdvRate:                  t.KdvRate,
                        requiresSurfaceSelection: t.RequiresSurface,
                        requiresLaboratory:       t.RequiresLab,
                        allowedScopes:            null,
                        tags:                     null,
                        sutCode:                  t.SutCode);
                    await _db.Treatments.AddAsync(treatment, ct);
                }

                await _db.SaveChangesAsync(ct);
            }
        }

        // TDB 2026 referans fiyat listesi (her seferinde kontrol et)
        var tdbList = await _db.ReferencePriceLists
            .FirstOrDefaultAsync(l => l.Code == "TDB_2026", ct);

        if (tdbList is null)
        {
            tdbList = ReferencePriceList.Create("TDB_2026", "TDB 2026 Rehber Tarife", "manual", 2026);
            await _db.ReferencePriceLists.AddAsync(tdbList, ct);
            await _db.SaveChangesAsync(ct);
        }

        // ReferencePriceItem + TreatmentMapping — her seferinde eksikleri tamamla
        var allTreatments = await _db.Treatments.Where(t => t.CompanyId == null).ToListAsync(ct);
        var allSeeds      = catalog.SelectMany(c => c.Treatments).ToDictionary(t => t.Code);
        var changed       = false;

        foreach (var tr in allTreatments)
        {
            if (!allSeeds.TryGetValue(tr.Code, out var seed)) continue;

            if (!await _db.ReferencePriceItems.AnyAsync(i => i.ListId == tdbList.Id && i.TreatmentCode == tr.Code, ct))
            {
                await _db.ReferencePriceItems.AddAsync(
                    ReferencePriceItem.Create(tdbList.Id, tr.Code, tr.Name, seed.TdbPrice, 0m, "TRY", null, null), ct);
                changed = true;
            }

            if (!await _db.TreatmentMappings.AnyAsync(m => m.InternalTreatmentId == tr.Id && m.ReferenceListId == tdbList.Id, ct))
            {
                await _db.TreatmentMappings.AddAsync(
                    TreatmentMapping.Create(tr.Id, tdbList.Id, tr.Code, "exact", null), ct);
                changed = true;
            }
        }

        if (changed) await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Tedavi kataloğu seed tamamlandı ({Count} kategori).", catalog.Length);
    }

    // ─── Bankalar ────────────────────────────────────────────────────────
    private async Task SeedBanksAsync(CancellationToken ct)
    {
        if (await _db.Banks.AnyAsync(ct)) return;

        var banks = new[]
        {
            Bank.Create("Türkiye İş Bankası A.Ş.",    "İşbank",      "ISBKTRIS"),
            Bank.Create("T.C. Ziraat Bankası A.Ş.",   "Ziraat",      "TCZBTR2A"),
            Bank.Create("Türkiye Garanti Bankası A.Ş.", "Garanti BBVA", "TGBATRIS"),
            Bank.Create("Akbank T.A.Ş.",               "Akbank",      "AKBKTRIS"),
            Bank.Create("Yapı ve Kredi Bankası A.Ş.", "YapıKredi",   "YAPITRISFEX"),
            Bank.Create("Türkiye Halk Bankası A.Ş.",  "Halkbank",    "TRHBTR2A"),
            Bank.Create("Türkiye Vakıflar Bankası T.A.O.", "VakıfBank", "TVBATR2A"),
            Bank.Create("QNB Finansbank A.Ş.",         "QNB Finansbank", "FNNBTRSX"),
            Bank.Create("Denizbank A.Ş.",              "DenizBank",   "DENITRIS"),
            Bank.Create("ING Bank A.Ş.",               "ING",         "INGBTRIS"),
            Bank.Create("HSBC Bank A.Ş.",              "HSBC",        "HSBCTRIS"),
            Bank.Create("TEB A.Ş.",                    "TEB",         "TEBUTRIS"),
            Bank.Create("Şekerbank T.A.Ş.",            "Şekerbank",   "SEKETR2A"),
            Bank.Create("Odeabank A.Ş.",               "Odeabank",    "ODEATRI2"),
        };

        _db.Banks.AddRange(banks);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Bankalar seed tamamlandı ({Count} banka).", banks.Length);
    }

    // ─── Ödeme Sağlayıcıları ─────────────────────────────────────────────
    private async Task SeedPaymentProvidersAsync(CancellationToken ct)
    {
        if (await _db.PaymentProviders.AnyAsync(ct)) return;

        var providers = new[]
        {
            PaymentProvider.Create("PayTR",   "PayTR",   "https://www.paytr.com"),
            PaymentProvider.Create("iyzico",  "iyzico",  "https://www.iyzico.com"),
            PaymentProvider.Create("Stripe",  "Stripe",  "https://stripe.com"),
            PaymentProvider.Create("Param",   "Param",   "https://www.param.com.tr"),
            PaymentProvider.Create("PayU",    "PayU",    "https://www.payu.com.tr"),
        };

        _db.PaymentProviders.AddRange(providers);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Ödeme sağlayıcıları seed tamamlandı ({Count} sağlayıcı).", providers.Length);
    }

    // ─── Demo Fiyat Verileri (sadece Development) ─────────────────────────
    private async Task SeedDemoPricingDataAsync(CancellationToken ct)
    {
        const string demoCompanyName = "Demo Diş Kliniği";

        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Name == demoCompanyName, ct);
        if (company is null)
        {
            _logger.LogWarning("Demo Diş Kliniği bulunamadı, fiyat seed atlanıyor.");
            return;
        }

        // İdempotent guard
        if (await _db.PricingRules.AnyAsync(r => r.CompanyId == company.Id, ct))
        {
            _logger.LogDebug("Demo fiyat verileri zaten mevcut, atlanıyor.");
            return;
        }

        // ── 1. Şubeler (MULTI çarpanları) ───────────────────────────────────
        var branches = await _db.Branches
            .Where(b => b.CompanyId == company.Id && b.IsActive)
            .ToListAsync(ct);

        Branch GetOrAddBranch(string name, decimal multiplier)
        {
            var b = branches.FirstOrDefault(x => x.Name == name);
            if (b is null)
            {
                b = Branch.Create(name, company.Id);
                _db.Branches.Add(b);
                branches.Add(b);
            }
            b.SetPricingMultiplier(multiplier);
            return b;
        }

        GetOrAddBranch("Nişantaşı Şubesi", 1.15m);
        GetOrAddBranch("Bodrum Şubesi",    1.10m);
        await _db.SaveChangesAsync(ct);

        // ── 2. Şirkete özel kurumlar ─────────────────────────────────────────
        async Task<Institution> GetOrAddInstitutionAsync(string name, string code, string type)
        {
            var inst = await _db.Institutions
                .FirstOrDefaultAsync(i => i.CompanyId == company.Id && i.Code == code, ct);
            if (inst is null)
            {
                inst = Institution.Create(name, code, type, company.Id);
                await _db.Institutions.AddAsync(inst, ct);
            }
            return inst;
        }

        var thy      = await GetOrAddInstitutionAsync("Türk Hava Yolları", "THY",      "kurumsal");
        var koc      = await GetOrAddInstitutionAsync("Koç Topluluğu",     "KOC",      "kurumsal");
        _              = await GetOrAddInstitutionAsync("Turkcell",          "TURKCELL", "kurumsal");
        await _db.SaveChangesAsync(ct);

        // ── 3. CARI referans fiyat listesi ────────────────────────────────────
        var cariList = await _db.ReferencePriceLists
            .FirstOrDefaultAsync(l => l.Code == "CARI", ct);
        if (cariList is null)
        {
            cariList = ReferencePriceList.Create("CARI", "Cari Fiyat Listesi 2026", "private", 2026);
            await _db.ReferencePriceLists.AddAsync(cariList, ct);
            await _db.SaveChangesAsync(ct);
        }

        // CARI fiyatları: klinik piyasa fiyatı (TDB'den ~%15-20 yüksek)
        var cariItems = new Dictionary<string, (string Name, decimal Price)>
        {
            ["1-18"] = ("Panoramik Film",                                  2200m),
            ["2-4"]  = ("Kompozit Dolgu (Bir Yüzlü)",                     3500m),
            ["2-5"]  = ("Kompozit Dolgu (İki Yüzlü)",                     4400m),
            ["2-27"] = ("Kanal Tedavisi (1 Kanallı)",                      4800m),
            ["2-28"] = ("Kanal Tedavisi (2 Kanallı)",                      7500m),
            ["2-29"] = ("Kanal Tedavisi (3 Kanallı)",                     10800m),
            ["4-29"] = ("Tam Seramik Kuron",                              22000m),
            ["4-51"] = ("Zirkonyum Kuron",                                14500m),
            ["5-1"]  = ("Diş Çekimi (Basit)",                              2600m),
            ["5-34"] = ("Kemik İçi İmplant",                              23000m),
            ["6-1"]  = ("Detartraj (Tek Çene)",                            3500m),
            ["7-62"] = ("Şeffaf Plak Ortodontik Tedavi (Hafif)",          55000m),
        };

        var globalTreatments = await _db.Treatments
            .Where(t => t.CompanyId == null)
            .ToListAsync(ct);

        var anyChanged = false;
        foreach (var (code, (name, price)) in cariItems)
        {
            if (!await _db.ReferencePriceItems.AnyAsync(
                    i => i.ListId == cariList.Id && i.TreatmentCode == code, ct))
            {
                await _db.ReferencePriceItems.AddAsync(
                    ReferencePriceItem.Create(cariList.Id, code, name, price, 0m, "TRY", null, null), ct);
                anyChanged = true;
            }

            var treatment = globalTreatments.FirstOrDefault(t => t.Code == code);
            if (treatment is not null &&
                !await _db.TreatmentMappings.AnyAsync(
                    m => m.InternalTreatmentId == treatment.Id && m.ReferenceListId == cariList.Id, ct))
            {
                await _db.TreatmentMappings.AddAsync(
                    TreatmentMapping.Create(treatment.Id, cariList.Id, code, "exact", null), ct);
                anyChanged = true;
            }
        }

        if (anyChanged) await _db.SaveChangesAsync(ct);

        // ── 4. Fiyatlandırma kuralları ─────────────────────────────────────
        var thyId = thy.Id;
        var kocId = koc.Id;

        var rules = new[]
        {
            PricingRule.Create(
                companyId:      company.Id,
                branchId:       null,
                name:           "THY + ÖSS VIP",
                description:    "THY çalışanı ve ÖSS kapsamındaki hastalar için özel tarife",
                ruleType:       "formula",
                priority:       5,
                includeFilters: $$$"""{"institutionIds":[{{{thyId}}}],"ossOnly":true}""",
                excludeFilters: null,
                formula:        "TDB * 0.70",
                outputCurrency: "TRY",
                validFrom:      null,
                validUntil:     null,
                stopProcessing: true,
                createdBy:      null),

            PricingRule.Create(
                companyId:      company.Id,
                branchId:       null,
                name:           "THY Kurumsal",
                description:    "Türk Hava Yolları anlaşmalı personel fiyatı (%18 indirim)",
                ruleType:       "formula",
                priority:       10,
                includeFilters: $$$"""{"institutionIds":[{{{thyId}}}]}""",
                excludeFilters: null,
                formula:        "TDB * 0.82",
                outputCurrency: "TRY",
                validFrom:      null,
                validUntil:     null,
                stopProcessing: true,
                createdBy:      null),

            PricingRule.Create(
                companyId:      company.Id,
                branchId:       null,
                name:           "Koç / Turkcell Kurumsal",
                description:    "Koç Topluluğu çalışanları için kurumsal anlaşma fiyatı (%15 indirim)",
                ruleType:       "formula",
                priority:       15,
                includeFilters: $$$"""{"institutionIds":[{{{kocId}}}]}""",
                excludeFilters: null,
                formula:        "TDB * 0.85",
                outputCurrency: "TRY",
                validFrom:      null,
                validUntil:     null,
                stopProcessing: true,
                createdBy:      null),

            PricingRule.Create(
                companyId:      company.Id,
                branchId:       null,
                name:           "ÖSS Genel",
                description:    "Özel sağlık sigortası kapsamındaki tüm hastalar (%22 indirim)",
                ruleType:       "formula",
                priority:       20,
                includeFilters: """{"ossOnly":true}""",
                excludeFilters: null,
                formula:        "TDB * 0.78",
                outputCurrency: "TRY",
                validFrom:      null,
                validUntil:     null,
                stopProcessing: true,
                createdBy:      null),

            PricingRule.Create(
                companyId:      company.Id,
                branchId:       null,
                name:           "Kurumsal Anlaşmalı",
                description:    "Herhangi bir anlaşmalı kurumun hastası (%10 indirim)",
                ruleType:       "formula",
                priority:       25,
                includeFilters: """{"institutionAgreement":true}""",
                excludeFilters: null,
                formula:        "TDB * 0.90",
                outputCurrency: "TRY",
                validFrom:      null,
                validUntil:     null,
                stopProcessing: true,
                createdBy:      null),

            PricingRule.Create(
                companyId:      company.Id,
                branchId:       null,
                name:           "Standart Cari Tarife",
                description:    "Tüm özel hastalar — şube çarpanı (MULTI) ile uygulanır",
                ruleType:       "formula",
                priority:       50,
                includeFilters: null,
                excludeFilters: null,
                formula:        "TDB * MULTI",
                outputCurrency: "TRY",
                validFrom:      null,
                validUntil:     null,
                stopProcessing: true,
                createdBy:      null),
        };

        await _db.PricingRules.AddRangeAsync(rules, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Demo fiyat seed tamamlandı: 2 şube (MULTI), 3 kurum, {Cari} CARI kalemi, {Rules} kural.",
            cariItems.Count, rules.Length);
    }

    private static CategorySeed[] GetDentalTreatmentCatalog() =>
    [
        new("Teşhis ve Tedavi Planlaması", 1,
        [
            new("1-1",  "Dişhekimi Muayenesi",                        "401010", 1500.00m),
            new("1-2",  "Uzman Dişhekimi Muayenesi",                  "401020", 1850.00m),
            new("1-3",  "Kontrol Hekim Muayenesi",                    "401010", 1300.00m),
            new("1-4",  "Konsültasyon",                               "401030", 1009.09m),
            new("1-5",  "Uzman Dişhekimi Konsültasyonu",              "401040", 1313.64m),
            new("1-8",  "Teşhis ve Tedavi Planlaması",                "401000", 1395.45m),
            new("1-9",  "Oral Hijyen Eğitimi",                       "401010", 1068.18m),
            new("1-13", "Vitalite Kontrolü (Diş Başına)",             "401010",  213.64m),
            new("1-14", "Diş Röntgen Filmi (Periapikal)",             "401050",  754.55m),
            new("1-16", "Bite-Wing Radyografi",                       "401150",  754.55m),
            new("1-18", "Panoramik Film",                             "401080", 1854.55m),
            new("1-19", "Lateral Sefalometrik Film",                  "401090", 1863.64m),
            new("1-21", "İntra Oral Dijital Radyografi (RVG)",        "401160",  900.00m),
            new("1-24", "Tomografi (Bölgesel)",                       "401120", 2500.00m),
            new("1-25", "Tomografi (Tek Çene)",                       "401120", 4000.00m),
            new("1-26", "Tomografi (İki Çene)",                       "401120", 5400.00m),
            new("1-31", "Lokal Anestezi (İnfiltratif)",               "405420",  336.36m),
            new("1-32", "Lokal Anestezi (Rejyonal)",                  "405430",  336.36m),
            new("1-34", "Ağız İçi Dijital Tarama",                   null,      2500.00m),
        ]),

        new("Tedavi ve Endodonti", 2,
        [
            new("2-1",  "Amalgam Dolgu (Bir Yüzlü)",                  "402010", 2586.36m, RequiresSurface: true),
            new("2-2",  "Amalgam Dolgu (İki Yüzlü)",                  "402020", 3459.09m, RequiresSurface: true),
            new("2-3",  "Amalgam Dolgu (Üç Yüzlü)",                   "402030", 4404.55m, RequiresSurface: true),
            new("2-4",  "Kompozit Dolgu (Bir Yüzlü)",                 "200120", 3068.18m, RequiresSurface: true),
            new("2-5",  "Kompozit Dolgu (İki Yüzlü)",                 "200130", 3850.00m, RequiresSurface: true),
            new("2-6",  "Kompozit Dolgu (Üç Yüzlü)",                  "200140", 4818.18m, RequiresSurface: true),
            new("2-7",  "Direkt Kompozit Laminate Restorasyonu",      "404390", 8450.00m, RequiresSurface: true),
            new("2-9",  "Cam İyonomer Dolgu",                         "402190", 2431.82m, RequiresSurface: true),
            new("2-11", "İnley Dolgu (Bir Yüzlü)",                    "402040", 5713.64m, RequiresSurface: true, RequiresLab: true),
            new("2-12", "İnley Dolgu (İki Yüzlü)",                    "402050", 5900.00m, RequiresSurface: true, RequiresLab: true),
            new("2-13", "İnley Dolgu (Üç Yüzlü)",                     "402060", 6154.55m, RequiresSurface: true, RequiresLab: true),
            new("2-17", "Seramik İnley Dolgu (Bir Yüzlü)",            "200150", 14200.00m, RequiresSurface: true, RequiresLab: true),
            new("2-18", "Seramik İnley Dolgu (İki Yüzlü)",            "200160", 14200.00m, RequiresSurface: true, RequiresLab: true),
            new("2-19", "Seramik İnley Dolgu (Üç Yüzlü)",             "200170", 14200.00m, RequiresSurface: true, RequiresLab: true),
            new("2-23", "Dolgu (Restorasyon) Tamiri",                 "401010", 2654.55m),
            new("2-25", "Kuafaj (Dolgu Hariç)",                       "402130",  400.00m),
            new("2-26", "Ekstirpasyon (Her Kanal İçin)",              "402300", 1827.27m),
            new("2-27", "Kanal Tedavisi - Tek Kanal",                 "402150", 4190.91m),
            new("2-28", "Kanal Tedavisi - İki Kanal",                 "402152", 6563.64m),
            new("2-29", "Kanal Tedavisi - Üç Kanal",                  "402153", 9409.09m),
            new("2-30", "Kanal Tedavisi - İlave Her Kanal",           "402154", 2272.73m),
            new("2-31", "Periapikal Lezyonlu Kanal Tedavisi - Tek",   "402271", 4595.45m),
            new("2-32", "Periapikal Lezyonlu Kanal Tedavisi - İki",   "402272", 7000.00m),
            new("2-33", "Periapikal Lezyonlu Kanal Tedavisi - Üç",    "402273", 9981.82m),
            new("2-35", "Kanal Dolgusu Tekrarı (Retreatment - Kanal)","401010", 4100.00m),
            new("2-37", "Kanal İçi Hazır Post (Metal)",               "402240", 2795.45m, RequiresLab: true),
            new("2-38", "Kanal İçi Fiber Post",                       "402240", 4500.00m),
            new("2-42", "Endokron",                                   null,     12500.00m, RequiresLab: true),
            new("2-43", "Hassasiyet Tedavisi (Tek Diş)",              "402250", 1109.09m),
            new("2-45", "Diş Ağartma (Vital Tek Diş)",               "200100", 1950.00m),
            new("2-47", "Diş Ağartma (Tek Çene)",                    "200110", 10850.00m),
        ]),

        new("Pedodonti", 3,
        [
            new("3-2",  "Fissür Örtücü (Sealant - Tek Diş)",         "403010", 1313.64m),
            new("3-3",  "Yüzeysel Flor Uygulaması (Yarım Çene)",     "403020", 1250.00m),
            new("3-4",  "Kompomer Dolgu",                             "403090", 3800.00m, RequiresSurface: true),
            new("3-6",  "Amputasyon",                                 "402140", 3500.00m),
            new("3-7",  "Süt Dişi Kanal Tedavisi",                   null,      6150.00m),
            new("3-10", "Yer Tutucu (Sabit)",                        "403040", 6800.00m, RequiresLab: true),
            new("3-11", "Yer Tutucu (Hareketli)",                    "403050", 9100.00m, RequiresLab: true),
            new("3-12", "Prefabrike Kron",                            "403030", 3463.64m, RequiresLab: true),
            new("3-13", "Strip Kron",                                 "403080", 3250.00m, RequiresLab: true),
            new("3-16", "Çocuk Protezi (Akrilik - Bölümlü - Tek Çene)", "403060", 13681.82m, RequiresLab: true),
            new("3-17", "Çocuk Protezi (Akrilik - Tam - Tek Çene)",  "403070", 15450.00m, RequiresLab: true),
            new("3-18", "Avülsiyon Tedavisi",                        null,     17000.00m),
        ]),

        new("Protez", 4,
        [
            new("4-1",  "Tam Protez (Akrilik - Tek Çene)",            "404010", 29000.00m, RequiresLab: true),
            new("4-2",  "Bölümlü Protez (Akrilik - Tek Çene)",        "404020", 27900.00m, RequiresLab: true),
            new("4-3",  "Tam Protez (Döküm Metal - Tek Çene)",        "404030", 36000.00m, RequiresLab: true),
            new("4-4",  "Bölümlü Protez (Döküm Metal - Tek Çene)",    "404040", 35000.00m, RequiresLab: true),
            new("4-7",  "Geçici (İmmediyet) Protez (Tek Çene)",       "404050", 20259.09m, RequiresLab: true),
            new("4-8",  "Besleme (Tek Çene)",                         "404080", 10877.27m),
            new("4-9",  "Kaide Yenileme (Rebazaj - Tek Çene)",        "404060", 11300.00m),
            new("4-12", "Tamir (Akrilik Protez)",                     "404090", 3459.09m),
            new("4-15", "Diş İlavesi (Tek Diş)",                     "404120", 3800.00m, RequiresLab: true),
            new("4-17", "Gece Plağı (Yumuşak)",                      "404150", 5350.00m, RequiresLab: true),
            new("4-18", "Gece Plağı (Sert Oklüzal Splint)",          "404150", 18400.00m, RequiresLab: true),
            new("4-20", "Tek Parça Döküm Kuron",                     "404170", 7031.82m, RequiresLab: true),
            new("4-22", "Veneer Kuron (Seramik)",                    "404181", 11000.00m, RequiresLab: true),
            new("4-24", "Laminate Veneer Kompozit",                  null,      8450.00m, RequiresLab: true),
            new("4-26", "Laminate Veneer (Seramik)",                 "404181", 24000.00m, RequiresLab: true),
            new("4-27", "Jaket Kuron (Akrilik)",                     "404200", 6800.00m, RequiresLab: true),
            new("4-29", "Tam Seramik Kuron (Metal Desteksiz)",        "404201", 19500.00m, RequiresLab: true),
            new("4-30", "Teleskop Kuron (Koping)",                   "404210", 8995.45m, RequiresLab: true),
            new("4-32", "Döküm Post Core (Pivo)",                    "404190", 6236.36m, RequiresLab: true),
            new("4-33", "Adeziv Köprü (Maryland)",                   "404220", 10500.00m, RequiresLab: true),
            new("4-34", "Geçici Kuron (Tek Diş)",                    "404240", 2040.91m, RequiresLab: true),
            new("4-35", "Kuron Sökümü (Tek Sabit Üye)",             "404250", 1704.55m),
            new("4-36", "Düşmüş Kuron/Köprü Simantasyonu",          "404260", 1113.64m),
            new("4-51", "Zirkonyum Kuron",                           null,     12500.00m, RequiresLab: true),
            new("4-49", "İmplant Rehberi (Yarım Çene)",             null,      8850.00m, RequiresLab: true),
            new("4-50", "İmplant Rehberi (Tam Çene)",               null,     13000.00m, RequiresLab: true),
        ]),

        new("Ağız-Diş ve Çene Cerrahisi", 5,
        [
            new("5-1",  "Diş Çekimi",                                "405010", 2250.00m),
            new("5-2",  "Komplikasyonlu Diş Çekimi",                 "405020", 4500.00m),
            new("5-3",  "Gömülü Diş Operasyonu",                    "405030", 7250.00m),
            new("5-4",  "Gömülü Diş Operasyonu (Kemik Retansiyonlu)","405040", 10650.00m),
            new("5-5",  "Tek Kökte Kök Ucu Rezeksiyonu",             "405060", 9500.00m),
            new("5-6",  "İki Kökte Kök Ucu Rezeksiyonu",             "405060", 11750.00m),
            new("5-7",  "Üç Kökte Kök Ucu Rezeksiyonu",              "405060", 13750.00m),
            new("5-8",  "Alveolitis Cerrahi Tedavisi",               "405070", 6777.27m),
            new("5-13", "Kist Operasyonu (Küçük)",                   "405110", 10050.00m),
            new("5-14", "Kist Operasyonu (1 cm Büyük)",              "405110", 15000.00m),
            new("5-20", "Sert Doku Greftleme",                       "405170", 14500.00m),
            new("5-21", "Yumuşak Doku Greftleme",                    "405170", 12000.00m),
            new("5-22", "Sinüs Lifting",                             "401010", 13150.00m),
            new("5-23", "Biyopsi",                                   "405180", 5800.00m),
            new("5-25", "Apse Drenajı (Ekstraoral)",                 "405190", 10150.00m),
            new("5-26", "Apse Drenajı (İntraoral)",                  "405190", 7990.91m),
            new("5-27", "Kapişon İzalesi / İmplant Üstü Açılması",  "401010", 3309.09m),
            new("5-32", "Reimplantasyon",                            "405210", 10500.00m),
            new("5-34", "Kemik İçi İmplant (Tek Silindirik)",        "405260", 20400.00m),
            new("5-35", "Torus Operasyonu (Yarım Çene)",             "405270", 9800.00m),
            new("5-42", "Ortodontik Amaçlı Gömük Diş Üzeri Açılması","405380", 9500.00m),
            new("5-51", "İmplant Çıkartılması",                     null,      9500.00m),
            new("5-55", "Koronektomi",                               null,      9500.00m),
        ]),

        new("Periodontoloji", 6,
        [
            new("6-1",  "Detartraj (Diş Taşı Temizliği - Tek Çene)", "406020", 3000.00m),
            new("6-2",  "Subgingival Küretaj (Tek Diş)",             "406030", 1700.00m),
            new("6-3",  "Subgingival İlaç Uygulaması",               "406180",  300.00m),
            new("6-4",  "Gingivoplasti (Tek Diş)",                   "406130", 2850.00m),
            new("6-5",  "Gingivektomi (Tek Diş)",                    "406040", 2950.00m),
            new("6-6",  "Flap Operasyonu (Tek Diş)",                 "406050", 4750.00m),
            new("6-9",  "Serbest Diş Eti Grefti (Tek Diş)",         "406070", 11950.00m),
            new("6-10", "Saplı Yumuşak Doku Grefti (Tek Diş)",      "406080", 10450.00m),
            new("6-14", "Biyomateryal Uygulaması (Tek Diş)",        "406140", 1250.00m),
            new("6-15", "Membran Uygulaması (Tek Diş)",             "406160", 1236.36m),
            new("6-17", "Subepitelyal Bağ Dokusu Grefti",           "406170", 13500.00m),
            new("6-18", "Frenektomi - Frenetomi",                   "401010", 7000.00m),
            new("6-19", "Peri-İmplantitis (Cerrahi - Tek İmp.)",    null,      6250.00m),
        ]),

        new("Ortodonti", 7,
        [
            new("7-1",  "Lateral Sefalometrik Film Analizi",          "407010", 1522.73m),
            new("7-3",  "Kemik Yaşı Tayini",                         "407060",  586.36m),
            new("7-6",  "Ortodontik Model Yapımı",                   "407090", 1100.00m),
            new("7-7",  "Ortodontik Model Analizi",                  "407100", 1345.45m),
            new("7-10", "Angle Sınıf I Anomali Tedavisi",            "407110", 37190.91m),
            new("7-11", "Angle Sınıf II Anomali Tedavisi",           "407120", 46722.73m),
            new("7-12", "Angle Sınıf III Anomali Tedavisi",          "407130", 56977.27m),
            new("7-18", "Önleyici Ortodontik Tedavi",                "407150", 23750.00m),
            new("7-19", "Kısa Süreli Ortodontik Tedavi",            "407140", 21172.73m),
            new("7-20", "Pekiştirme Tedavisi",                      "407160", 8450.00m),
            new("7-21", "Pekiştirme Aygıtı (Hawley vb.)",           "407170", 6150.00m, RequiresLab: true),
            new("7-22", "Sabit Pekiştirme (Lingual Retainer)",      "407180", 8750.00m),
            new("7-24", "Tek Çene Aparey Yapımı",                   "407190", 6850.00m, RequiresLab: true),
            new("7-25", "Çift Çene Aparey Yapımı (Frankel/Aktivatör)","407200", 11150.00m, RequiresLab: true),
            new("7-35", "Bant Tatbiki (Tek Diş)",                   "407270", 2050.00m),
            new("7-36", "Braket Tatbiki (Tek Diş)",                 "407270", 1750.00m),
            new("7-40", "Bant veya Braket Çıkarılması (Tek Diş)",   "407270",  713.64m),
            new("7-44", "Hızlı Maksiller Genişletme Apareyi",       "407250", 12500.00m, RequiresLab: true),
            new("7-62", "Şeffaf Plak Ortodontik Tedavi (Hafif)",    null,     49000.00m),
            new("7-63", "Şeffaf Plak Ortodontik Tedavi (Orta)",     null,     63000.00m),
            new("7-64", "Şeffaf Plak Ortodontik Tedavi (Ağır)",     null,     87000.00m),
            new("7-60", "Mini Vida Uygulaması",                     null,      4500.00m),
        ]),
    ];
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

// ─── Treatment Catalog Seed Data ─────────────────────────────────────────────

internal record TreatmentSeed(string Code, string Name, string? SutCode, decimal TdbPrice,
    bool RequiresSurface = false, bool RequiresLab = false, decimal KdvRate = 10m);

internal record CategorySeed(string Name, int SortOrder, TreatmentSeed[] Treatments);
