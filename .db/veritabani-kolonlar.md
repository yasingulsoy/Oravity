# Oravity — tablo ve kolon sözlüğü

Kaynak: `src/Oravity.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` (EF Core model anlık görüntüsü).

- **Tablo adı:** PostgreSQL `snake_case` tablo adı.
- **Kolon:** C# entity property adı (EF varsayılanıyla PascalCase); veritabanında çoğunlukla snake_case eşlemesi migration’da tanımlıdır.
- **Açıklamalar:** `.db/column_tr_data.py` + `.db/column_tr.py` içindeki sözlükten; güncellemek için önce bu dosyaları, ardından `python .db/_extract_schema.py` çalıştırın.

**Özet:** 83 tablo, 1092 kolon.

---

## `appointment_statuses`

*EF entity:* `AppointmentStatus`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `integer` | Birincil anahtar (surrogate key). |
| `AllowedNextStatusIds` | `text` | İzin verilen sonraki durum ID’leri JSON dizi. |
| `BorderColor` | `character varying(7)` | Kenarlık rengi. |
| `ClassName` | `character varying(50)` | CSS sınıf adı. |
| `Code` | `character varying(50)` | Kısa kod / benzersiz tanımlayıcı. |
| `ContainerColor` | `character varying(7)` | Blok arka plan rengi. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsPatientStatus` | `boolean` | Hasta randevusu durumu mu (hekim bloğu değil). |
| `Name` | `character varying(100)` | Görünen ad. |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |
| `TextColor` | `character varying(7)` | Metin rengi. |
| `TitleColor` | `character varying(7)` | Başlık şeridi rengi (hex). |

## `appointment_types`

*EF entity:* `AppointmentType`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `integer` | Birincil anahtar (surrogate key). |
| `Code` | `character varying(50)` | Kısa kod / benzersiz tanımlayıcı. |
| `Color` | `character varying(7)` | Görsel renk (hex). |
| `DefaultDurationMinutes` | `integer` | Varsayılan süre (dakika). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsPatientAppointment` | `boolean` | Hasta randevusu mu; false ise hekim bloğu. |
| `Name` | `character varying(100)` | Görünen ad. |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |

## `appointments`

*EF entity:* `Appointment`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AppointmentNo` | `character varying(50)` | İnsan okunur randevu numarası (ör. APT-2025-0001). |
| `AppointmentTypeId` | `integer` | Randevu tipi FK (`appointment_types.id`). |
| `BookingSource` | `character varying(50)` | Randevunun kaynağı: online, phone, walk_in, manual vb. |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedByUserId` | `bigint` | Kaydı oluşturan kullanıcı (`users.id`). |
| `DoctorId` | `bigint` | Hekim (kullanıcı) FK (`users.id`). |
| `EndTime` | `timestamp with time zone` | Bitiş saati. |
| `EnteredRoomAt` | `timestamp with time zone` | Hastanın muayene odasına alınma zamanı. |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `IsEarlierRequest` | `boolean` | Erken sıra / öncelik talebi bayrağı. |
| `IsNewPatient` | `boolean` | İlk geliş / yeni hasta olarak işaretlendi mi. |
| `IsUrgent` | `boolean` | Acil randevu işareti. |
| `LeftClinicAt` | `timestamp with time zone` | Hastanın klinikten ayrılma zamanı. |
| `LeftRoomAt` | `timestamp with time zone` | Hastanın odadan çıkış zamanı. |
| `Notes` | `text` | Serbest metin notu. |
| `PatientArrivedAt` | `timestamp with time zone` | Hastanın kliniğe geliş zamanı. |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `RowVersion` | `integer` | Optimistic concurrency: eşzamanlı güncelleme çakışması kontrolü. |
| `SendSmsNotification` | `boolean` | Bu randevu için SMS hatırlatma gönderilsin mi. |
| `SpecializationId` | `integer` | Uzmanlık alanı FK (`specializations.id`). |
| `StartTime` | `timestamp with time zone` | Başlangıç saati (randevu veya günlük program). |
| `StatusId` | `integer` | Durum FK (`appointment_statuses.id` veya ilgili lookup). |
| `TenantId` | `bigint` | Çok kiracılı bağlam: oluşturma anındaki tenant kimliği. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `UpdatedByUserId` | `bigint` | Kaydı son güncelleyen kullanıcı (`users.id`). |

## `audit_logs`

*EF entity:* `AuditLog`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Action` | `character varying(100)` | Denetim eylemi: CREATE, UPDATE, DELETE, LOGIN vb. |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `EntityId` | `character varying(100)` | Etkilenen kaydın kimliği (string). |
| `EntityType` | `character varying(100)` | Etkilenen varlık türü adı (örn. Patient). |
| `IpAddress` | `character varying(45)` | İstemci IP adresi (IPv4/IPv6). |
| `NewValues` | `jsonb` | Değişiklik sonrası değerler (JSONB). |
| `OldValues` | `jsonb` | Değişiklik öncesi değerler (JSONB). |
| `UserAgent` | `character varying(500)` | İstemci user-agent bilgisi. |
| `UserEmail` | `character varying(200)` | Denetim anında kullanıcı e-postası (silinse bile saklanır). |
| `UserId` | `bigint` | Kullanıcı FK (`users.id`). |

## `backup_logs`

*EF entity:* `BackupLog`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BackupType` | `character varying(50)` | Yedek türü: full, incremental, schema vb. |
| `Checksum` | `character varying(200)` | Bütünlük için hash/checksum. |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CompletedAt` | `timestamp with time zone` | İşlemin bitiş zamanı. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `DurationSeconds` | `integer` | İşlem süresi (saniye). |
| `ErrorMessage` | `text` | Hata veya işlem mesajı metni. |
| `FileName` | `character varying(500)` | Dosya adı. |
| `FileSizeMb` | `numeric(10,2)` | Dosya boyutu (MB). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `RestoreSuccess` | `boolean` | Geri yükleme testi başarılı mı. |
| `RestoreTestedAt` | `timestamp with time zone` | Geri yükleme testinin yapıldığı zaman. |
| `StartedAt` | `timestamp with time zone` | İşlemin başlangıç zamanı. |
| `Status` | `character varying(20)` | Durum kodu veya enum (bağlama göre). |
| `StorageLocation` | `character varying(1000)` | Yedeğin saklandığı konum/path/URL. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `branch_calendar_settings`

*EF entity:* `BranchCalendarSettings`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `DayEndHour` | `integer` | Gün görünümü bitiş saati. |
| `DayStartHour` | `integer` | Gün görünümü başlangıç saati. |
| `SlotIntervalMinutes` | `integer` | Takvim slot aralığı (dakika). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `branch_online_booking_settings`

*EF entity:* `BranchOnlineBookingSettings`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CancellationHours` | `integer` | Randevudan kaç saat önce iptal hakkı. |
| `IsEnabled` | `boolean` | Özellik/modül açık mı. |
| `LogoUrl` | `character varying(500)` | Logo adresi. |
| `PatientTypeSplit` | `boolean` | Yeni/mevcut hasta ayrımı gösterilsin mi. |
| `PrimaryColor` | `character varying(7)` | Widget birincil rengi (hex). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `WidgetSlug` | `character varying(100)` | Portal/widget URL parçası (benzersiz). |

## `branch_security_policies`

*EF entity:* `BranchSecurityPolicy`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `AllowedIpRanges` | `jsonb` | İzin verilen IP aralıkları (JSON). |
| `LockoutMinutes` | `integer` | Hesap kilidi süresi (dakika). |
| `MaxFailedAttempts` | `integer` | Kilitlemeden önce max hatalı giriş. |
| `SessionTimeoutMinutes` | `integer` | Oturum zaman aşımı (dakika). |
| `TwoFaRequired` | `boolean` | Şubede 2FA zorunlu mu. |
| `TwoFaSkipInternalIp` | `boolean` | İç ağdan girişte 2FA atlanabilsin mi. |

## `branches`

*EF entity:* `Branch`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `DefaultLanguageCode` | `character varying(5)` | Varsayılan dil kodu (örn. tr). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Name` | `character varying(200)` | Görünen ad. |
| `PricingMultiplier` | `numeric` | Şube bazlı fiyat çarpanı (MULTI formül değişkeni). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `VerticalId` | `bigint` | Sektör dikeyi FK (`verticals.id`); şube için opsiyonel override. |

## `campaigns`

*EF entity:* `Campaign`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Code` | `character varying(50)` | Kısa kod / benzersiz tanımlayıcı. |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedBy` | `bigint` | Oluşturan kullanıcı FK (`users.id`) veya ham id. |
| `CreatedByUserId` | `bigint` | Kaydı oluşturan kullanıcı (`users.id`). |
| `Description` | `text` | Açıklama metni. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `LinkedRulePublicId` | `uuid` | İsteğe bağlı bağlı fiyat kuralının `PublicId` değeri. |
| `Name` | `character varying(200)` | Görünen ad. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `TenantId` | `bigint` | Çok kiracılı bağlam: oluşturma anındaki tenant kimliği. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `UpdatedByUserId` | `bigint` | Kaydı son güncelleyen kullanıcı (`users.id`). |
| `ValidFrom` | `timestamp with time zone` | Geçerlilik başlangıcı. |
| `ValidUntil` | `timestamp with time zone` | Geçerlilik bitişi. |

## `cities`

*EF entity:* `City`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CountryId` | `bigint` | Ülke FK (`countries.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Name` | `character varying(100)` | Görünen ad. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `citizenship_types`

*EF entity:* `CitizenshipType`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Code` | `character varying(50)` | Kısa kod / benzersiz tanımlayıcı. |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Name` | `character varying(100)` | Görünen ad. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `companies`

*EF entity:* `Company`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `DefaultLanguageCode` | `character varying(5)` | Varsayılan dil kodu (örn. tr). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Name` | `character varying(200)` | Görünen ad. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `SubscriptionEndsAt` | `timestamp with time zone` | Abonelik bitiş tarihi (lisans). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `VerticalId` | `bigint` | Sektör dikeyi FK (`verticals.id`); şube için opsiyonel override. |

## `complaint_notes`

*EF entity:* `ComplaintNote`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `ComplaintId` | `bigint` | Şikayet FK (`complaints.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedBy` | `bigint` | Oluşturan kullanıcı FK (`users.id`) veya ham id. |
| `IsInternal` | `boolean` | İç not mu (hasta görmez). |
| `Note` | `text` | Kısa not metni. |

## `complaints`

*EF entity:* `Complaint`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AssignedTo` | `bigint` | Atanan kullanıcı. |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedBy` | `bigint` | Oluşturan kullanıcı FK (`users.id`) veya ham id. |
| `Description` | `text` | Açıklama metni. |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `Priority` | `integer` | Öncelik (küçük sayı önce işlenir). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `Resolution` | `text` | Çözüm özeti. |
| `ResolvedAt` | `timestamp with time zone` | Şikayet çözüldüğü zaman. |
| `SlaDueAt` | `timestamp with time zone` | SLA bitiş zamanı. |
| `Source` | `integer` | Kur kaynağı (tcmb, manual…). |
| `Status` | `integer` | Durum kodu veya enum (bağlama göre). |
| `Subject` | `character varying(300)` | Konu (SSO veya mesaj). |
| `SurveyResponseId` | `bigint` | Anket yanıt FK (`survey_responses.id`). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `countries`

*EF entity:* `Country`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `IsoCode` | `character varying(3)` | ISO ülke kodu. |
| `Name` | `character varying(100)` | Görünen ad. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `data_export_requests`

*EF entity:* `DataExportRequest`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CompletedAt` | `timestamp with time zone` | İşlemin bitiş zamanı. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `ExpiresAt` | `timestamp with time zone` | Atama/geçerlilik bitiş zamanı. |
| `FilePath` | `character varying(500)` | Dosya yolu (export ZIP vb.). |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `RequestedBy` | `bigint` | Talebi başlatan kullanıcı. |
| `Status` | `integer` | Durum kodu veya enum (bağlama göre). |

## `districts`

*EF entity:* `District`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CityId` | `bigint` | Şehir FK (`cities.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Name` | `character varying(100)` | Görünen ad. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `doctor_commissions`

*EF entity:* `DoctorCommission`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BaseAmount` | `numeric(18,4)` | TRY bazında karşılık. |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CommissionAmount` | `numeric(12,2)` | Hesaplanan komisyon tutarı. |
| `CommissionRate` | `numeric(5,2)` | Komisyon yüzdesi. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `Currency` | `character varying(3)` | Para birimi kodu (TRY, EUR…). |
| `DistributedAt` | `timestamp with time zone` | Hakedişin dağıtıldığı zaman. |
| `DoctorId` | `bigint` | Hekim (kullanıcı) FK (`users.id`). |
| `ExchangeRate` | `numeric(18,6)` | İşlem anındaki kur (TRY’ye çeviri için). |
| `GrossAmount` | `numeric(12,2)` | Hakediş için brüt tutar. |
| `Status` | `integer` | Durum kodu veya enum (bağlama göre). |
| `TreatmentPlanItemId` | `bigint` | Tedavi planı kalemi FK (`treatment_plan_items.id`). |

## `doctor_on_call_settings`

*EF entity:* `DoctorOnCallSettings`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `DoctorId` | `bigint` | Hekim (kullanıcı) FK (`users.id`). |
| `Friday` | `boolean` | Cuma bayrağı. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `Monday` | `boolean` | Pazartesi nöbet/çalışma bayrağı. |
| `PeriodEnd` | `date` | Dönem bitiş tarihi. |
| `PeriodStart` | `date` | Dönem başlangıç tarihi. |
| `PeriodType` | `integer` | Nöbet dönem tipi (haftalık, aylık…). |
| `Saturday` | `boolean` | Cumartesi bayrağı. |
| `Sunday` | `boolean` | Pazar bayrağı. |
| `Thursday` | `boolean` | Perşembe bayrağı. |
| `Tuesday` | `boolean` | Salı bayrağı. |
| `Wednesday` | `boolean` | Çarşamba bayrağı. |

## `doctor_online_blocks`

*EF entity:* `DoctorOnlineBlock`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedBy` | `bigint` | Oluşturan kullanıcı FK (`users.id`) veya ham id. |
| `DoctorId` | `bigint` | Hekim (kullanıcı) FK (`users.id`). |
| `EndDatetime` | `timestamp with time zone` | Bloğun bitiş zamanı. |
| `Reason` | `character varying(200)` | Sebep (izin, kongre vb.). |
| `StartDatetime` | `timestamp with time zone` | Bloğun başlangıç zamanı (online blok). |

## `doctor_online_booking_settings`

*EF entity:* `DoctorOnlineBookingSettings`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AutoApprove` | `boolean` | Online talep otomatik onaylansın mı. |
| `BookingNote` | `text` | Widget’ta gösterilen yönlendirme notu. |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `DoctorId` | `bigint` | Hekim (kullanıcı) FK (`users.id`). |
| `IsOnlineVisible` | `boolean` | Hekim online takvimde görünsün mü. |
| `MaxAdvanceDays` | `integer` | En fazla kaç gün sonrasına randevu. |
| `PatientTypeFilter` | `integer` | Hangi hasta tipine açık (0/1/2). |
| `SlotDurationMinutes` | `integer` | Online slot süresi (dakika). |
| `SpecialityId` | `bigint` | Online’da filtre uzmanlık ID (opsiyonel). |

## `doctor_online_schedule`

*EF entity:* `DoctorOnlineSchedule`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `BreakEnd` | `time without time zone` | Öğle/mola bitişi. |
| `BreakStart` | `time without time zone` | Öğle/mola başlangıcı. |
| `DayOfWeek` | `integer` | Haftanın günü (1=Pzt … 7=Paz). |
| `DoctorId` | `bigint` | Hekim (kullanıcı) FK (`users.id`). |
| `EndTime` | `time without time zone` | Bitiş saati. |
| `IsWorking` | `boolean` | O gün çalışıyor mu. |
| `StartTime` | `time without time zone` | Başlangıç saati (randevu veya günlük program). |

## `doctor_schedules`

*EF entity:* `DoctorSchedule`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `BreakEnd` | `time without time zone` | Öğle/mola bitişi. |
| `BreakLabel` | `text` | Mola açıklaması. |
| `BreakStart` | `time without time zone` | Öğle/mola başlangıcı. |
| `DayOfWeek` | `integer` | Haftanın günü (1=Pzt … 7=Paz). |
| `DoctorId` | `bigint` | Hekim (kullanıcı) FK (`users.id`). |
| `EndTime` | `time without time zone` | Bitiş saati. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsWorking` | `boolean` | O gün çalışıyor mu. |
| `StartTime` | `time without time zone` | Başlangıç saati (randevu veya günlük program). |

## `doctor_special_days`

*EF entity:* `DoctorSpecialDay`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `DoctorId` | `bigint` | Hekim (kullanıcı) FK (`users.id`). |
| `EndTime` | `time without time zone` | Bitiş saati. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `Reason` | `character varying(200)` | Sebep (izin, kongre vb.). |
| `SpecificDate` | `date` | Tek seferlik özel gün tarihi. |
| `StartTime` | `time without time zone` | Başlangıç saati (randevu veya günlük program). |
| `Type` | `integer` | Tür kodu (bağlama göre). |

## `einvoice_integrations`

*EF entity:* `EInvoiceIntegration`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Address` | `text` | Açık adres. |
| `AutoSendEArchive` | `boolean` | Ödeme sonrası otomatik e-arşiv. |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CompanyTitle` | `character varying(300)` | Fatura/entegrasyonda geçen tüzel unvan. |
| `Config` | `jsonb` | API yapılandırması (şifreli JSON). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsTestMode` | `boolean` | Test ortamı bayrağı. |
| `Provider` | `character varying(50)` | Entegratör sağlayıcı kodu. |
| `TaxOffice` | `character varying(100)` | Vergi dairesi. |
| `Vkn` | `character varying(10)` | Vergi kimlik numarası. |

## `einvoice_items`

*EF entity:* `EInvoiceItem`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Description` | `character varying(500)` | Açıklama metni. |
| `DiscountAmount` | `numeric(12,2)` | İndirim tutarı. |
| `DiscountRate` | `numeric(5,2)` | İndirim yüzdesi. |
| `EInvoiceId` | `bigint` | E-belge başlık FK (`einvoices.id`). |
| `Quantity` | `numeric(10,3)` | Miktar. |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |
| `TaxAmount` | `numeric(12,2)` | KDV tutarı. |
| `TaxRate` | `numeric(5,2)` | KDV oranı. |
| `Total` | `numeric(12,2)` | Toplam. |
| `Unit` | `character varying(20)` | Birim (adet vb.). |
| `UnitPrice` | `numeric(12,2)` | Birim fiyat. |

## `einvoices`

*EF entity:* `EInvoice`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BaseAmount` | `numeric(18,4)` | TRY bazında karşılık. |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CancelReason` | `text` | İptal nedeni. |
| `CancelledAt` | `timestamp with time zone` | İptal zamanı. |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedBy` | `bigint` | Oluşturan kullanıcı FK (`users.id`) veya ham id. |
| `Currency` | `character varying(3)` | Para birimi kodu (TRY, EUR…). |
| `DiscountAmount` | `numeric(12,2)` | İndirim tutarı. |
| `EInvoiceNo` | `character varying(50)` | GİB/entegratör fatura numarası. |
| `ExchangeRate` | `numeric(18,6)` | İşlem anındaki kur (TRY’ye çeviri için). |
| `GibResponse` | `jsonb` | GİB yanıtı JSON. |
| `GibStatus` | `character varying(50)` | GİB durumu (WAITING/ACCEPTED…). |
| `GibUuid` | `character varying(100)` | GİB UUID. |
| `InvoiceDate` | `date` | Fatura tarihi. |
| `InvoiceType` | `integer` | E-belge türü (e-arşiv, e-fatura…). |
| `IsCancelled` | `boolean` | Belge iptal edildi mi. |
| `LanguageCode` | `character varying(5)` | Belge dili. |
| `PaymentId` | `bigint` | Ödeme FK (`payments.id`). |
| `PdfPath` | `character varying(500)` | PDF dosya yolu. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `ReceiverAddress` | `text` | Alıcı adres. |
| `ReceiverEmail` | `character varying(200)` | Alıcı e-posta. |
| `ReceiverName` | `character varying(300)` | Alıcı ad/unvan. |
| `ReceiverTaxOffice` | `character varying(100)` | Alıcı vergi dairesi. |
| `ReceiverTc` | `character varying(11)` | Alıcı TC (fatura belgesi). |
| `ReceiverType` | `integer` | Alıcı tipi: gerçek/tüzel. |
| `ReceiverVkn` | `character varying(10)` | Alıcı VKN. |
| `SentToGibAt` | `timestamp with time zone` | GİB’e gönderilme zamanı. |
| `SentToReceiverAt` | `timestamp with time zone` | Alıcıya e-posta gönderim zamanı. |
| `Sequence` | `integer` | Sayısal sıra. |
| `Series` | `character varying(3)` | Seri öneki. |
| `Subtotal` | `numeric(12,2)` | Ara toplam. |
| `TaxAmount` | `numeric(12,2)` | KDV tutarı. |
| `TaxRate` | `numeric(5,2)` | KDV oranı. |
| `TaxableAmount` | `numeric(12,2)` | Matrah. |
| `Total` | `numeric(12,2)` | Toplam. |

## `exchange_rate_differences`

*EF entity:* `ExchangeRateDifference`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `ActualRate` | `numeric(18,6)` | Fiili kur. |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `Currency` | `character varying(3)` | Para birimi kodu (TRY, EUR…). |
| `DifferenceAmount` | `numeric(18,4)` | Kur farkı TRY tutarı. |
| `DifferenceType` | `integer` | Fark tipi: kâr/zarar. |
| `ForeignAmount` | `numeric(18,4)` | Döviz cinsinden tutar. |
| `Notes` | `character varying(500)` | Serbest metin notu. |
| `OriginalRate` | `numeric(18,6)` | İşlem anındaki kur. |
| `RecordedAt` | `timestamp with time zone` | Kayıt zamanı. |
| `SourceId` | `bigint` | Kaynak kaydın id’si (kur farkı bağlamında). |
| `SourceType` | `character varying(50)` | Kaynak tipi (SUT, SGK, payment, einvoice… bağlama göre). |

## `exchange_rate_overrides`

*EF entity:* `ExchangeRateOverride`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedBy` | `bigint` | Oluşturan kullanıcı FK (`users.id`) veya ham id. |
| `Currency` | `character varying(3)` | Para birimi kodu (TRY, EUR…). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `Notes` | `character varying(500)` | Serbest metin notu. |
| `Rate` | `numeric(18,6)` | Döviz kuru değeri. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `ValidFrom` | `date` | Geçerlilik başlangıcı. |
| `ValidUntil` | `date` | Geçerlilik bitişi. |

## `exchange_rates`

*EF entity:* `ExchangeRate`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `FromCurrency` | `character varying(3)` | Kur çifti kaynak para. |
| `Rate` | `numeric(18,6)` | Döviz kuru değeri. |
| `RateDate` | `date` | Kurun geçerli olduğu tarih. |
| `Source` | `character varying(20)` | Kur kaynağı (tcmb, manual…). |
| `ToCurrency` | `character varying(3)` | Kur çifti hedef para. |

## `icd_codes`

*EF entity:* `IcdCode`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Category` | `character varying(20)` | Çeviri kategorisi. |
| `Code` | `character varying(20)` | Kısa kod / benzersiz tanımlayıcı. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `Description` | `character varying(500)` | Açıklama metni. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `Type` | `integer` | Tür kodu (bağlama göre). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `institutions`

*EF entity:* `Institution`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Address` | `text` | Açık adres. |
| `City` | `character varying(100)` | Şehir adı (metin). |
| `Code` | `character varying(50)` | Kısa kod / benzersiz tanımlayıcı. |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `ContactPerson` | `character varying(200)` | Yetkili kişi. |
| `ContactPhone` | `character varying(30)` | Yetkili telefon. |
| `Country` | `character varying(100)` | Ülke adı (metin). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `DiscountRate` | `numeric(5,2)` | İndirim yüzdesi. |
| `District` | `character varying(100)` | İlçe adı (metin). |
| `Email` | `character varying(200)` | E-posta adresi. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `MarketSegment` | `character varying(20)` | Pazar segmenti (domestic/international). |
| `Name` | `character varying(200)` | Görünen ad. |
| `Notes` | `text` | Serbest metin notu. |
| `PaymentDays` | `integer` | Vade günü. |
| `PaymentTerms` | `text` | Ödeme koşulları metni. |
| `Phone` | `character varying(30)` | Cep telefonu. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `TaxNumber` | `character varying(20)` | Vergi numarası. |
| `TaxOffice` | `character varying(200)` | Vergi dairesi. |
| `Type` | `character varying(50)` | Tür kodu (bağlama göre). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `Website` | `character varying(300)` | Web sitesi URL. |

## `kvkk_consent_logs`

*EF entity:* `KvkkConsentLog`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `ConsentType` | `character varying(100)` | KVKK onay türü kodu. |
| `GivenAt` | `timestamp with time zone` | Kayıt zamanı. |
| `IpAddress` | `character varying(45)` | İstemci IP adresi (IPv4/IPv6). |
| `IsGiven` | `boolean` | Onay verildi mi. |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `RevokedAt` | `timestamp with time zone` | Geri çekilme zamanı. |

## `languages`

*EF entity:* `Language`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Code` | `character varying(5)` | Kısa kod / benzersiz tanımlayıcı. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `Direction` | `character varying(3)` | Yazı yönü (ltr/rtl). |
| `FlagEmoji` | `character varying(10)` | Bayrak emoji. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDefault` | `boolean` | Varsayılan dil mi. |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Name` | `character varying(100)` | Görünen ad. |
| `NativeName` | `character varying(100)` | Dilin kendi adı. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `login_attempts`

*EF entity:* `LoginAttempt`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `Identifier` | `character varying(300)` | Giriş denemesinde kullanılan tanımlayıcı (e-posta vb.). |
| `IpAddress` | `character varying(45)` | İstemci IP adresi (IPv4/IPv6). |
| `Success` | `boolean` | Giriş başarılı mı. |

## `nationalities`

*EF entity:* `Nationality`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Code` | `character varying(10)` | Kısa kod / benzersiz tanımlayıcı. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Name` | `character varying(100)` | Görünen ad. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `notifications`

*EF entity:* `Notification`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `IsRead` | `boolean` | Okundu mu (bildirim). |
| `IsUrgent` | `boolean` | Acil randevu işareti. |
| `Message` | `text` | Mesaj gövdesi veya SMS metni. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `ReadAt` | `timestamp with time zone` | Bildirimin okunduğu zaman. |
| `RelatedEntityId` | `bigint` | İlişkili kayıt kimliği. |
| `RelatedEntityType` | `character varying(50)` | İlişkili kayıt türü. |
| `Title` | `character varying(200)` | Hekim unvanı (Dr., Dt. vb.). |
| `ToRole` | `integer` | Hedef rol dağıtım kodu. |
| `ToUserId` | `bigint` | Hedef kullanıcı. |
| `Type` | `integer` | Tür kodu (bağlama göre). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `online_booking_requests`

*EF entity:* `OnlineBookingRequest`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AppointmentId` | `bigint` | Randevu FK (`appointments.id`). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `DoctorId` | `bigint` | Hekim (kullanıcı) FK (`users.id`). |
| `Email` | `character varying(200)` | E-posta adresi. |
| `FirstName` | `character varying(100)` | Ad. |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `LastName` | `character varying(100)` | Soyad. |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `PatientNote` | `text` | Hastanın online notu. |
| `PatientType` | `integer` | Yeni/mevcut hasta tipi. |
| `Phone` | `character varying(20)` | Cep telefonu. |
| `PhoneVerified` | `boolean` | Telefon doğrulandı mı. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `RejectionReason` | `text` | Red gerekçesi. |
| `RequestedDate` | `date` | Online talep edilen tarih. |
| `RequestedTime` | `time without time zone` | Online talep edilen saat. |
| `ReviewedAt` | `timestamp with time zone` | Onay zamanı. |
| `ReviewedBy` | `bigint` | Onaylayan kullanıcı. |
| `SlotDuration` | `integer` | Talep edilen slot süresi (dakika). |
| `Source` | `integer` | Kur kaynağı (tcmb, manual…). |
| `Status` | `integer` | Durum kodu veya enum (bağlama göre). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `VerificationCode` | `character varying(6)` | SMS doğrulama kodu. |
| `VerificationExpires` | `timestamp with time zone` | Kod geçerlilik süresi. |

## `outbox_messages`

*EF entity:* `OutboxMessage`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AttemptCount` | `integer` | Deneme sayısı. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `ErrorMessage` | `text` | Hata veya işlem mesajı metni. |
| `EventType` | `character varying(200)` | Outbox olay tipi. |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `MaxAttempts` | `integer` | Maksimum deneme. |
| `NextRetryAt` | `timestamp with time zone` | Sonraki yeniden deneme zamanı. |
| `Payload` | `jsonb` | Olay gövdesi JSON. |
| `ProcessedAt` | `timestamp with time zone` | İşlendiği zaman. |
| `Status` | `integer` | Durum kodu veya enum (bağlama göre). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `patient_anamnesis`

*EF entity:* `PatientAnamnesis`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AdditionalNotes` | `text` | Ek notlar. |
| `AlcoholUse` | `integer` | Alkol kullanım kodu. |
| `AnticoagulantDrug` | `character varying(200)` | Antikoagülan ilaç adı. |
| `BisphosphonateUse` | `boolean` | Bifosfonat kullanımı. |
| `BleedingTendency` | `boolean` | Kanama eğilimi. |
| `BloodType` | `character varying(5)` | Kan grubu. |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `BrushingFrequency` | `integer` | Fırçalama sıklığı. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `FilledAt` | `timestamp with time zone` | Doldurulma zamanı. |
| `FilledBy` | `bigint` | Formu dolduran kullanıcı. |
| `HasAspirinAllergy` | `boolean` | Aspirin alerjisi. |
| `HasAsthma` | `boolean` | Astım. |
| `HasDiabetes` | `boolean` | Diyabet. |
| `HasEpilepsy` | `boolean` | Epilepsi. |
| `HasHeartDisease` | `boolean` | Kalp hastalığı. |
| `HasHepatitisB` | `boolean` | Hepatit B. |
| `HasHepatitisC` | `boolean` | Hepatit C. |
| `HasHiv` | `boolean` | HIV. |
| `HasHypertension` | `boolean` | Hipertansiyon. |
| `HasKidneyDisease` | `boolean` | Böbrek hastalığı. |
| `HasLatexAllergy` | `boolean` | Lateks alerjisi. |
| `HasLiverDisease` | `boolean` | Karaciğer hastalığı. |
| `HasPacemaker` | `boolean` | Pil. |
| `HasPenicillinAllergy` | `boolean` | Penisilin alerjisi. |
| `IsBreastfeeding` | `boolean` | Emzirme. |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `IsPregnant` | `boolean` | Gebelik. |
| `LocalAnesthesiaAllergy` | `boolean` | Lokal anestezi alerjisi var mı. |
| `LocalAnesthesiaAllergyNote` | `text` | Lokal anestezi alerji notu. |
| `OnAnticoagulant` | `boolean` | Antikoagülan kullanımı. |
| `OtherAllergies` | `text` | Diğer alerjiler. |
| `OtherSystemicDiseases` | `text` | Diğer sistemik hastalıklar. |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `PreviousSurgeries` | `text` | Geçmiş ameliyatlar. |
| `ProtocolId` | `bigint` | Protokol FK (`protocols.id`). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `SmokingAmount` | `character varying(50)` | Sigara miktarı. |
| `SmokingStatus` | `integer` | Sigara durumu kodu. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `UpdatedBy` | `bigint` | Son güncelleyen kullanıcı id (ham). |
| `UpdatedByAt` | `timestamp with time zone` | Güncelleme zamanı (anamnez). |
| `UsesFloss` | `boolean` | Diş ipi kullanımı. |

## `patient_emergency_contacts`

*EF entity:* `PatientEmergencyContact`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Address` | `text` | Açık adres. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `Email` | `character varying(200)` | E-posta adresi. |
| `FullName` | `character varying(200)` | Ad soyad. |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `Phone` | `character varying(20)` | Cep telefonu. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `Relationship` | `character varying(100)` | Yakınlık (anne, eş vb.). |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `patient_files`

*EF entity:* `PatientFile`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `Category` | `character varying(100)` | Çeviri kategorisi. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `DeletedAt` | `timestamp with time zone` | Silinme zamanı (not/dosya). |
| `FileExt` | `character varying(10)` | Dosya uzantısı. |
| `FilePath` | `character varying(500)` | Dosya yolu (export ZIP vb.). |
| `FileSize` | `integer` | Dosya boyutu bayt. |
| `FileType` | `integer` | Dosya türü enum. |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Note` | `text` | Kısa not metni. |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `TakenAt` | `timestamp with time zone` | Çekim/tarih. |
| `Title` | `character varying(300)` | Hekim unvanı (Dr., Dt. vb.). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `UploadedAt` | `timestamp with time zone` | Yükleme zamanı. |
| `UploadedBy` | `bigint` | Yükleyen kullanıcı. |

## `patient_medications`

*EF entity:* `PatientMedication`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AddedAt` | `timestamp with time zone` | Ekleme zamanı. |
| `AddedBy` | `bigint` | Ekleyen kullanıcı. |
| `Dose` | `character varying(100)` | Doz. |
| `DrugName` | `character varying(300)` | İlaç adı. |
| `Frequency` | `character varying(100)` | Kullanım sıklığı. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `Reason` | `character varying(300)` | Sebep (izin, kongre vb.). |

## `patient_notes`

*EF entity:* `PatientNote`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AppointmentId` | `bigint` | Randevu FK (`appointments.id`). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `Content` | `text` | Not içeriği. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedBy` | `bigint` | Oluşturan kullanıcı FK (`users.id`) veya ham id. |
| `DeletedAt` | `timestamp with time zone` | Silinme zamanı (not/dosya). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `IsHidden` | `boolean` | Gizli not. |
| `IsPinned` | `boolean` | Üstte sabitle. |
| `NoteUpdatedAt` | `timestamp with time zone` | Not güncelleme zamanı. |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `Title` | `character varying(300)` | Hekim unvanı (Dr., Dt. vb.). |
| `Type` | `integer` | Tür kodu (bağlama göre). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `UpdatedBy` | `bigint` | Son güncelleyen kullanıcı id (ham). |

## `patient_portal_accounts`

*EF entity:* `PatientPortalAccount`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `Email` | `character varying(200)` | E-posta adresi. |
| `EmailVerificationToken` | `character varying(200)` | E-posta doğrulama token’ı. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `IsEmailVerified` | `boolean` | E-posta doğrulandı mı. |
| `IsPhoneVerified` | `boolean` | Telefon doğrulandı mı. |
| `LastLoginAt` | `timestamp with time zone` | Son başarılı giriş zamanı. |
| `PasswordHash` | `character varying(500)` | Parolanın hash’i (düz metin saklanmaz). |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `Phone` | `character varying(20)` | Cep telefonu. |
| `PhoneVerificationCode` | `character varying(6)` | Telefon OTP. |
| `PreferredLanguageCode` | `character varying(5)` | Kullanıcı tercih dili (null ise şube/şirket varsayılanı). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `VerificationExpires` | `timestamp with time zone` | Kod geçerlilik süresi. |

## `patient_portal_sessions`

*EF entity:* `PatientPortalSession`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AccountId` | `bigint` | Portal hesabı FK (`patient_portal_accounts.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `ExpiresAt` | `timestamp with time zone` | Atama/geçerlilik bitiş zamanı. |
| `IpAddress` | `character varying(45)` | İstemci IP adresi (IPv4/IPv6). |
| `IsRevoked` | `boolean` | İptal edilmiş token. |
| `TokenHash` | `character varying(500)` | Token’ın hash’i (refresh veya cihaz token’ı). |
| `UserAgent` | `character varying(500)` | İstemci user-agent bilgisi. |

## `patients`

*EF entity:* `Patient`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Address` | `text` | Açık adres. |
| `AgreementInstitutionId` | `bigint` | Anlaşmalı kurum FK (`institutions.id`). |
| `BirthDate` | `date` | Doğum tarihi. |
| `BloodType` | `character varying(5)` | Kan grubu. |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CampaignOptIn` | `boolean` | Kampanya/pazarlama ileti onayı. |
| `CitizenshipTypeId` | `bigint` | Vatandaşlık tipi FK (`citizenship_types.id`). |
| `City` | `character varying(100)` | Şehir adı (metin). |
| `Country` | `character varying(100)` | Ülke adı (metin). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedByUserId` | `bigint` | Kaydı oluşturan kullanıcı (`users.id`). |
| `District` | `character varying(100)` | İlçe adı (metin). |
| `Email` | `character varying(200)` | E-posta adresi. |
| `FatherName` | `character varying(200)` | Baba adı. |
| `FirstName` | `character varying(200)` | Ad. |
| `Gender` | `character varying(10)` | Cinsiyet kodu. |
| `HomePhone` | `character varying(20)` | Ev telefonu. |
| `InsuranceInstitutionId` | `bigint` | ÖSS / sigorta kurumu FK (`institutions.id`). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `LastName` | `character varying(200)` | Soyad. |
| `MaritalStatus` | `character varying(20)` | Medeni hal. |
| `MotherName` | `character varying(200)` | Anne adı. |
| `Nationality` | `character varying(100)` | Uyruk metni (serbest veya kod). |
| `Neighborhood` | `character varying(200)` | Mahalle. |
| `Notes` | `text` | Serbest metin notu. |
| `Occupation` | `character varying(200)` | Meslek. |
| `PassportNoEncrypted` | `character varying(500)` | Pasaport numarası şifreli. |
| `Phone` | `character varying(20)` | Cep telefonu. |
| `PreferredLanguageCode` | `character varying(5)` | Kullanıcı tercih dili (null ise şube/şirket varsayılanı). |
| `PregnancyStatus` | `integer` | Gebelik/emzirme durumu kodu. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `ReferralPerson` | `character varying(200)` | Hastayı yönlendiren kişi adı. |
| `ReferralSourceId` | `bigint` | Geliş kaynağı FK (`referral_sources.id`). |
| `SmokingType` | `character varying(20)` | Sigara kullanım tipi. |
| `SmsOptIn` | `boolean` | SMS bilgilendirme onayı. |
| `TcNumberEncrypted` | `character varying(500)` | TC Kimlik No şifreli saklama (AES). |
| `TcNumberHash` | `character varying(64)` | TC için arama amaçlı hash (SHA-256). |
| `TenantId` | `bigint` | Çok kiracılı bağlam: oluşturma anındaki tenant kimliği. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `UpdatedByUserId` | `bigint` | Kaydı son güncelleyen kullanıcı (`users.id`). |
| `WorkPhone` | `character varying(20)` | İş telefonu. |

## `payment_allocations`

*EF entity:* `PaymentAllocation`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AllocatedAmount` | `numeric(12,2)` | Ödemeden kaleme yazılan tutar. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `IsRefunded` | `boolean` | Ödeme iade edildi mi. |
| `PaymentId` | `bigint` | Ödeme FK (`payments.id`). |
| `TreatmentPlanItemId` | `bigint` | Tedavi planı kalemi FK (`treatment_plan_items.id`). |

## `payments`

*EF entity:* `Payment`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Amount` | `numeric(12,2)` | Tutar. |
| `BaseAmount` | `numeric(18,4)` | TRY bazında karşılık. |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedByUserId` | `bigint` | Kaydı oluşturan kullanıcı (`users.id`). |
| `Currency` | `character varying(3)` | Para birimi kodu (TRY, EUR…). |
| `ExchangeRate` | `numeric(18,6)` | İşlem anındaki kur (TRY’ye çeviri için). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `IsRefunded` | `boolean` | Ödeme iade edildi mi. |
| `Method` | `integer` | Ödeme yöntemi enum/int. |
| `Notes` | `text` | Serbest metin notu. |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `PaymentDate` | `date` | Ödeme tarihi (gün). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `TenantId` | `bigint` | Çok kiracılı bağlam: oluşturma anındaki tenant kimliği. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `UpdatedByUserId` | `bigint` | Kaydı son güncelleyen kullanıcı (`users.id`). |

## `permissions`

*EF entity:* `Permission`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Action` | `character varying(50)` | İzin eylemi (read, create, update, delete, export). |
| `Code` | `character varying(100)` | Kısa kod / benzersiz tanımlayıcı. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `IsDangerous` | `boolean` | Kritik/tehlikeli izin (ek onay gerektirebilir). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `Resource` | `character varying(100)` | İzin kaynağı adı (patient, invoice…). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `pricing_rules`

*EF entity:* `PricingRule`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedBy` | `bigint` | Oluşturan kullanıcı FK (`users.id`) veya ham id. |
| `CreatedByUserId` | `bigint` | Kaydı oluşturan kullanıcı (`users.id`). |
| `Description` | `text` | Açıklama metni. |
| `ExcludeFilters` | `jsonb` | Hariç filtre JSON. |
| `Formula` | `text` | Fiyat formülü metni (FormulaEngine). |
| `IncludeFilters` | `jsonb` | Dahil filtre JSON (tedavi, kampanya kodu vb.). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Name` | `character varying(200)` | Görünen ad. |
| `OutputCurrency` | `character varying(3)` | Çıktı para birimi. |
| `Priority` | `integer` | Öncelik (küçük sayı önce işlenir). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `RuleType` | `character varying(50)` | Kural tipi: percentage, fixed, formula. |
| `StopProcessing` | `boolean` | İlk eşleşmede sonraki kuralları atla. |
| `TenantId` | `bigint` | Çok kiracılı bağlam: oluşturma anındaki tenant kimliği. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `UpdatedByUserId` | `bigint` | Kaydı son güncelleyen kullanıcı (`users.id`). |
| `ValidFrom` | `timestamp with time zone` | Geçerlilik başlangıcı. |
| `ValidUntil` | `timestamp with time zone` | Geçerlilik bitişi. |

## `protocol_sequences`

*EF entity:* `ProtocolSequence`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `Year` | `integer` | Liste yılı veya protokol yılı. |
| `LastSeq` | `integer` | Son kullanılan sıra (sayacın değeri). |

## `protocol_types`

*EF entity:* `ProtocolTypeSetting`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `integer` | Birincil anahtar (surrogate key). |
| `Code` | `character varying(50)` | Kısa kod / benzersiz tanımlayıcı. |
| `Color` | `character varying(7)` | Görsel renk (hex). |
| `Description` | `character varying(500)` | Açıklama metni. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `Name` | `character varying(100)` | Görünen ad. |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |

## `protocols`

*EF entity:* `Protocol`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `ChiefComplaint` | `text` | Ana şikayet. |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CompletedAt` | `timestamp with time zone` | İşlemin bitiş zamanı. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedBy` | `bigint` | Oluşturan kullanıcı FK (`users.id`) veya ham id. |
| `Diagnosis` | `text` | Tanı metni (özet). |
| `DoctorId` | `bigint` | Hekim (kullanıcı) FK (`users.id`). |
| `ExaminationFindings` | `text` | Muayene bulguları. |
| `IcdDiagnosesJson` | `text` | Seçilen ICD tanıları JSON dizisi. |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Notes` | `text` | Serbest metin notu. |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `ProtocolNo` | `character varying(20)` | İnsan okunur protokol no (örn. 2026/1452). |
| `ProtocolSeq` | `integer` | Yıl içi sıra numarası. |
| `ProtocolType` | `integer` | Protokol türü enum. |
| `ProtocolYear` | `integer` | Protokol yılı (sıra için). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `StartedAt` | `timestamp with time zone` | İşlemin başlangıç zamanı. |
| `Status` | `integer` | Durum kodu veya enum (bağlama göre). |
| `TreatmentPlan` | `text` | Tedavi planı metni (serbest). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `VisitId` | `bigint` | Ziyaret FK (`visits.id`). |

## `reference_price_items`

*EF entity:* `ReferencePriceItem`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `Currency` | `character varying(3)` | Para birimi kodu (TRY, EUR…). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `ListId` | `bigint` | Referans fiyat listesi FK (`reference_price_lists.id`). |
| `Metadata` | `jsonb` | Ek bilgi JSON (SGK puanı vb.). |
| `Price` | `numeric(12,2)` | Birim veya liste fiyatı (bağlama göre). |
| `PriceKdv` | `numeric(12,2)` | KDV tutarı veya KDV dahil bileşen. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `TreatmentCode` | `character varying(50)` | Referans listedeki tedavi kodu. |
| `TreatmentName` | `character varying(300)` | Referans listedeki tedavi adı. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `ValidFrom` | `timestamp with time zone` | Geçerlilik başlangıcı. |
| `ValidUntil` | `timestamp with time zone` | Geçerlilik bitişi. |

## `reference_price_lists`

*EF entity:* `ReferencePriceList`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Code` | `character varying(50)` | Kısa kod / benzersiz tanımlayıcı. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Name` | `character varying(200)` | Görünen ad. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `SourceType` | `character varying(50)` | Kaynak tipi (SUT, SGK, payment, einvoice… bağlama göre). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `Year` | `integer` | Liste yılı veya protokol yılı. |

## `referral_sources`

*EF entity:* `ReferralSource`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Code` | `character varying(50)` | Kısa kod / benzersiz tanımlayıcı. |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Name` | `character varying(100)` | Görünen ad. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `refresh_tokens`

*EF entity:* `RefreshToken`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `ExpiresAt` | `timestamp with time zone` | Atama/geçerlilik bitiş zamanı. |
| `IpAddress` | `character varying(45)` | İstemci IP adresi (IPv4/IPv6). |
| `IsRevoked` | `boolean` | İptal edilmiş token. |
| `TokenHash` | `character varying(200)` | Token’ın hash’i (refresh veya cihaz token’ı). |
| `UserId` | `bigint` | Kullanıcı FK (`users.id`). |

## `role_template_permissions`

*EF entity:* `RoleTemplatePermission`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `PermissionId` | `bigint` | İzin FK (`permissions.id`). |
| `RoleTemplateId` | `bigint` | Rol şablonu FK (`role_templates.id`). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `role_templates`

*EF entity:* `RoleTemplate`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Code` | `character varying(50)` | Kısa kod / benzersiz tanımlayıcı. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `Description` | `text` | Açıklama metni. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Name` | `character varying(200)` | Görünen ad. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `sms_queue`

*EF entity:* `SmsQueue`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AttemptCount` | `integer` | Deneme sayısı. |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `ErrorMessage` | `text` | Hata veya işlem mesajı metni. |
| `Message` | `text` | Mesaj gövdesi veya SMS metni. |
| `NextRetryAt` | `timestamp with time zone` | Sonraki yeniden deneme zamanı. |
| `ProviderId` | `integer` | SMS sağlayıcı ID. |
| `ProviderMessageId` | `character varying(200)` | SMS sağlayıcı mesaj id. |
| `SentAt` | `timestamp with time zone` | Gönderim zamanı (SMS vb.). |
| `SourceType` | `character varying(50)` | Kaynak tipi (SUT, SGK, payment, einvoice… bağlama göre). |
| `Status` | `integer` | Durum kodu veya enum (bağlama göre). |
| `ToPhone` | `character varying(20)` | Alıcı telefon. |

## `specializations`

*EF entity:* `Specialization`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `integer` | Birincil anahtar (surrogate key). |
| `Code` | `character varying(50)` | Kısa kod / benzersiz tanımlayıcı. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `Name` | `character varying(200)` | Görünen ad. |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |

## `survey_answers`

*EF entity:* `SurveyAnswer`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AnswerBoolean` | `boolean` | Evet/hayır yanıtı. |
| `AnswerScore` | `integer` | Yıldız/puan. |
| `AnswerText` | `text` | Metin yanıtı. |
| `QuestionId` | `bigint` | Anket sorusu FK (`survey_questions.id`). |
| `ResponseId` | `bigint` | Anket yanıt oturumu FK (`survey_responses.id`). |
| `SelectedOption` | `character varying(200)` | Seçilen seçenek etiketi. |

## `survey_questions`

*EF entity:* `SurveyQuestion`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `IsRequired` | `boolean` | Zorunlu soru mu. |
| `Options` | `jsonb` | Çoktan seçmeli seçenekler JSON. |
| `QuestionText` | `text` | Soru metni. |
| `QuestionType` | `integer` | Soru tipi (yıldız, metin…). |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |
| `TemplateId` | `bigint` | Anket şablonu FK (`survey_templates.id`). |

## `survey_responses`

*EF entity:* `SurveyResponse`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AppointmentId` | `bigint` | Randevu FK (`appointments.id`). |
| `AverageScore` | `numeric(3,1)` | Ortalama puan. |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `Channel` | `integer` | Gönderim kanalı. |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CompletedAt` | `timestamp with time zone` | İşlemin bitiş zamanı. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `NpsScore` | `integer` | NPS puanı (0-10). |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `SentAt` | `timestamp with time zone` | Gönderim zamanı (SMS vb.). |
| `Status` | `integer` | Durum kodu veya enum (bağlama göre). |
| `TemplateId` | `bigint` | Anket şablonu FK (`survey_templates.id`). |
| `Token` | `character varying(200)` | Anket veya portal erişim token’ı. |
| `TokenExpiresAt` | `timestamp with time zone` | Token bitiş (portal). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `survey_templates`

*EF entity:* `SurveyTemplate`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedBy` | `bigint` | Oluşturan kullanıcı FK (`users.id`) veya ham id. |
| `Description` | `text` | Açıklama metni. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Name` | `character varying(200)` | Görünen ad. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `TriggerDelayHours` | `integer` | Tetikleyiciden kaç saat sonra gönderim. |
| `TriggerType` | `integer` | Anket tetikleyici türü. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `tooth_condition_history`

*EF entity:* `ToothConditionHistory`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `ChangedAt` | `timestamp with time zone` | Değişim zamanı. |
| `ChangedBy` | `bigint` | Değiştiren kullanıcı. |
| `NewStatus` | `integer` | Yeni diş durumu. |
| `OldStatus` | `integer` | Önceki diş durumu. |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `Reason` | `text` | Sebep (izin, kongre vb.). |
| `ToothNumber` | `character varying(5)` | FDI diş numarası. |

## `tooth_records`

*EF entity:* `ToothRecord`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedByUserId` | `bigint` | Kaydı oluşturan kullanıcı (`users.id`). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Notes` | `text` | Serbest metin notu. |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `RecordedAt` | `timestamp with time zone` | Kayıt zamanı. |
| `RecordedBy` | `bigint` | Kaydı oluşturan kullanıcı (`users.id`). |
| `Status` | `integer` | Durum kodu veya enum (bağlama göre). |
| `Surfaces` | `character varying(20)` | Etkilenen yüzeyler (diş şeması). |
| `TenantId` | `bigint` | Çok kiracılı bağlam: oluşturma anındaki tenant kimliği. |
| `ToothNumber` | `character varying(5)` | FDI diş numarası. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `UpdatedByUserId` | `bigint` | Kaydı son güncelleyen kullanıcı (`users.id`). |

## `translation_keys`

*EF entity:* `TranslationKey`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `Category` | `character varying(100)` | Çeviri kategorisi. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `Description` | `text` | Açıklama metni. |
| `Key` | `character varying(300)` | Çeviri anahtarı (noktalı). |

## `translations`

*EF entity:* `Translation`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `IsReviewed` | `boolean` | İnsan onaylı çeviri mi. |
| `KeyId` | `bigint` | Çeviri anahtarı FK (`translation_keys.id`). |
| `LanguageId` | `bigint` | Dil FK (`languages.id`). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `Value` | `text` | Çeviri metni. |

## `treatment_categories`

*EF entity:* `TreatmentCategory`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedByUserId` | `bigint` | Kaydı oluşturan kullanıcı (`users.id`). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Name` | `character varying(200)` | Görünen ad. |
| `ParentId` | `bigint` | Üst kayıt FK (hiyerarşi: kategori vb.). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |
| `TenantId` | `bigint` | Çok kiracılı bağlam: oluşturma anındaki tenant kimliği. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `UpdatedByUserId` | `bigint` | Kaydı son güncelleyen kullanıcı (`users.id`). |

## `treatment_mappings`

*EF entity:* `TreatmentMapping`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `InternalTreatmentId` | `bigint` | İç tedavi FK (`treatments.id`). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `MappingQuality` | `character varying(20)` | Eşleştirme kalitesi: exact, partial… |
| `Notes` | `text` | Serbest metin notu. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `ReferenceCode` | `character varying(50)` | Eşleşen referans kodu. |
| `ReferenceListId` | `bigint` | Referans liste FK (`reference_price_lists.id`). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `treatment_plan_items`

*EF entity:* `TreatmentPlanItem`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BodyRegionCode` | `character varying(50)` | Diş dışı bölge kodu. |
| `CompletedAt` | `timestamp with time zone` | İşlemin bitiş zamanı. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `DiscountRate` | `numeric(5,2)` | İndirim yüzdesi. |
| `DoctorId` | `bigint` | Hekim (kullanıcı) FK (`users.id`). |
| `FinalPrice` | `numeric(12,2)` | İndirim sonrası net (KDV hariç). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `KdvAmount` | `numeric` | KDV tutarı. |
| `KdvRate` | `numeric` | KDV oranı snapshot. |
| `Notes` | `text` | Serbest metin notu. |
| `PlanId` | `bigint` | Tedavi planı FK (`treatment_plans.id`). |
| `PriceBaseAmount` | `numeric(18,4)` | TRY bazında tutar. |
| `PriceCurrency` | `character varying(3)` | Fiyat para birimi. |
| `PriceExchangeRate` | `numeric(18,6)` | Fiyat hesabında kullanılan kur. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `RateLockType` | `integer` | Kur kilidi tipi (güncel/onay/manuel). |
| `RateLockedAt` | `timestamp with time zone` | Kurun kilitlendiği zaman. |
| `RateLockedValue` | `numeric` | Kilitlenmiş kur değeri. |
| `Status` | `integer` | Durum kodu veya enum (bağlama göre). |
| `ToothNumber` | `character varying(10)` | FDI diş numarası. |
| `ToothSurfaces` | `character varying(20)` | Yüzey kodları (MOD vb.). |
| `TotalAmount` | `numeric` | KDV dahil toplam. |
| `TreatmentId` | `bigint` | Tedavi kataloğu FK (`treatments.id`). |
| `UnitPrice` | `numeric(12,2)` | Birim fiyat. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `treatment_plans`

*EF entity:* `TreatmentPlan`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedByUserId` | `bigint` | Kaydı oluşturan kullanıcı (`users.id`). |
| `DoctorId` | `bigint` | Hekim (kullanıcı) FK (`users.id`). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Name` | `character varying(200)` | Görünen ad. |
| `Notes` | `text` | Serbest metin notu. |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `ProtocolId` | `bigint` | Protokol FK (`protocols.id`). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `Status` | `integer` | Durum kodu veya enum (bağlama göre). |
| `TenantId` | `bigint` | Çok kiracılı bağlam: oluşturma anındaki tenant kimliği. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `UpdatedByUserId` | `bigint` | Kaydı son güncelleyen kullanıcı (`users.id`). |

## `treatments`

*EF entity:* `Treatment`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AllowedScopes` | `integer[]` | İzin verilen kapsam kodları (diş/vücut). |
| `CategoryId` | `bigint` | Kategori FK (`treatment_categories.id` veya ilgili kategori tablosu). |
| `Code` | `character varying(20)` | Kısa kod / benzersiz tanımlayıcı. |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedByUserId` | `bigint` | Kaydı oluşturan kullanıcı (`users.id`). |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `KdvRate` | `numeric(5,2)` | KDV oranı snapshot. |
| `Name` | `character varying(300)` | Görünen ad. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `RequiresLaboratory` | `boolean` | Lab gerektirir mi. |
| `RequiresSurfaceSelection` | `boolean` | Yüzey seçimi zorunlu mu. |
| `SutCode` | `character varying(20)` | SUT işlem kodu. |
| `Tags` | `jsonb` | Etiketler JSON/dizi. |
| `TenantId` | `bigint` | Çok kiracılı bağlam: oluşturma anındaki tenant kimliği. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `UpdatedByUserId` | `bigint` | Kaydı son güncelleyen kullanıcı (`users.id`). |

## `trusted_devices`

*EF entity:* `TrustedDevice`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `DeviceName` | `character varying(200)` | Cihaz adı. |
| `DeviceToken` | `character varying(200)` | Güvenilen cihaz token’ı. |
| `ExpiresAt` | `timestamp with time zone` | Atama/geçerlilik bitiş zamanı. |
| `IpAddress` | `character varying(45)` | İstemci IP adresi (IPv4/IPv6). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `LastUsedAt` | `timestamp with time zone` | Son kullanım zamanı. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `TrustedAt` | `timestamp with time zone` | Güvene alınma zamanı. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `UserId` | `bigint` | Kullanıcı FK (`users.id`). |

## `user_2fa_settings`

*EF entity:* `User2FASettings`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `UserId` | `bigint` | Kullanıcı FK (`users.id`). |
| `BackupCodes` | `jsonb` | Yedek kodlar JSON. |
| `BackupCodesAt` | `timestamp with time zone` | Yedek kodların üretim zamanı. |
| `EmailEnabled` | `boolean` | E-posta ile 2FA. |
| `Last2faAt` | `timestamp with time zone` | Son 2FA doğrulama zamanı. |
| `PreferredMethod` | `character varying(20)` | Tercih edilen 2FA yöntemi. |
| `SmsEnabled` | `boolean` | SMS ile 2FA. |
| `TotpEnabled` | `boolean` | TOTP etkin mi. |
| `TotpSecret` | `text` | Şifreli TOTP secret. |
| `TotpVerifiedAt` | `timestamp with time zone` | TOTP’nin doğrulanma zamanı. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `user_permission_overrides`

*EF entity:* `UserPermissionOverride`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `IsGranted` | `boolean` | Override: true=ek izin, false=açıkça red. |
| `PermissionId` | `bigint` | İzin FK (`permissions.id`). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `UserId` | `bigint` | Kullanıcı FK (`users.id`). |

## `user_role_assignments`

*EF entity:* `UserRoleAssignment`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AssignedAt` | `timestamp with time zone` | Rol atama zamanı. |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `ExpiresAt` | `timestamp with time zone` | Atama/geçerlilik bitiş zamanı. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `RoleTemplateId` | `bigint` | Rol şablonu FK (`role_templates.id`). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `UserId` | `bigint` | Kullanıcı FK (`users.id`). |

## `users`

*EF entity:* `User`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `CalendarColor` | `character varying(7)` | Takvimde hekim rengi (hex). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `DefaultAppointmentDuration` | `integer` | Varsayılan randevu süresi (dakika). |
| `Email` | `character varying(300)` | E-posta adresi. |
| `FullName` | `character varying(200)` | Ad soyad. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsChiefPhysician` | `boolean` | Başhekim işareti (takvim sıralaması vb.). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `IsPlatformAdmin` | `boolean` | Platform yöneticisi (tüm kiracılar). |
| `LastLoginAt` | `timestamp with time zone` | Son başarılı giriş zamanı. |
| `PasswordHash` | `text` | Parolanın hash’i (düz metin saklanmaz). |
| `PreferredLanguageCode` | `character varying(5)` | Kullanıcı tercih dili (null ise şube/şirket varsayılanı). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `SpecializationId` | `integer` | Uzmanlık alanı FK (`specializations.id`). |
| `SsoEmail` | `character varying(200)` | SSO profilinden gelen e-posta. |
| `SsoProvider` | `character varying(50)` | SSO sağlayıcı kodu (microsoft, google…). |
| `SsoSubject` | `character varying(200)` | Sağlayıcıdaki kullanıcı subject/id. |
| `Title` | `character varying(50)` | Hekim unvanı (Dr., Dt. vb.). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `verticals`

*EF entity:* `Vertical`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `BodyChartType` | `character varying(50)` | Vücut şeması tipi. |
| `Code` | `character varying(50)` | Kısa kod / benzersiz tanımlayıcı. |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `DefaultModules` | `text[]` | Varsayılan modül kodları (dizi). |
| `HasBodyChart` | `boolean` | Vücut şeması kullanılıyor mu. |
| `IsActive` | `boolean` | Kayıt aktif mi (pasif kayıtlar genelde seçilmez). |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `Name` | `character varying(200)` | Görünen ad. |
| `PatientLabel` | `character varying(100)` | Hasta etiketi. |
| `ProviderLabel` | `character varying(100)` | Sağlayıcı etiketi (ör. Hekim). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `RequiresKts` | `boolean` | KTS zorunluluğu bayrağı. |
| `SortOrder` | `integer` | Listeleme sırası (küçük önce). |
| `TreatmentLabel` | `character varying(100)` | Tedavi etiketi. |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |

## `visits`

*EF entity:* `Visit`

| Kolon | PostgreSQL tipi | Açıklama |
|-------|-----------------|----------|
| `Id` | `bigint` | Birincil anahtar (surrogate key). |
| `AppointmentId` | `bigint` | Randevu FK (`appointments.id`). |
| `BranchId` | `bigint` | Şube FK (`branches.id`). |
| `CalledAt` | `timestamp with time zone` | Hekimin hastayı çağırdığı zaman. |
| `CheckInAt` | `timestamp with time zone` | Kliniğe giriş zamanı. |
| `CheckOutAt` | `timestamp with time zone` | Çıkış zamanı. |
| `CompanyId` | `bigint` | Şirket FK (`companies.id`). |
| `CreatedAt` | `timestamp with time zone` | Kaydın oluşturulma zamanı (UTC). |
| `CreatedBy` | `bigint` | Oluşturan kullanıcı FK (`users.id`) veya ham id. |
| `IsDeleted` | `boolean` | Soft delete: true ise kayıt mantıksal olarak silinmiş. |
| `IsWalkIn` | `boolean` | Randevusuz (walk-in) ziyaret mi. |
| `Notes` | `text` | Serbest metin notu. |
| `PatientId` | `bigint` | Hasta FK (`patients.id`). |
| `PublicId` | `uuid` | Dış API ve istemcide kullanılan benzersiz UUID. |
| `Status` | `integer` | Durum kodu veya enum (bağlama göre). |
| `UpdatedAt` | `timestamp with time zone` | Son güncelleme zamanı (UTC). |
| `VisitDate` | `date` | Ziyaret tarihi. |
