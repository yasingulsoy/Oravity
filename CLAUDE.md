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
- [ ] **Kambiyo kârı/zararı muhasebesi (VUK 280)**: Dövizli ödemelerde `Payment.ExchangeRate` ile TCMB kuru arasındaki fark ileride 646 (Kambiyo Kârları) / 656 (Kambiyo Zararları) hesabına kayıt edilmeli. Şu an backend ₺1 toleransıyla zorunlu yuvarlama farkını klinik üstleniyor (kabul edilebilir); ama kullanıcı kuru elle değiştirirse büyük fark oluşabilir → `UpdatePaymentRate` komutu + muhasebe kaydı gerekli.
- [ ] **Dövizli ödemede kur kilidi / uyarı**: Ödeme dialogunda TCMB kurundan %X'den fazla sapma olursa kullanıcıya uyarı göster. Büyük sapmalı manuel kur girişi kambiyo kârı/zararı doğurur ve şu an muhasebeleştirilmiyor.

## Vizite & Protokol Akışı (Visit/Protocol Flow)

### Durum Geçişleri

```
Appointment ──► Geldi(3) ──► Odada(5) ──► Ayrıldı(4)
                  │                           ▲
                  ▼                           │
               Visit(Waiting) ──► ProtocolOpened ──► Completed
```

### Adım Adım Kural

| # | Kim | Aksiyon | Backend | Sonuç |
|---|-----|---------|---------|-------|
| 1 | Resepsiyon | Randevu → "Geldi" | `UpdateAppointmentStatusCommand` | Apt→Arrived(3), Visit oluşturulur (Waiting), WaitingList'e düşer |
| 2 | Resepsiyon | WaitingList → "Protokol Aç" | `CreateProtocolCommand` | Protocol oluşturulur, Visit→ProtocolOpened, WaitingList: **"Protokol Açık"** (violet) |
| 3 | Hekim | Dashboard → "Odaya Al" | `StartProtocolCommand` | Protocol.StartedAt=now, Apt→InRoom(5), WaitingList: **"Odada"** (blue) |
| 4 | Hekim | Dashboard → "Protokolü Kapat" | `CompleteProtocolCommand` | Protocol→Completed, auto-checkout: Visit→Completed, Apt→Ayrıldı(4), WaitingList'ten çıkar |
| 5 | Resepsiyon | WaitingList → "Klinikten Çıktı" | `CheckOutVisitCommand` | Manuel çıkış (fallback, açık protokol yoksa çalışır), Apt→Tamamlandı(7) |

### Hekim "Hastayı Çağır" Butonu (RequestPatientCallCommand)
- **Gösterim koşulu:** `apt.statusId === 3 (Arrived) && !isActive && !apt.hasOpenProtocol`
- **Sonuç:** Visit.CalledAt=now, resepsiyona bildirim → WaitingList: **"Çağrıldı"** (amber), AppointmentBlock'ta çan ikonu
- Bu buton yalnızca protokol YOK iken görünür; protokol açıldıktan sonra hekim "Odaya Al" kullanır

### IsBeingCalled Kuralı
- `Visit.CalledAt.HasValue && Visit.Status == Waiting` — her iki sorguda da (GetWaitingList + GetAppointmentsByDate) aynı kural
- Visit.Status ProtocolOpened'a geçince otomatik false olur

### WaitingList Bölümleri (Section)
| Bölüm | Koşul | Renk |
|-------|-------|------|
| Çağrıldı | isBeingCalled=true | Amber |
| Protokol Açık | ProtocolOpened + hasOpenProtocol + protocol.startedAt=null | Violet |
| Odada | ProtocolOpened + hasOpenProtocol + protocol.startedAt!=null | Blue |
| Çıkış Hazır | ProtocolOpened + !hasOpenProtocol | Emerald |
| Bekliyor | Waiting | Gri |

### 1 Vizite = 1 Protokol Kuralı
- `CreateProtocolCommand`: aktif (non-Cancelled) protokol varsa `InvalidOperationException` fırlatır
- Randevulu hastada hekim her zaman randevunun hekiminden belirlenir (`visit.Appointment?.DoctorId ?? request.DoctorId`)

### SignalR Broadcast Haritası
| Command | Broadcast Mesajı | Frontend Etkisi |
|---------|-----------------|-----------------|
| CheckInPatient / UpdateAptStatus(Arrived) | `VisitUpdated` | WaitingList refetch |
| RequestPatientCall | `VisitUpdated` + (PatientInRoom varsa) `CalendarUpdated` | WaitingList + Calendar |
| CreateProtocol | `ProtocolUpdated` | WaitingList + Calendar (AppointmentCalendarPage invalidates both) |
| StartProtocol | `ProtocolUpdated` | WaitingList + Calendar |
| CompleteProtocol | `ProtocolUpdated` + `VisitUpdated` + `CalendarUpdated` | WaitingList + Calendar + DoctorDashboard |
| CheckOutVisit | `VisitUpdated` | WaitingList + Calendar |

### Önemli Dosyalar
- `GetWaitingListQuery.cs` — WaitingList sorgusu, IsBeingCalled ve WaitingProtocolItem.StartedAt dahil
- `GetAppointmentsByDateQuery.cs` — Appointments sorgusu, IsBeingCalled aynı mantıkla
- `RequestPatientCallCommand.cs` — Çağır akışı, ConflictException guard var (ProtocolOpened zaten)
- `CompleteProtocolCommand.cs` — Auto-checkout mantığı (remainingOpen==0 → visit.CheckOut())
- `WaitingList.tsx` — 5 section, getSection() protocol.startedAt'e bakıyor
- `AppointmentBlock.tsx` — isBeingCalled → amber border + "Hekim Çağırdı" chip
- `AppointmentCalendarPage.tsx` — useCalendarSocket → her event'te appointments + visits/waiting invalidate; selectedAppointment live sync useEffect

## Kayıtlı Servisler (DI)
FormulaEngine, PricingEngine → Singleton
PricingEngine.Calculate(ctx) veya EvaluateFormula(formula, ctx) kullan

## Sık Kullanılan Dosyalar
- `src/Oravity.Infrastructure/Database/AppDbContext.cs` — tüm DbSet + config
- `src/Oravity.Core/Filters/RequirePermissionAttribute.cs`
- `src/Oravity.Core/Middleware/GlobalExceptionHandler.cs`
- `src/Oravity.Infrastructure/InfrastructureServiceRegistration.cs`
