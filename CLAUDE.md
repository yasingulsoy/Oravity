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
Appointments, Audit, Auth, BookingRequests, Complaints, DentalChart,
EInvoice, Health, Localization, Notifications, PatientPortal,
PatientRecords, Patients, Payments, **Pricing**, PublicBooking,
PublicSurvey, Reports, **Security** (2FA), Surveys, TreatmentMappings,
TreatmentPlans, **Treatments**

## DB Migration Durumu (son: 20260405)
Oluşturulan tablolar (her şey migrations ile yönetiliyor):
- Core: companies, branches, users, permissions, role_templates, ...
- Klinik: patients, appointments, treatment_plans, treatment_plan_items
- Yeni (04/2026): treatments, treatment_categories, treatment_mappings,
  pricing_rules, reference_price_lists, reference_price_items,
  user_2fa_settings, trusted_devices, branch_security_policies, backup_logs

## Henüz Yapılmayan / Eksik Alanlar
> git log ile doğrula, bu liste stalest olabilir

- [ ] Frontend (React) — büyük çoğunluğu yazılmamış
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

## Kayıtlı Servisler (DI)
FormulaEngine, PricingEngine → Singleton
PricingEngine.Calculate(ctx) veya EvaluateFormula(formula, ctx) kullan

## Sık Kullanılan Dosyalar
- `src/Oravity.Infrastructure/Database/AppDbContext.cs` — tüm DbSet + config
- `src/Oravity.Core/Filters/RequirePermissionAttribute.cs`
- `src/Oravity.Core/Middleware/GlobalExceptionHandler.cs`
- `src/Oravity.Infrastructure/InfrastructureServiceRegistration.cs`
