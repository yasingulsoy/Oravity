# Oravity veritabanı — ayrıntılı referans

Bu belge, **PostgreSQL** üzerinde **Entity Framework Core** ile yönetilen Oravity şemasının **işlevsel ve alan düzeyinde** özetidir. Kaynak: `Oravity.SharedKernel/Entities/*`, `Oravity.Infrastructure/Database/AppDbContext.cs`, `Oravity.Infrastructure/Migrations/`.

**Tüm tablolar ve kolonlar (tip + Türkçe açıklama):** aynı klasördeki [`veritabani-kolonlar.md`](veritabani-kolonlar.md) — `AppDbContextModelSnapshot.cs` ile senkron; güncellemek için `python .db/_extract_schema.py`.

---

## 1. Veritabanının genel rolü

Oravity veritabanı, **çok kiracılı (multi-tenant) diş kliniği yönetim** uygulamasının kalıcı verisini tutar:

- **Kiracı hiyerarşisi:** dikey (sektör) → şirket → şube.
- **Kimlik ve yetki:** kullanıcılar, roller, izinler, JWT/refresh ile oturum.
- **Hasta ve klinik:** hasta kartı, randevu, fiziksel **ziyaret**, **protokol** (muayene/tedavi oturumu), **tedavi planı** ve kalemleri, diş şeması.
- **Fiyatlandırma:** tedavi kataloğu, referans fiyat listeleri, kurallar, kampanyalar, şube çarpanı (`MULTI`).
- **Finans:** ödeme, kaleme dağıtım, hekim hakedişi, döviz kurları ve kur farkı.
- **Operasyon:** SMS kuyruğu, klinik içi bildirimler, outbox, yedek logları, denetim ve KVKK.

**Kimlik sütunları:** Çoğu tabloda `id` (bigint, PK) ve dış dünyaya açılan `public_id` (uuid). **Tenant izolasyonu** çoğunlukla `branch_id` veya `company_id` + uygulama katmanı ile sağlanır; `AuditableEntity` için `tenant_id` oluşturma anında set edilir.

---

## 2. Ortak teknik kalıplar

### 2.1 `BaseEntity` ve `AuditableEntity`

- **`BaseEntity`:** `Id`, `PublicId`, `CreatedAt`, `UpdatedAt`, `IsDeleted` — çoğu iş tablosu için temel.
- **`AuditableEntity`:** Ayrıca `CreatedByUserId`, `UpdatedByUserId`, `TenantId` — `SaveChangesAsync` ile oturumdaki kullanıcıdan doldurulur.
- **Soft delete:** `IsDeleted = true` ile mantıksal silme; birçok entity için global `HasQueryFilter` ile silinenler listeden çıkar. Bazı tablolar (junction, audit, outbox vb.) **istisna** — `AppDbContext.ApplySoftDeleteFilters` içinde listelenir.

### 2.2 JSON / özel tipler

- **ICD tanıları:** `protocols.icd_diagnoses_json` — protokol başına seçilen ICD-10 kayıtları (birincil/ikincil).
- **Randevu durum geçişleri:** `appointment_statuses.allowed_next_status_ids` — JSON dizi.
- **Fiyat kuralları:** `pricing_rules.include_filters` / `exclude_filters` — JSONB (tedavi, kategori, kampanya kodu vb. filtreleri).
- **PostgreSQL dizileri:** Örn. `verticals.default_modules` (`text[]`), `treatments.allowed_scopes` (`integer[]`).

---

## 3. Tablolar — tablo tablo açıklama

Aşağıda **tablo adı** (PostgreSQL `snake_case`) ve **ne işe yaradığı**, önemli alanlar ve ilişkiler özetlenir.

---

### 3.1 Referans ve platform

| Tablo | Ayrıntı |
|--------|---------|
| **`languages`** | Desteklenen diller: `code` (örn. `tr`), `name`, `native_name`, yazı yönü (`ltr`/`rtl`), bayrak emoji, sıralama, aktiflik, varsayılan dil işareti. UI ve çeviri sistemi için. |
| **`verticals`** | Sektör **dikeyi** (ör. diş): `code`, `name`, vücut şeması kullanımı (`has_body_chart`, `body_chart_type`), varsayılan modül listesi (`default_modules`), arayüz etiketleri (hekim/hasta/tedavi), `requires_kts` vb. Şirket ve isteğe bağlı şube bu dikeye bağlanır. |
| **`countries`** | Ülkeler: ad, **ISO 3166-1 alpha-2** (`iso_code`), sıra. |
| **`cities`** | İller/şehirler: `country_id`, ad, sıra. |
| **`districts`** | İlçeler: `city_id`, ad, sıra. |
| **`nationalities`** | Uyruk listesi: ad, kısa kod (örn. `TC`). |

---

### 3.2 Kiracı: şirket ve şube

| Tablo | Ayrıntı |
|--------|---------|
| **`companies`** | **Şirket (üst kiracı):** ad, `vertical_id`, varsayılan dil, `is_active`, abonelik bitiş tarihi. Tüm operasyonel verinin üst sınırı. |
| **`branches`** | **Şube:** `company_id`, ad, isteğe bağlı `vertical_id` (şirket dikeyini override eder, karma klinik senaryosu), varsayılan dil, aktiflik. **`pricing_multiplier`:** fiyat formüllerinde **MULTI** değişkeni (örn. 1.10 = %10 bölgesel çarpan). |

---

### 3.3 Kurumlar (anlaşma / sigorta)

| Tablo | Ayrıntı |
|--------|---------|
| **`institutions`** | Anlaşmalı kurum, sigorta, kurumsal müşteri vb.: `company_id` null = platform geneli şablon; dolu = şirkete özel. Tip (`sigorta`, `kamu`…), pazar segmenti, iletişim, adres, vergi, ödeme vadesi ve iskonto. Hastada **AK** ve **ÖSS** kurumu olarak FK ile kullanılır. |

---

### 3.4 Kullanıcılar, roller, izinler

| Tablo | Ayrıntı |
|--------|---------|
| **`users`** | Uygulama kullanıcısı: e-posta (benzersiz), ad soyad, parola hash, tercih dil, aktiflik, **platform admin**, son giriş. **SSO:** `sso_provider`, `sso_subject`, `sso_email` (OAuth/OpenID benzersiz eşleşme). **Hekim alanları:** unvan, `specialization_id`, takvim rengi, varsayılan randevu süresi, **başhekim** (`is_chief_physician`). |
| **`permissions`** | İzin tanımı: `code` (örn. `patient.view`), `resource`, `action`, `is_dangerous` (kritik işlemler için). |
| **`role_templates`** | Rol şablonu: sabit `code` (örn. `DOCTOR`, `BRANCH_MANAGER`), ad, açıklama. Rol → izin `role_template_permissions` ile bağlanır. |
| **`role_template_permissions`** | **Junction:** hangi rol şablonunun hangi izinlere sahip olduğu. `PublicId` yok; iç kullanım. |
| **`user_role_assignments`** | Kullanıcıya atanmış rol: `user_id`, `role_template_id`, isteğe bağlı `company_id` / `branch_id` (kapsam: platform / firma / şube), `is_active`, `assigned_at`, `expires_at`. Silinmiş kullanıcı filtresi ile birlikte sorgulanır. |
| **`user_permission_overrides`** | Rol dışı **tekil izin ver / reddet:** `user_id`, `permission_id`, isteğe bağlı `company_id` / `branch_id`, `is_granted` (true=ek izin, false=explicit deny). |

---

### 3.5 Oturum ve güvenlik

| Tablo | Ayrıntı |
|--------|---------|
| **`refresh_tokens`** | JWT yenileme: `user_id`, token **hash**, süre, iptal, IP. Silinmiş kullanıcıya ait token’lar gizlenir. |
| **`login_attempts`** | Giriş denemesi: `identifier` (e-posta vb.), IP, başarı/başarısız, zaman — brute force analizi. |
| **`user_2fa_settings`** | Kullanıcı başına 2FA (PK = `user_id`): TOTP (şifreli secret), SMS/e-posta bayrakları, tercih yöntemi, yedek kodlar JSON, son 2FA zamanı. |
| **`trusted_devices`** | Cihaz token’ı ile belirli süre **2FA’sız giriş**; süre dolunca geçersiz. |
| **`branch_security_policies`** | Şube başına (PK = `branch_id`): zorunlu 2FA, iç IP’de atlama, izin verilen IP aralıkları JSON, oturum süresi, kilitleme eşiği ve süresi. |

---

### 3.6 Hasta kartı ve lookup’lar

| Tablo | Ayrıntı |
|--------|---------|
| **`patients`** | **Hasta:** `branch_id` ile tenant. Kimlik: TC ve pasaport **AES-256 şifreli**, TC için **SHA-256 hash** ile arama. Demografik, iletişim, adres, tıbbi (kan grubu), geliş: `referral_source_id`, `referral_person`, **anlaşmalı kurum** ve **ÖSS kurumu** FK’leri, notlar, dil, SMS/pazarlama onayı, aktiflik. |
| **`patient_emergency_contacts`** | Hasta başına en fazla iki acil kişi: sıra, ad, yakınlık, telefon/e-posta/adres. |
| **`citizenship_types`** | Vatandaşlık tipi: `company_id` null = global; dolu = şirkete özel ek kayıt. |
| **`referral_sources`** | Hastanın nereden geldiği: aynı global/şirket mantığı. |

---

### 3.7 Randevu ve takvim altyapısı

| Tablo | Ayrıntı |
|--------|---------|
| **`appointment_statuses`** | Randevu durumu: kod, ad, **takvim renkleri** (hex), `allowed_next_status_ids` (JSON — durum makinesi), `is_patient_status`, sıra. Terminal durumlar taşıma/iptali kilitler. |
| **`appointment_types`** | Randevu tipi: hasta randevusu vs **hekim bloğu** (toplantı/izin), varsayılan süre, renk. |
| **`specializations`** | Hekim uzmanlık alanı (global lookup); `users.specialization_id` ile bağlanır. |
| **`appointments`** | **Randevu:** `branch_id`, `patient_id`, `doctor_id`, `status_id`, `appointment_type_id`, `specialization_id`, başlangıç/bitiş (UTC), `appointment_no`, kaynak (`online` / `phone` / `walk_in` / `manual`), notlar, acil/erken/yeni hasta bayrakları, SMS bildirimi, **optimistic lock** (`row_version`). Hasta akış zamanları: geliş, odaya giriş, çıkış, klinikten ayrılış. |
| **`doctor_schedules`** | Hekimin **haftalık** tekrarlayan çalışma saatleri: şube + gün (1–7), çalışıyor mu, başlangıç/bitiş, öğle arası, mola etiketi. |
| **`doctor_special_days`** | Tek seferlik **override:** o gün farklı saat, izin veya ekstra mesai; `DoctorSpecialDayType` ile tür. |
| **`doctor_on_call_settings`** | Hekim **nöbet** günleri ve dönem tipi (haftalık/aylık vb.); takvimde ayrı gösterim için. |
| **`branch_calendar_settings`** | Şube takvim UI: slot aralığı (dk), gün başlangıç/bitiş saati. |

---

### 3.8 Ziyaret ve protokol

| Tablo | Ayrıntı |
|--------|---------|
| **`visits`** | Hastanın **fiziksel klinik ziyareti:** şube, şirket, hasta, isteğe bağlı `appointment_id` (yoksa walk-in), `visit_date`, check-in/out, **`called_at`** (hekim “çağır”), durum (bekliyor/protokol açıldı/tamamlandı/iptal), notlar, oluşturan kullanıcı. |
| **`protocols`** | Vizit içinde **hekim protokolü:** protokol numarası (yıl + sıra), tip (muayene/tedavi/konsültasyon…), şikayet, bulgu, tanı metni, tedavi planı özeti, notlar, **ICD JSON**, başlangıç/bitiş, durum. Tanı kodları ayrıca `icd_diagnoses_json` içinde tutulur. |
| **`protocol_sequences`** | **Sayaç:** (`branch_id`, `year`) → son sıra numarası; yeni protokol numarası üretmek için. |
| **`protocol_types`** | Protokol tipi **meta** (ad, kod, renk, sıra) — backend enum ile ID eşlemesi. |
| **`icd_codes`** | ICD-10 **kataloğu** (diş odaklı seed): kod, açıklama, kategori, tip. |

---

### 3.9 Tedavi kataloğu ve plan

| Tablo | Ayrıntı |
|--------|---------|
| **`treatment_categories`** | Hiyerarşik kategori: `company_id` null = global şablon; `parent_id` ile ağaç. |
| **`treatments`** | Şirkete özel veya global tedavi: **TDB benzeri kod**, ad, kategori, SUT kodu, etiketler JSON, **KDV oranı**, yüzey/lab gereksinimi, **allowed_scopes** (diş/vücut kapsamı), aktiflik. |
| **`treatment_plans`** | Hasta **tedavi planı:** hasta, şube, sorumlu hekim, ad, durum (taslak/onaylı/tamamlandı/iptal), notlar, isteğe bağlı `protocol_id`. |
| **`treatment_plan_items`** | Plan **satırı:** `treatment_id`, diş numarası/yüzey/bölge, durum, birim fiyat, indirim %, net, **KDV snapshot** (oran, tutar, KDV dahil toplam), para birimi ve kur, kur kilidi tipi, isteğe bağlı **yapan hekim** `doctor_id`, tamamlanma zamanı. |

---

### 3.10 Fiyatlandırma motoru

| Tablo | Ayrıntı |
|--------|---------|
| **`reference_price_lists`** | Resmi/ticari **referans listesi:** kod (örn. `TDB_2026`), ad, kaynak tipi (SUT/SGK/sigorta…), yıl, aktiflik. |
| **`reference_price_items`** | Listeye **ait satır:** referans tedavi kodu/adı, fiyat, KDV’li fiyat, para birimi, geçerlilik aralığı, ek metadata JSON. |
| **`treatment_mappings`** | **İç tedavi** → **referans liste kodu** eşlemesi; kalite (`exact`/`partial`…) ve not. Pricing engine önce mapping, sonra listeden tutar okur. |
| **`pricing_rules`** | Şirket/isteğe bağlı şube kapsamında **kural:** öncelik, `rule_type` (yüzde/sabit/formül), include/exclude JSON filtreleri, formül metni (`FormulaEngine`), para birimi, geçerlilik tarihleri, **`stop_processing`** (ilk eşleşmede dur), aktiflik. |
| **`campaigns`** | **Kampanya:** benzersiz `code` (kural filtrelerinde `campaignCodes` ile eşleşir), ad, açıklama, tarih aralığı, isteğe bağlı bağlı kural `public_id`, aktiflik. |

---

### 3.11 Finans ve döviz

| Tablo | Ayrıntı |
|--------|---------|
| **`payments`** | Hasta **ödemesi:** tutar, para birimi, işlem anı kur ve TRY karşılığı (`base_amount`), yöntem (nakit/kart/havale…), tarih, notlar, **iade** bayrağı. |
| **`payment_allocations`** | Ödemenin **hangi tedavi kalemi**ne ne kadar yazıldığı — muhasebe köprüsü; iade durumu. |
| **`doctor_commissions`** | Tamamlanan kalem için **hekim hakedişi:** brüt, oran, komisyon tutarı, para birimi/kur, durum (bekliyor/dağıtıldı/iptal). |
| **`exchange_rates`** | Günlük **kur tablosu** (örn. TCMB): from/to para, kur, tarih, kaynak; benzersiz (from, to, date). |
| **`exchange_rate_overrides`** | Şirket/şube için **manuel kur** override; geçerlilik aralığı. |
| **`exchange_rate_difference`** | Fatura/ödeme anı ile tahsilat anı **kur farkı** TRY cinsinden (kâr/zarar); kaynak tip ve id ile ilişkilendirilir. |

---

### 3.12 Diş şeması

| Tablo | Ayrıntı |
|--------|---------|
| **`tooth_records`** | Hasta + diş için **güncel durum** (FDI numarası, enum durum, yüzeyler, not, kaydeden hekim/zaman). |
| **`tooth_condition_history`** | Durum **değişiklik geçmişi** (append-only): eski/yeni durum, değiştiren, zaman, sebep. Soft delete yok. |

---

### 3.13 Hasta dosyası: anamnez, ilaç, not, dosya

| Tablo | Ayrıntı |
|--------|---------|
| **`patient_anamnesis`** | Anamnez formu: sistemik hastalık, alerji, ilaç, gebelik/emzirme, ağız bakımı, sigara/alkol vb.; isteğe bağlı `protocol_id`; dolduran kullanıcı. Soft delete global filtresi yok — kendi yaşam döngüsü. |
| **`patient_medications`** | Kullanılan ilaç listesi: ad, doz, sıklık, neden, aktiflik, ekleyen kullanıcı. |
| **`patient_notes`** | Not: tip (genel/klinik/gizli/plan/tedavi…), içerik, sabitleme, gizli not, isteğe bağlı randevu FK, **`deleted_at`** ile soft delete. |
| **`patient_files`** | Yüklenen dosya **meta:** tür (röntgen, onam…), depolama yolu/URL, boyut, not, **`deleted_at`**. |

---

### 3.14 Bildirim ve SMS

| Tablo | Ayrıntı |
|--------|---------|
| **`notifications`** | Klinik içi **uygulama bildirimi** (SignalR ile anlık): şube, hedef kullanıcı veya rol, tip, başlık/mesaj, okundu, acil, ilişkili entity. Arşiv kalıcı — klasik soft delete yok. |
| **`sms_queue`** | **SMS kuyruğu:** alıcı, mesaj, sağlayıcı, durum (kuyruk/gönderildi/hata/İYS), yeniden deneme, kaynak tipi. |

---

### 3.15 Online randevu

| Tablo | Ayrıntı |
|--------|---------|
| **`doctor_online_booking_settings`** | Hekim+şube: online görünürlük, slot süresi, otomatik onay, en fazla kaç gün öncesi, hasta tipi filtresi, widget notu. |
| **`doctor_online_schedule`** | Online için **haftalık** müsait saatler (iç randevu takviminden ayrı). |
| **`doctor_online_blocks`** | Online slotları kesen **tarih aralığı** (izin, kongre). |
| **`branch_online_booking_settings`** | Şube: widget açık/kapalı, **slug** (portal URL), renk/logo, iptal süresi kuralı. |
| **`online_booking_requests`** | Gelen **talep:** hekim, şube, hasta veya yeni hasta bilgisi, istenen tarih/saat, slot süresi, kaynak (widget/portal…), durum (bekliyor/onaylı/reddedildi…), telefon doğrulama, onay sonrası `appointment_id`. |

---

### 3.16 Hasta portalı

| Tablo | Ayrıntı |
|--------|---------|
| **`patient_portal_accounts`** | Hasta **portal hesabı:** e-posta/telefon, parola hash, hasta FK (opsiyonel), doğrulama token’ları, dil, son giriş, aktiflik. `is_active` ile yönetilir; klasik `IsDeleted` filtresi yok. |
| **`patient_portal_sessions`** | Portal **refresh oturumu:** token hash, süre, iptal, IP/UA. |

---

### 3.17 Anket ve şikayet

| Tablo | Ayrıntı |
|--------|---------|
| **`survey_templates`** | Şirket anket şablonu: tetikleyici (randevu sonrası vb.), gecikme (saat), sorular. |
| **`survey_questions`** | Soru metni, tip (yıldız, evet/hayır, çoktan seçmeli, metin), seçenekler JSON, zorunluluk. |
| **`survey_responses`** | Hastaya gönderilen **anket örneği:** token, durum, kanal, NPS/ortalama puan, isteğe bağlı randevu bağlantısı. |
| **`survey_answers`** | Her soruya verilen yanıt (metin/puan/boolean/seçenek). |
| **`complaints`** | Şikayet kaydı: numara, şube/hasta, kaynak, öncelik, durum, SLA alanları (koddan devam). |
| **`complaint_notes`** | Şikayet üzerinde **not:** iç not vs hastaya gösterilebilir not. |

---

### 3.18 E-fatura

| Tablo | Ayrıntı |
|--------|---------|
| **`einvoice_integrations`** | Şirket başına **entegratör** ayarları: sağlayıcı kodu, VKN, vergi dairesi, şifreli `config` JSON, otomatik e-arşiv, test modu. |
| **`einvoices`** | Kesilen **e-belge:** tip (e-arşiv/e-fatura/SMM), alıcı bilgisi, tutarlar, KDV, döviz ve TRY karşılığı, GİB UUID/durum, PDF yolu, iptal. |
| **`einvoice_items`** | Fatura **kalem satırları:** açıklama, miktar, birim fiyat, indirim, KDV, satır toplamı. |

---

### 3.19 Çoklu dil

| Tablo | Ayrıntı |
|--------|---------|
| **`translation_keys`** | Çeviri **anahtarı** (noktalı hiyerarşi), kategori, açıklama. |
| **`translations`** | Anahtar + `language_id` için **metin** değeri; `is_reviewed` (taslak/onaylı çeviri). |

---

### 3.20 Denetim ve KVKK

| Tablo | Ayrıntı |
|--------|---------|
| **`audit_logs`** | **Değiştirilemez** denetim: şirket/şube/kullanıcı, e-posta snapshot, eylem (CREATE/UPDATE/…), entity tip/id, eski/yeni değer JSON, IP, UA, zaman. Güncelleme/silme yok. |
| **`kvkk_consent_logs`** | Hasta başına **onay/ret geçmişi:** consent tipi (veri işleme, pazarlama…), verildi mi, zaman, IP, iptal zamanı. Append-only mantık. |
| **`data_export_requests`** | KVKK **veri dışa aktarma** talebi: durum, hazır dosya yolu, sona erme (indirme linki süresi). |

---

### 3.21 Entegrasyon ve operasyon

| Tablo | Ayrıntı |
|--------|---------|
| **`outbox_messages`** | **Transactional outbox:** event tipi, JSON payload, işlem durumu, deneme sayısı, exponential backoff, hata mesajı. Ana işlemle aynı transaction’da commit. |
| **`backup_logs`** | Yedekleme işi: tip (full/incremental…), dosya adı/boyut/konum/checksum, durum, süre, hata; isteğe bağlı geri yükleme testi sonucu. |

---

## 4. İlişki özeti (okuma ipuçları)

- **Şirket** → çok **şube**; **kullanıcı** rolleri şirket ve/veya şube kapsamında atanır.
- **Hasta** şubeye bağlıdır; **randevu** şube + hekim + durum; **ziyaret** randevulu veya walk-in.
- **Protokol** ziyite ve hekime bağlıdır; **tedavi planı** hastaya ve isteğe bağlı protokole bağlanır.
- **Fiyat:** `treatments` → (isteğe bağlı) `treatment_mappings` → `reference_price_items`; üzerine `pricing_rules` + `campaigns` + şube `pricing_multiplier`.
- **Ödeme** → `payment_allocations` → `treatment_plan_items`; **hakediş** tamamlanan kalemlerden.

---

## 5. Şema değişikliği ve okuma sırası

1. Yeni alan/tablo için entity ve `AppDbContext` güncellemesi.
2. Migration: `dotnet ef migrations add ... --project src/Oravity.Infrastructure --startup-project src/Oravity.Core`
3. Bu belgeyi **manuel** güncelleyin veya migration adından hatırlatıcı ekleyin.

**Kolay doğrulama:** `src/Oravity.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` — güncel kolon ve FK listesi.

---

*Son içerik güncellemesi: proje entity’leri ve `AppDbContext` tablo eşlemelerine göre derlenmiştir.*
