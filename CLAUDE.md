# CLAUDE.md — Oravity

## Proje Nedir
Çok kiracılı (multi-tenant) diş kliniği yönetim platformu.
Şirket → Şube → Kullanıcı hiyerarşisi. Tam spec: `docs/SPEC.md`

## Mimari
```
Oravity.SharedKernel   — Entity'ler, interface'ler, base sınıflar
Oravity.Infrastructure — EF Core (PostgreSQL), Redis, MinIO, Hangfire, JWT
Oravity.Core           — ASP.NET Web API, MediatR CQRS, controller'lar
Oravity.Backend        — Hangfire job worker (ayrı process)
Oravity.Web            — React/TypeScript frontend (src/Oravity.Web/)
```

## Temel Kurallar
- Entity'ler `Oravity.SharedKernel/Entities/` altında, `BaseEntity` veya `AuditableEntity` extend eder
- CQRS: `Modules/{Modül}/Application/Commands/` ve `Queries/`
- Controller → `[RequirePermission("kaynak:eylem")]` filter'ı zorunlu
- Soft delete: `SoftDelete()` metodu, global query filter var
- Permission kodu formatı: `"patient.view"` (nokta ile, DB'de böyle saklanır)
- Exception mapping: `AppException` → HTTP status, `InvalidOperationException` → 400
- Her yeni migration: `--project src/Oravity.Infrastructure --startup-project src/Oravity.Core`

## Mevcut Controller'lar (Swagger)
Appointments, Audit, Auth, BookingRequests, **Campaigns**, Complaints, DentalChart,
EInvoice, Health, Localization, Notifications, PatientPortal,
PatientRecords, Patients, Payments, **Pricing**, PublicBooking,
PublicSurvey, Reports, **Security** (2FA), **Settings** (şirket/şube/kullanıcı/rol/güvenlik),
Surveys, TreatmentMappings, TreatmentPlans, **Treatments**

## DB Migration Durumu (son: 20260414 — AddBranchPricingMultiplier)
Oluşturulan tablolar (her şey migrations ile yönetiliyor):
- Core: companies, branches, users, permissions, role_templates, ...
- Klinik: patients, appointments, treatment_plans, treatment_plan_items
- Fiyatlandırma (04/2026): treatments, treatment_categories, treatment_mappings,
  pricing_rules, reference_price_lists, reference_price_items, **campaigns**
- Güvenlik (04/2026): user_2fa_settings, trusted_devices, branch_security_policies, backup_logs
- branch.pricing_multiplier (04/2026): MULTI formül değişkeni için

## Fiyatlandırma Sistemi
- **PricingEngine**: kural motoru, `RuleEvalContext` ile çalışır
- **FormulaEngine**: `TDB`, `CARI`, `SUT`, `ISAK`, `MULTI` değişkenleri + `MIN()`, `MAX()` fonksiyonları
  - Prefix eşleştirme: `TDB_2026` → `TDB`, `CARI_2026` → `CARI`, `ISAK_2026` → `ISAK` vb.
  - `ISAK` artık referans fiyat listesinden çekiliyor (ISAK_* prefix), boolean değil
- **MULTI**: `Branch.PricingMultiplier` — şube bazlı fiyat katsayısı (ör: Bodrum=1.10)
- **Campaign modülü**: `campaigns` tablosu, CRUD API (`/api/campaigns`), `/pricing → Kampanyalar` sekmesi
  - Kampanya kodu kural motorunun `includeFilters.campaignCodes` ile eşleşir
  - Muayene plan builder'da aktif kampanya seçilebilir → fiyat otomatik hesaplanır
- **StopProcessing**: `true` → ilk eşleşen kuralda dur; `false` → sonraki kurallarla devam et, son eşleşen kazanır
- **TenantCompanyResolver**: branch-level kullanıcılar için CompanyId çözümleme
  - JWT.CompanyId → BranchId→Branch.CompanyId → UserRoleAssignment sıralaması
  - `_tenant.CompanyId ?? throw` KULLANMA, her zaman bu resolver'ı kullan
- **TreatmentMapping**: iç tedavi → referans liste kodu eşleştirmesi
  - Pricing engine önce mapping'i bulur, sonra o listedeki fiyatı çeker
  - Global kategoriler `CompanyId == null`, category sorgusu her zaman `(c.CompanyId == null || c.CompanyId == companyId)`

## Frontend Sayfaları (React — /src/Oravity.Web/src/)
- `/dashboard` — DashboardPage
- `/doctor` — DoctorDashboardPage
- `/muayene/:id` — ExaminationPage (muayene + tedavi plan builder)
- `/patients` — PatientListPage
- `/patients/:id` — PatientDetailPage
- `/catalog` — TreatmentCatalogPage (tedavi kataloğu + referans eşleştirme)
- `/treatments` — TreatmentPlansPage (hasta tedavi planları listesi)
- `/pricing` — PricingPage (referans fiyatlar, kurallar, kampanyalar, eşleştirmeler, şube ayarları, fiyat testi)
- `/finance` — FinancePage
- `/appointments` — AppointmentCalendarPage
- `/settings` — SettingsPage (şirket, şubeler, kullanıcılar, roller & izinler, güvenlik)

## API Endpoints (Yeni/Önemli)
- `GET /api/treatment-categories` — hiyerarşik kategori listesi (ParentPublicId ile)
- `GET /api/treatments/{id}/mappings` — tedavinin referans eşleştirmeleri
- `POST /api/treatments/{id}/mappings` — eşleştirme ekle
- `DELETE /api/treatments/{id}/mappings/{mappingId}` — eşleştirme sil
- `GET /api/pricing/treatment/{id}/price?branchId=&institutionId=&isOss=&campaignCode=` — fiyat hesapla
- `GET/PATCH /api/pricing/branches[/{id}/multiplier]` — şube MULTI ayarı
- `GET/PUT /api/pricing/reference-lists[/{id}/items/{code}]` — referans listeler
- `GET/POST/PUT/DELETE /api/campaigns` — kampanya CRUD
- `GET/PUT /api/settings/company` — şirket bilgileri
- `GET/POST/PUT/DELETE /api/settings/branches` — şube CRUD (tam: listeleme, detay, oluştur, güncelle, sil)
- `GET /api/settings/branches/{id}` — şube detay (kullanıcılar dahil)
- `GET /api/settings/branches/{id}/users` — şubeye atanmış kullanıcılar
- `GET/POST/PUT/DELETE /api/settings/users` — kullanıcı CRUD (tam: listeleme, detay, oluştur, güncelle, sil)
- `POST/DELETE /api/settings/users/{id}/roles` — rol atama/kaldırma
- `GET /api/settings/roles` — rol şablonları + izinler
- `GET /api/settings/permissions` — tüm izin listesi
- `GET/PUT /api/settings/branches/{id}/security-policy` — şube güvenlik politikası

## Henüz Yapılmayan / Eksik Alanlar
> git log ile doğrula, bu liste stalest olabilir

- [ ] Lab iş takibi modülü (SPEC §LABORATUVAR)
- [ ] Randevu takvimi UI
- [ ] Hasta portalı frontend
- [ ] E-fatura entegrasyonu tam implement
- [ ] BackupJob gerçek implementasyon (şu an stub)
- [ ] 2FA login flow entegrasyonu (kurulum var, login'e bağlı değil)
- [ ] Raporlama modülü (ReportsController var, query'ler eksik)
- [ ] Push notification / WebSocket event'leri
- [ ] Fatura / ödeme PDF
- [ ] Çoklu dil frontend entegrasyonu
- [ ] Treatment catalog: kategori CRUD UI (şu an sadece listeleme var)

## Kayıtlı Servisler (DI)
FormulaEngine, PricingEngine → Singleton
PricingEngine.Calculate(ctx) veya EvaluateFormula(formula, ctx) kullan

## Sık Kullanılan Dosyalar
- `src/Oravity.Infrastructure/Database/AppDbContext.cs` — tüm DbSet + config
- `src/Oravity.Core/Filters/RequirePermissionAttribute.cs`
- `src/Oravity.Core/Middleware/GlobalExceptionHandler.cs`
- `src/Oravity.Infrastructure/InfrastructureServiceRegistration.cs`
