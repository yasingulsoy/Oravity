# Fiyatlandırma Sistemi — Kavramlar, Mimari ve Örnekler

> Bu belge Oravity fiyatlandırma motorunun iş mantığını, teknik mimarisini ve kullanım
> senaryolarını açıklar. Kafanız karıştığında buraya dönün.

---

## İçindekiler

1. [Tedavi Kataloğu](#1-tedavi-kataloğu)
2. [Referans Fiyat Listeleri](#2-referans-fiyat-listeleri)
3. [Tedavi Eşleştirme (Mapping)](#3-tedavi-eşleştirme-mapping)
4. [Formül Motoru (FormulaEngine)](#4-formül-motoru-formulaengine)
5. [Formül Değişkenleri](#5-formül-değişkenleri)
6. [Fiyatlandırma Kuralları (PricingRule)](#6-fiyatlandırma-kuralları-pricingrule)
7. [Kural Motoru (PricingEngine)](#7-kural-motoru-pricingengine)
8. [Fiyat Hesaplama Akışı (GetTreatmentPriceQuery)](#8-fiyat-hesaplama-akışı)
9. [Anlaşmalı Kurum (Institution)](#9-anlaşmalı-kurum-institution)
10. [ÖSS (Özel Sağlık Sigortası)](#10-öss-özel-sağlık-sigortası)
11. [Kampanya Kodu](#11-kampanya-kodu)
12. [MULTI — Şube Fiyat Katsayısı](#12-multi--şube-fiyat-katsayısı)
13. [Strateji Sonuçları](#13-strateji-sonuçları)
14. [NoPriceConfigured Durumu](#14-nopriceconfigured-durumu)
15. [API Endpoint'leri](#15-api-endpointleri)
16. [Hızlı Referans — Hangi Sayfa, Ne İş?](#16-hızlı-referans--hangi-sayfa-ne-iş)

---

## 1. Tedavi Kataloğu

Kliniğin sunduğu tedavilerin listesi. Klinik kendi kodlarını ve adlarını belirler.

```
Yönetim:  /catalog sayfası
DB:       treatments
```

| Kod  | Ad                       | Kategori   | KDV |
|------|--------------------------|------------|-----|
| D001 | Kompozit Dolgu (1 yüz)  | Restoratif | %20 |
| D002 | Kompozit Dolgu (2 yüz)  | Restoratif | %20 |
| I001 | Implant Bego (silindirik)| İmplant    | %20 |
| C001 | Diş Çekimi (basit)      | Cerrahi    | %20 |

Bu liste **TDB'den tamamen bağımsız**. Siz "Implant Bego" diyebilirsiniz,
TDB buna "Silindirik Kemik içi İmplant" diyor — önemli değil, eşleştirme ile bağlarsınız.

---

## 2. Referans Fiyat Listeleri

Dışarıdan gelen standart fiyat cetvelleri.

```
Yönetim:  /pricing → Referans Fiyatlar sekmesi
DB:       reference_price_lists + reference_price_items
```

| Liste Kodu | Ad                           | Kaynak | Yıl  |
|------------|------------------------------|--------|------|
| TDB_2026   | Türk Diş Hekimleri Birliği   | Resmi  | 2026 |
| SUT_2026   | SGK Sağlık Uygulama Tebliği  | Resmi  | 2026 |
| CARI_2026  | Kliniğin Cari Fiyatları      | Özel   | 2026 |

Her listenin içinde kodlu kalemler vardır:

**TDB_2026 örneği:**

| Kod    | İsim                          | Fiyat   |
|--------|-------------------------------|---------|
| 201010 | Kompozit Dolgu (1 yüz)       | 1.200 ₺ |
| 201020 | Kompozit Dolgu (2 yüz)       | 1.600 ₺ |
| 301010 | Silindirik Kemik içi İmplant  | 8.000 ₺ |
| 101010 | Diş Çekimi (basit)           | 800 ₺   |

**CARI_2026 örneği:**

| Kod  | İsim                        | Fiyat    |
|------|-----------------------------|----------|
| D001 | Kompozit Dolgu (1 yüz)     | 1.500 ₺  |
| D002 | Kompozit Dolgu (2 yüz)     | 2.000 ₺  |
| I001 | Implant Bego (silindirik)   | 12.000 ₺ |
| C001 | Diş Çekimi (basit)         | 1.000 ₺  |

**Önemli:** Liste kodu formül değişkeniyle prefix üzerinden eşleşir:
- `TDB_2026` → formülde `TDB` değişkeni
- `CARI_2026` → formülde `CARI` değişkeni
- `SUT_2025` → formülde `SUT` değişkeni
- `ISAK_2026` → formülde `ISAK` değişkeni

---

## 3. Tedavi Eşleştirme (Mapping)

**Problem:** Klinik kataloğundaki "Implant Bego (I001)" ile TDB'deki "Silindirik Kemik içi İmplant (301010)" aynı şey ama kodları farklı. Motor bunu bilemez.

**Çözüm:** Her tedavi için hangi referans listesindeki hangi koda karşılık geldiğini tanımlarsınız.

```
Yönetim:  /pricing → Tedavi Eşleştirmeleri sekmesi (veya /catalog → Düzenle)
DB:       treatment_mappings
```

**Örnek eşleştirme:**

| İç Kod | İç Ad                     | Liste     | Referans Kodu | Referans Adı                 |
|--------|---------------------------|-----------|---------------|------------------------------|
| I001   | Implant Bego (silindirik) | TDB_2026  | 301010        | Silindirik Kemik içi İmplant |
| I001   | Implant Bego (silindirik) | CARI_2026 | I001          | Implant Bego (silindirik)    |
| D001   | Kompozit Dolgu (1 yüz)   | TDB_2026  | 201010        | Kompozit Dolgu (1 yüz)      |
| D001   | Kompozit Dolgu (1 yüz)   | CARI_2026 | D001          | Kompozit Dolgu (1 yüz)      |

Her eşleştirmede bir **kalite** bilgisi de tutulur:
- `exact` — Birebir aynı tedavi
- `partial` — Kısmi eşleşme
- `approximate` — Yaklaşık eşleşme

> Bir tedavi birden fazla listeyle eşleştirilebilir. Motor hangi listeyi kullanacağını
> formüldeki değişkenden anlar (`TDB`, `CARI`, vb).

---

## 4. Formül Motoru (FormulaEngine)

`FormulaEngine` sınıfı, string olarak tanımlanan formülleri verilen değişkenlerle
matematiksel olarak değerlendirir.

**Desteklenen yapılar:**

| Yapı            | Söz Dizimi           | Örnek                                   |
|-----------------|----------------------|-----------------------------------------|
| Değişken        | BÜYÜK HARF           | `TDB`, `CARI`, `SUT`, `ISAK`, `MULTI`  |
| Sayısal literal | Nokta veya virgüllü  | `1000`, `0.90`, `1,10`                  |
| Aritmetik       | `+  -  *  /`         | `TDB * 2.5`                             |
| Karşılaştırma   | `== != > >= < <=`    | `ISAK == 1`                             |
| Ternary         | `koşul ? doğru : yanlış` | `ISAK==1 ? TDB*0.90 : CARI`        |
| Gruplama        | `(ifade)`            | `(TDB + CARI) / 2`                     |
| Fonksiyonlar    | `MIN(a, b)`, `MAX(a, b)` | `MIN(CARI, TDB)`                   |
| İç içe ternary  | Birleşik koşullar    | `ISAK==1 ? (TDB>CARI ? CARI : TDB) : CARI*0.80` |

**Önemli notlar:**
- TR virgülü (`,`) otomatik olarak noktaya (`.`) çevrilir
- Sıfıra bölme hatası `FormulaException` fırlatır
- Tanımsız değişken `UnknownVariableException` fırlatır
- Karşılaştırma sonucu `true=1`, `false=0` olarak decimal döner

---

## 5. Formül Değişkenleri

Formül motoruna geçirilen değişkenler, `BuildFormulaVariables()` metodu tarafından
`RuleEvalContext` içindeki `ReferencePrices` sözlüğünden **prefix eşleştirmesiyle** çözümlenir.

**Çözümleme mantığı:**

```
ReferencePrices sözlüğü → { "TDB_2026": 8000, "CARI_2026": 12000 }

Resolve("TDB", fallback)  → Key "TDB_2026" ile başlıyor → 8.000
Resolve("CARI", fallback) → Key "CARI_2026" ile başlıyor → 12.000
Resolve("SUT", fallback)  → Eşleşen yok → TDB değeri (fallback)
Resolve("ISAK", fallback) → Eşleşen yok → 0
```

| Değişken | Kaynak                                  | Fallback               |
|----------|-----------------------------------------|------------------------|
| `TDB`    | `TDB_*` kodlu referans listedeki fiyat  | İlk bulunan fiyat      |
| `CARI`   | `CARI_*` kodlu referans listedeki fiyat | TDB değeri             |
| `SUT`    | `SUT_*` kodlu referans listedeki fiyat  | TDB değeri             |
| `ISAK`   | `ISAK_*` kodlu referans listedeki fiyat | 0                      |
| `MULTI`  | Şube `PricingMultiplier` değeri         | 1.0 (şube ayarlarından)|

**Fonksiyonlar:**

| Fonksiyon    | Açıklama                | Örnek                     |
|--------------|-------------------------|---------------------------|
| `MIN(a, b)`  | İkisinden küçük olanı al| `MIN(CARI, TDB)` → 8.000 |
| `MAX(a, b)`  | İkisinden büyük olanı al| `MAX(CARI, TDB)` → 12.000|

---

## 6. Fiyatlandırma Kuralları (PricingRule)

Kurallar öncelik sırasına göre değerlendirilir. İlk eşleşen ve `StopProcessing=true` olan kural kazanır.

```
Yönetim:  /pricing → Fiyat Kuralları sekmesi
DB:       pricing_rules
```

### Kural Alanları

| Alan            | Tip       | Açıklama                                          |
|-----------------|-----------|---------------------------------------------------|
| Name            | string    | Kuralın ismi (ör: "THY Personel İndirimi")        |
| RuleType        | string    | `"formula"`, `"percentage"`, `"fixed"`            |
| Priority        | int       | Küçük = daha önce (1 en yüksek öncelik)           |
| IncludeFilters  | JSON      | Bu kural kime uygulanır? (AND mantığı)            |
| ExcludeFilters  | JSON      | Bu kural kimden hariç? (eşleşirse kural uygulanmaz)|
| Formula         | string    | Fiyat formülü veya sabit değer                    |
| OutputCurrency  | string    | Sonuç para birimi (varsayılan "TRY")              |
| StopProcessing  | bool      | `true` ise sonraki kurallar atlanır               |
| ValidFrom       | DateTime? | Kuralın geçerlilik başlangıcı                     |
| ValidUntil      | DateTime? | Kuralın geçerlilik bitişi                         |
| IsActive        | bool      | Aktif/pasif                                       |
| BranchId        | long?     | null = tüm şubeler, değer = sadece o şube         |
| CompanyId       | long      | Kuralın ait olduğu şirket                         |

### Kural Tipleri

| Tip          | Formula Alanı Anlamı                      | Hesaplama                     |
|--------------|-------------------------------------------|-------------------------------|
| `formula`    | FormulaEngine formülü                     | `FormulaEngine.Evaluate()`    |
| `percentage` | Yüzde indirim (ör: "20" = %20 indirim)   | `TDB × (1 - değer/100)`      |
| `fixed`      | Sabit fiyat (ör: "5000")                  | Direkt decimal parse          |

### Include Filtreler (JSON)

Kuralın kime uygulanacağını belirler. Tüm koşullar **AND** ile birleşir.
`treatmentIds` ve `categoryIds` kendi içlerinde **OR** mantığıyla çalışır.

```json
// Sadece THY çalışanlarına
{ "institutionIds": [42] }

// Sadece ÖSS hastalara
{ "ossOnly": true }

// Kampanya kodu
{ "campaignCodes": ["YAZ2026"] }

// Sadece belirli tedaviler
{ "treatmentIds": [101, 102] }

// Sadece belirli kategoriler
{ "categoryIds": [15] }

// Kombinasyon: THY'ye özel implant fiyatı (AND)
{ "institutionIds": [42], "categoryIds": [15] }
```

### Exclude Filtreler

Include ile aynı JSON yapısı. Eşleşirse kural **uygulanmaz** (kara liste mantığı).

### Filtre Eşleşme Mantığı (Kod Akışı)

```
1. ExcludeFilters varsa ve eşleşirse → false (kural atlanır)
2. IncludeFilters boşsa → true (herkese uygulanır)
3. IncludeFilters varsa:
   a. institutionIds kontrolü  → eşleşmezse false
   b. institutionAgreement kontrolü → true istenip hasta anlaşmasızsa false
   c. ossOnly kontrolü → true istenip hasta ÖSS değilse false
   d. campaignCodes kontrolü → kod eşleşmezse false
   e. treatmentIds/categoryIds → en az biri eşleşirse true (OR)
   f. Tedavi/kategori filtresi yoksa → true
```

---

## 7. Kural Motoru (PricingEngine)

`PricingEngine.CalculateWithRules()` metodu kuralları öncelik sırasına göre işler.

### İşlem Akışı

```
rules.OrderBy(Priority) ile sırala
    │
    ├─ Kural aktif mi?           → Pasifse atla
    ├─ ValidFrom/ValidUntil?     → Tarih dışındaysa atla
    ├─ Filtreler eşleşiyor mu?   → Eşleşmezse atla
    │
    ├─ Değişkenleri hazırla (BuildFormulaVariables)
    │
    ├─ RuleType'a göre hesapla:
    │   ├─ "formula"    → FormulaEngine.Evaluate(formula, vars)
    │   ├─ "percentage" → TDB × (1 - formula/100)
    │   └─ "fixed"      → decimal.Parse(formula)
    │
    ├─ Sonucu oluştur:
    │   ├─ FinalPrice    = hesaplanan fiyat (min 0, 2 ondalık)
    │   ├─ OriginalPrice = TDB referans fiyatı
    │   └─ TotalDiscount = TDB - FinalPrice
    │
    └─ StopProcessing = true ise → hemen döndür
       StopProcessing = false ise → sonraki kurala devam, son eşleşen kazanır
```

### Hata Yönetimi

- Formül hatası (parse/evaluate exception) → kural atlanır, sonrakine geçilir
- Hiçbir kural eşleşmezse → `null` döner (çağıran taraf fallback uygular)

---

## 8. Fiyat Hesaplama Akışı

`GetTreatmentPriceQuery` handler'ı tüm sistemi bir araya getirir.

### Adım Adım Akış

```
┌─────────────────────────────────────────────────────────┐
│  1. ŞİRKET BAĞLAMINI ÇÖZ                               │
│     JWT → tenant.CompanyId                              │
│     Yoksa → BranchId üzerinden Branches tablosundan     │
│     Yoksa → UserRoleAssignment üzerinden                │
│     Hâlâ yoksa → NoPriceConfigured döndür               │
└───────────────────────┬─────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────┐
│  2. TEDAVİYİ BUL                                        │
│     treatments tablosunda PublicId + CompanyId ile ara   │
│     Bulunamazsa → NotFoundException                     │
└───────────────────────┬─────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────┐
│  3. MAPPING'LERİ ÇEK                                   │
│     treatment_mappings → InternalTreatmentId ile filtre │
│     Her mapping için reference_price_items'dan fiyat al │
│     Sonuç: { "TDB_2026": 8000, "CARI_2026": 12000 }   │
└───────────────────────┬─────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────┐
│  4. ŞUBE ÇARPANINI ÇEK                                 │
│     Branches → PricingMultiplier (MULTI değişkeni)      │
│     Yoksa veya 0 ise → 1.0 varsayılan                  │
└───────────────────────┬─────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────┐
│  5. ŞİRKETİN KURALLARINI ÇEK                           │
│     pricing_rules → CompanyId + IsActive                │
│     BranchId null (genel) veya eşleşen şube             │
│     Priority sırasına göre sırala                       │
└───────────────────────┬─────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────┐
│  6. KURAL MOTORUNU ÇALIŞTIR                             │
│     Kurallar + referans fiyatlar varsa:                  │
│       RuleEvalContext oluştur (fiyatlar, kurum, ÖSS,    │
│       kampanya kodu, MULTI, tedavi/kategori bilgisi)    │
│       PricingEngine.CalculateWithRules() çağır          │
│       Sonuç varsa → Strategy="Rule" olarak döndür      │
│                                                         │
│     Kural yoksa veya eşleşme yoksa:                     │
│       TDB fiyatı > 0 ise → Strategy="ReferencePrice"   │
│       TDB fiyatı da yoksa → Strategy="NoPriceConfigured"│
└─────────────────────────────────────────────────────────┘
```

### Sonuç DTO'su (TreatmentPriceResponse)

| Alan            | Tip     | Açıklama                                       |
|-----------------|---------|-------------------------------------------------|
| UnitPrice       | decimal | Hesaplanan birim fiyat                          |
| ReferencePrice  | decimal | TDB referans fiyatı (karşılaştırma için)        |
| Currency        | string  | Para birimi ("TRY")                             |
| AppliedRuleName | string? | Uygulanan kuralın adı (varsa)                   |
| Strategy        | string  | `"Rule"`, `"ReferencePrice"`, `"NoPriceConfigured"` |

---

## 9. Anlaşmalı Kurum (Institution)

Bazı hastalar bir kurumla anlaşma kapsamında gelir (şirketten çalışan, sendika üyesi vb.).

```
Hasta kartında "Anlaşmalı Kurum" seçilir.
Muayenede bu bilgi fiyat hesabına otomatik aktarılır (institutionId parametresi).
```

**Örnek Senaryo — Türk Hava Yolları:**

| Tedavi         | Normal Fiyat | THY Fiyatı (Kural)                                         |
|----------------|--------------|-------------------------------------------------------------|
| Kompozit Dolgu | 1.500 ₺      | MIN(CARI×0.80, TDB×0.80) = MIN(1.200, 960) = **960 ₺**    |
| İmplant        | 12.000 ₺     | MIN(CARI×0.80, TDB×0.80) = MIN(9.600, 6.400) = **6.400 ₺**|

**Kural tanımı:**
```
Ad:       THY Personel İndirimi
Filtre:   { "institutionIds": [42] }
Formül:   MIN(CARI * 0.80, TDB * 0.80)
Öncelik:  10
```

---

## 10. ÖSS (Özel Sağlık Sigortası)

Özel sağlık sigortası olan hastalara özel fiyat uygulanabilir.

```
Hasta kartında veya randevuda "ÖSS" seçilir.
API'ye isOss=true olarak iletilir.
```

**Kural tanımı:**
```
Ad:      ÖSS Genel İndirim
Filtre:  { "ossOnly": true }
Formül:  CARI * 0.70
Öncelik: 20
```

**ÖSS + Kurum birlikte gelebilir mi?** Evet. Ayrı kurallar tanımlayıp öncelikle yönetirsiniz:

| Öncelik | Kural              | Filtre                                         | Formül              |
|---------|--------------------|-------------------------------------------------|---------------------|
| 5       | AXA + THY VIP      | `{ "institutionIds": [42], "ossOnly": true }`  | `CARI * 0.65`       |
| 10      | THY Personel       | `{ "institutionIds": [42] }`                   | `MIN(CARI*0.80, TDB*0.80)` |
| 20      | ÖSS Genel          | `{ "ossOnly": true }`                          | `CARI * 0.70`       |
| 30      | Varsayılan         | *(filtre yok — herkese)*                       | `CARI`              |

Motor sırayla bakar: AXA üyesi ve THY çalışanıysa → 5, sadece THY çalışanıysa → 10, sadece ÖSS'liyse → 20, normal hasta → 30.

---

## 11. Kampanya Kodu

Belirli bir promosyon veya kampanya kapsamında gelen hastalara özel fiyat.

```
Muayene sırasında kampanya kodu elle girilir (ör: "YAZ2026").
API'ye campaignCode parametresi olarak iletilir.
```

**Kural tanımı:**
```
Ad:      Yaz Kampanyası 2026
Filtre:  { "campaignCodes": ["YAZ2026", "SUMMER26"] }
Formül:  CARI * 0.75
Öncelik: 15
```

---

## 12. MULTI — Şube Fiyat Katsayısı

Bazı şubeler farklı maliyet yapısına sahip olabilir (kira, personel, bölge).

```
Yönetim:  /pricing → Şube Ayarları sekmesi
DB:       branches.pricing_multiplier
```

| Şube     | MULTI | Açıklama                         |
|----------|-------|----------------------------------|
| İstanbul | 1.00  | Baz fiyat                        |
| Bodrum   | 1.10  | Bodrum cari fiyatlar %10 yüksek  |
| Ankara   | 0.95  | Ankara'da %5 indirimli           |

**THY + Bodrum Şubesi Örneği:**

Bodrum'daki hasta THY çalışanı. Formül: `MIN(CARI * MULTI * 0.80, TDB * 0.80)`

```
CARI   = 12.000  (implant cari fiyatı)
MULTI  = 1.10    (Bodrum katsayısı)
TDB    = 8.000   (TDB referans fiyatı)

CARI * MULTI * 0.80 = 12.000 × 1.10 × 0.80 = 10.560 ₺
TDB  * 0.80         =  8.000 × 0.80         =  6.400 ₺

MIN(10.560, 6.400) = 6.400 ₺  ← uygulanan fiyat
```

---

## 13. Strateji Sonuçları

API yanıtındaki `strategy` alanı, fiyatın nereden geldiğini belirtir:

| Strategy            | Anlamı                                                          |
|---------------------|-----------------------------------------------------------------|
| `Rule`              | Bir fiyat kuralı eşleşti ve formül uygulandı                   |
| `ReferencePrice`    | Kural eşleşmedi, TDB referans fiyatı direkt kullanıldı         |
| `NoPriceConfigured` | Ne kural ne referans fiyat bulundu — kullanıcı elle fiyat girer |

### Fallback Zinciri

```
1. Kurallar varsa ve referans fiyatlar varsa
   → CalculateWithRules() çağır
   → Eşleşen kural varsa → "Rule"

2. TDB referans fiyatı > 0 ise
   → TDB fiyatını direkt kullan → "ReferencePrice"

3. Hiçbir şey yoksa
   → UnitPrice=0 → "NoPriceConfigured"
```

---

## 14. NoPriceConfigured Durumu

Motor `UnitPrice=0, Strategy="NoPriceConfigured"` döndürürse:

**Olası nedenler:**
1. Tedavinin hiç referans eşleştirmesi yok (mapping tanımlanmamış)
2. Şirket bağlamı çözülenemedi (JWT'de CompanyId yok, BranchId de bulunamadı)
3. Referans listedeki kalem silinmiş veya kodu değişmiş

**Çözüm:**
- `/pricing → Tedavi Eşleştirmeleri` sekmesinde tedaviyi TDB ve/veya CARI listeleriyle eşleştirin
- Şirket bağlamı sorunuysa kullanıcının oturum bilgilerini kontrol edin

---

## 15. API Endpoint'leri

### Fiyat Hesaplama

| Method | Endpoint                                          | Açıklama                              |
|--------|---------------------------------------------------|---------------------------------------|
| GET    | `/api/pricing/treatment/{publicId}/price`         | Tedavi fiyatını kural motoruyla hesapla|
| POST   | `/api/pricing/calculate`                          | Manuel bağlam ile fiyat hesapla       |

**GET /price parametreleri:**

| Parametre      | Tip    | Zorunlu | Açıklama                    |
|----------------|--------|---------|-----------------------------|
| branchId       | long   | Hayır   | Şube ID (MULTI için)        |
| institutionId  | long   | Hayır   | Anlaşmalı kurum ID          |
| isOss          | bool   | Hayır   | ÖSS kapsamı                 |
| campaignCode   | string | Hayır   | Kampanya kodu                |

### Kural Yönetimi

| Method | Endpoint                                 | Yetki           | Açıklama            |
|--------|------------------------------------------|-----------------|---------------------|
| GET    | `/api/pricing/rules`                     | pricing:view    | Kuralları listele   |
| POST   | `/api/pricing/rules`                     | pricing:create  | Yeni kural oluştur  |
| PUT    | `/api/pricing/rules/{publicId}`          | pricing:edit    | Kuralı güncelle     |

### Referans Listeler

| Method | Endpoint                                          | Yetki         | Açıklama               |
|--------|---------------------------------------------------|---------------|------------------------|
| GET    | `/api/pricing/reference-lists`                    | pricing:view  | Listeleri getir        |
| POST   | `/api/pricing/reference-lists`                    | pricing:edit  | Yeni liste oluştur     |
| GET    | `/api/pricing/reference-lists/{id}/items`         | pricing:view  | Liste kalemlerini getir|
| PUT    | `/api/pricing/reference-lists/{id}/items/{code}`  | pricing:edit  | Kalem ekle/güncelle    |

### Şube Ayarları

| Method | Endpoint                                          | Yetki         | Açıklama               |
|--------|---------------------------------------------------|---------------|------------------------|
| GET    | `/api/pricing/branches`                           | pricing:view  | Şube ayarlarını listele|
| PATCH  | `/api/pricing/branches/{id}/multiplier`           | pricing:edit  | Çarpanı güncelle       |

---

## 16. Hızlı Referans — Hangi Sayfa, Ne İş?

| Sayfa                            | Ne yapılır?                                       |
|----------------------------------|---------------------------------------------------|
| `/catalog`                       | Tedavi ekle/düzenle                               |
| `/pricing → Referans Fiyatlar`   | TDB, CARI fiyatlarını görüntüle ve düzenle        |
| `/pricing → Fiyat Kuralları`     | "THY'ye %20 indirim" gibi kurallar tanımla        |
| `/pricing → Tedavi Eşleştirmeleri`| Tedavileri referans listeleriyle eşleştir          |
| `/pricing → Şube Ayarları`       | MULTI katsayısını şubeye göre ayarla              |
| Hasta Kartı                      | Anlaşmalı kurum ve ÖSS seç                       |
| Muayene / Plan Builder           | Tedavi eklerken fiyat otomatik hesaplanır         |

---

## Özet Diyagram — Uçtan Uca Akış

```
 HASTA                     TEDAVİ                    FİYATLANDIRMA
 ─────                     ──────                    ─────────────
 ┌──────────────┐     ┌──────────────┐         ┌──────────────────┐
 │ institutionId│     │ treatment.Id │         │ reference_price  │
 │ isOss        │     │ categoryId   │    ┌───▶│ _lists           │
 │ campaignCode │     │ code         │    │    │ (TDB, CARI, SUT) │
 └──────┬───────┘     └──────┬───────┘    │    └────────┬─────────┘
        │                    │            │             │
        │              ┌─────▼─────┐      │    ┌────────▼─────────┐
        │              │ treatment │      │    │ reference_price  │
        │              │ _mappings ├──────┘    │ _items           │
        │              └───────────┘           │ (kod → fiyat)    │
        │                                     └────────┬─────────┘
        │                                              │
        │    ┌──────────────┐                          │
        │    │ branches     │                          │
        │    │ .pricing     │                          │
        │    │ _multiplier  │─── MULTI                 │
        │    └──────────────┘                          │
        │                                              │
 ┌──────▼──────────────────────────────────────────────▼──┐
 │                    RuleEvalContext                      │
 │  TreatmentId, CategoryId, ReferencePrices,             │
 │  InstitutionId, IsOss, CampaignCode, MULTI            │
 └──────────────────────┬─────────────────────────────────┘
                        │
                 ┌──────▼──────┐
                 │ PricingEngine│
                 │ .Calculate  │
                 │  WithRules()│
                 └──────┬──────┘
                        │
              ┌─────────▼──────────┐
              │ pricing_rules      │
              │ (Priority sırasıyla│
              │  filtre + formül)  │
              └─────────┬──────────┘
                        │
              ┌─────────▼──────────┐
              │ TreatmentPrice     │
              │ Response           │
              │ unitPrice, strategy│
              │ appliedRuleName    │
              └────────────────────┘
```
