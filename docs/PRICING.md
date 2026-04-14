# Fiyatlandırma Sistemi — Kavramlar ve Örnekler

> Bu belge kafanız karıştığında okuyun. Tablolar ve somut örneklerle her kavramı açıklar.

---

## 1. Tedavi Kataloğu (Sizin Listeniz)

Kliniğinizin sunduğu tedavilerin listesi. Siz tanımlarsınız, siz adlandırırsınız.

```
/catalog sayfasından yönetilir.
DB tablosu: treatments
```

| Kod   | Ad                        | Kategori        | KDV |
|-------|---------------------------|-----------------|-----|
| D001  | Kompozit Dolgu (1 yüz)    | Restoratif      | %20 |
| D002  | Kompozit Dolgu (2 yüz)    | Restoratif      | %20 |
| I001  | Implant Bego (silindirik) | İmplant         | %20 |
| C001  | Diş Çekimi (basit)        | Cerrahi         | %20 |

Bu liste **TDB'den tamamen bağımsız**. Siz "Implant Bego" diyebilirsiniz,
TDB buna "Silindirik Kemik içi İmplant" diyor — önemli değil, eşleştirme ile bağlarsınız.

---

## 2. Referans Fiyat Listeleri

Dışarıdan gelen standart fiyat cetvelleri. Bunları siz oluşturursunuz ama içeriği
genellikle resmi kaynaklardan gelir.

```
/pricing → Referans Fiyatlar sekmesinden yönetilir.
DB tablosu: reference_price_lists + reference_price_items
```

| Liste Kodu  | Ad                        | Kaynak   | Yıl  |
|-------------|---------------------------|----------|------|
| TDB_2026    | Türk Diş Hekimleri Birliği| Resmi    | 2026 |
| SUT_2026    | SGK Sağlık Uygulama Tebliği | Resmi  | 2026 |
| CARI_2026   | Kliniğin Cari Fiyatları   | Özel     | 2026 |

Her listenin içinde kodlu kalemler var:

**TDB_2026 örneği:**

| Kod    | İsim                              | Fiyat    |
|--------|-----------------------------------|----------|
| 201010 | Kompozit Dolgu (1 yüz)            | 1.200 ₺  |
| 201020 | Kompozit Dolgu (2 yüz)            | 1.600 ₺  |
| 301010 | Silindirik Kemik içi İmplant      | 8.000 ₺  |
| 101010 | Diş Çekimi (basit)                | 800 ₺    |

**CARI_2026 örneği (sizin belirlediğiniz):**

| Kod   | İsim                              | Fiyat    |
|-------|-----------------------------------|----------|
| D001  | Kompozit Dolgu (1 yüz)            | 1.500 ₺  |
| D002  | Kompozit Dolgu (2 yüz)            | 2.000 ₺  |
| I001  | Implant Bego (silindirik)         | 12.000 ₺ |
| C001  | Diş Çekimi (basit)                | 1.000 ₺  |

---

## 3. Tedavi Eşleştirme

**Problem:** Sizin listenizdeki "Implant Bego (I001)" ile TDB'deki "Silindirik Kemik içi İmplant (301010)"
aynı şey ama kodları farklı. Fiyatlandırma motoru bunu bilemez.

**Çözüm:** Her tedavi için hangi liste koduna karşılık geldiğini söylüyorsunuz.

```
/catalog → Düzenle → Referans Eşleştirmeleri bölümünden yapılır.
DB tablosu: treatment_mappings
```

**Örnek eşleştirme tablosu:**

| İç Kod | İç Ad                     | Liste     | Referans Kodu | Referans Adı                     |
|--------|---------------------------|-----------|---------------|----------------------------------|
| I001   | Implant Bego (silindirik) | TDB_2026  | 301010        | Silindirik Kemik içi İmplant     |
| I001   | Implant Bego (silindirik) | CARI_2026 | I001          | Implant Bego (silindirik)        |
| D001   | Kompozit Dolgu (1 yüz)    | TDB_2026  | 201010        | Kompozit Dolgu (1 yüz)           |
| D001   | Kompozit Dolgu (1 yüz)    | CARI_2026 | D001          | Kompozit Dolgu (1 yüz)           |

> Bir tedavi birden fazla listeyle eşleştirilebilir. Motor hangi listeyi kullanacağını
> formüldeki değişkenden anlar (`TDB`, `CARI`, vb).

---

## 4. Fiyatlandırma Motoru — Nasıl Çalışır?

Bir tedaviye fiyat biçilirken motor şu adımları izler:

```
1. Tedaviyi bul (treatment_mappings üzerinden)
2. Bu tedavinin eşleştiği referans fiyatlarını topla
   → TDB_2026: 8.000 ₺, CARI_2026: 12.000 ₺
3. Formül değişkenlerini doldur
   → TDB = 8.000, CARI = 12.000, MULTI = 1.0 (şube katsayısı)
4. Fiyatlandırma kurallarını sırayla uygula (öncelik = küçük numara)
5. Eşleşen ilk kuralın formülünü hesapla
6. Kural yoksa → TDB fiyatını direkt kullan
7. Hiç referans fiyat da yoksa → NoPriceConfigured (kullanıcı elle girer)
```

---

## 5. Formül Değişkenleri

```
/pricing → Fiyat Kuralları → kural oluştururken formül alanında kullanılır.
```

| Değişken | Açıklama                                              | Örnek Değer |
|----------|-------------------------------------------------------|-------------|
| `TDB`    | TDB_* kodlu referans listedeki fiyat                  | 8.000       |
| `CARI`   | CARI_* kodlu referans listedeki fiyat (sizin fiyatınız)| 12.000      |
| `SUT`    | SUT_* kodlu referans listedeki fiyat                  | 500         |
| `ISAK`   | ISAK_* kodlu referans listedeki fiyat                 | 600         |
| `MULTI`  | Şube fiyat katsayısı (Şube Ayarları'ndan)             | 1.10        |

**Fonksiyonlar:**

| Fonksiyon       | Açıklama                       | Örnek                        |
|-----------------|--------------------------------|------------------------------|
| `MIN(a, b)`     | İkisinden küçük olanı al       | `MIN(CARI, TDB)` → 8.000     |
| `MAX(a, b)`     | İkisinden büyük olanı al       | `MAX(CARI, TDB)` → 12.000    |

---

## 6. Fiyatlandırma Kuralları

Kurallar öncelik sırasına göre uygulanır. İlk eşleşen kural kazanır.

```
/pricing → Fiyat Kuralları sekmesinden yönetilir.
DB tablosu: pricing_rules
```

### Kural Anatomisi

| Alan            | Açıklama                                                   |
|-----------------|------------------------------------------------------------|
| Ad              | Kuralın ismi (ör: "THY Personel İndirimi")                 |
| Öncelik         | Küçük = daha önce değerlendirilir (1 en yüksek öncelik)    |
| Filtreler       | Bu kural kime/neye uygulanır? (aşağıda detay)              |
| Formül          | Fiyat nasıl hesaplansın?                                   |
| Para Birimi     | Sonuç hangi para biriminde? (TRY)                          |
| StopProcessing  | Bu kural eşleşirse sonraki kurallara bakma                 |

### Include Filtreler (JSON)

Kuralın kime uygulanacağını belirler. Tüm koşullar AND ile birleşir.

```json
// Sadece THY çalışanlarına (institutionId: 42)
{ "institutionIds": [42] }

// Sadece ÖSS hastalara
{ "ossOnly": true }

// Kampanya kodu "YAZ2026" girilmiş hastalara
{ "campaignCodes": ["YAZ2026"] }

// Sadece belirli tedaviler (implant kategorisi)
{ "categoryIds": [15] }

// Kombinasyon: THY'ye özel implant fiyatı
{ "institutionIds": [42], "categoryIds": [15] }
```

---

## 7. Anlaşmalı Kurum (Institution)

Bazı hastalar bir kurumla anlaşma kapsamında gelir (şirketten çalışan, sendika üyesi vb.).
Bu durumda o kuruma özel fiyat uygulanabilir.

```
Hasta kartında "Anlaşmalı Kurum" seçilir.
Muayenede bu bilgi fiyat hesabına otomatik aktarılır.
```

**Örnek Senaryo — Türk Hava Yolları:**

| Tedavi              | Normal Fiyat | THY Fiyatı (Kural) |
|---------------------|--------------|--------------------|
| Kompozit Dolgu      | 1.500 ₺      | MIN(CARI*0.80, TDB*0.80) = MIN(1.200, 960) = **960 ₺** |
| İmplant             | 12.000 ₺     | MIN(CARI*0.80, TDB*0.80) = MIN(9.600, 6.400) = **6.400 ₺** |

**Kural tanımı:**
```
Ad:       THY Personel İndirimi
Filtre:   { "institutionIds": [42] }
Formül:   MIN(CARI * 0.80, TDB * 0.80)
Öncelik:  10
```

> "CARI'dan %20 indir veya TDB'den %20 indir, hangisi ucuzsa onu ver."

---

## 8. MULTI — Şube Fiyat Katsayısı

Bazı şubeler farklı maliyet yapısına sahip olabilir (kira, personel, bölge).
Bu farkı MULTI değişkeniyle yönetirsiniz.

```
/pricing → Şube Ayarları sekmesinden tanımlanır.
DB alanı: branches.pricing_multiplier
```

| Şube     | MULTI | Açıklama                               |
|----------|-------|----------------------------------------|
| İstanbul | 1.00  | Baz fiyat                              |
| Bodrum   | 1.10  | Bodrum cari fiyatlar %10 yüksek        |
| Ankara   | 0.95  | Ankara'da %5 indirimli                 |

**THY + Bodrum Şubesi Örneği:**

Bodrum'daki hasta THY çalışanı. Formül: `MIN(CARI * MULTI * 0.80, TDB * 0.80)`

```
CARI   = 12.000  (implant cari fiyatı)
MULTI  = 1.10    (Bodrum katsayısı)
TDB    = 8.000   (TDB referans fiyatı)

CARI * MULTI * 0.80 = 12.000 * 1.10 * 0.80 = 10.560 ₺
TDB  * 0.80         =  8.000 * 0.80         =  6.400 ₺

MIN(10.560, 6.400) = 6.400 ₺  ← uygulanan fiyat
```

---

## 9. ÖSS (Özel Sağlık Sigortası)

Özel sağlık sigortası olan hastalara özel fiyat uygulanabilir.

```
Hasta kartında veya randevuda "ÖSS" seçilir.
```

**Kural tanımı:**
```
Ad:      ÖSS Genel İndirim
Filtre:  { "ossOnly": true }
Formül:  CARI * 0.70
Öncelik: 20
```

**ÖSS + Kurum birlikte gelebilir mi?**

Evet. Filtrelerde birlikte yazamazsınız (AND olur), ama ayrı ayrı kural tanımlayıp öncelikle yönetirsiniz:

| Öncelik | Kural                  | Filtre                              | Formül           |
|---------|------------------------|-------------------------------------|------------------|
| 5       | AXA + THY VIP          | `{ "institutionIds": [42], "ossOnly": true }` | `CARI * 0.65` |
| 10      | THY Personel           | `{ "institutionIds": [42] }`        | `MIN(CARI*0.80, TDB*0.80)` |
| 20      | ÖSS Genel              | `{ "ossOnly": true }`               | `CARI * 0.70`    |
| 30      | Varsayılan             | *(filtre yok — herkese)*            | `CARI`           |

Motor sırayla bakar: AXA üyesi ve THY çalışanıysa → 5 numaralı kural, sadece THY çalışanıysa → 10 numaralı, sadece ÖSS'liyse → 20 numaralı, normal hasta → 30 numaralı.

---

## 10. Kampanya Kodu

Belirli bir promosyon veya kampanya kapsamında gelen hastalara özel fiyat.

```
Muayene sırasında kampanya kodu elle girilir (ör: "YAZ2026").
```

**Kural tanımı:**
```
Ad:      Yaz Kampanyası 2026
Filtre:  { "campaignCodes": ["YAZ2026", "SUMMER26"] }
Formül:  CARI * 0.75
Öncelik: 15
```

---

## 11. Fiyat Hesaplama Akışı — Özet Diyagramı

```
Hasta Seç
    │
    ├─ Anlaşmalı Kurum var mı?  → institutionId
    ├─ ÖSS var mı?              → isOss = true
    └─ Kampanya kodu girildi mi? → campaignCode

Tedavi Seç (ör: I001 - Implant Bego)
    │
    └─ Mapping: I001 → TDB_2026/301010, CARI_2026/I001
          │
          └─ Referans fiyatlar: TDB=8.000, CARI=12.000

Şube belirlendi (ör: Bodrum)
    └─ MULTI = 1.10

Kural motoru çalışır:
    Öncelik 5:  institutionId=42 AND ossOnly? → HAYIR
    Öncelik 10: institutionId=42?             → EVET ✓
    Formül: MIN(CARI * MULTI * 0.80, TDB * 0.80)
          = MIN(12.000 * 1.10 * 0.80, 8.000 * 0.80)
          = MIN(10.560, 6.400)
          = 6.400 ₺

Sonuç: unitPrice=6.400, referencePrice=8.000, strategy="Rule", rule="THY Personel İndirimi"
```

---

## 12. NoPriceConfigured Durumu

Motor `0, NoPriceConfigured` döndürürse şu anlama gelir:

1. Tedavinin hiç referans eşleştirmesi yok (CARI/TDB'de karşılığı tanımlanmamış)
2. **veya** şirket bağlamı çözümlenemedi (login sorununu kontrol et)

**Çözüm:** `/catalog → Düzenle → Referans Eşleştirmeleri` bölümünde tedaviyi
TDB ve/veya CARI listeleriyle eşleştirin.

---

## 13. Hızlı Referans — Hangi Sayfa, Ne İş?

| Sayfa                        | Ne yapılır?                                              |
|------------------------------|----------------------------------------------------------|
| `/catalog`                   | Tedavi ekle/düzenle, TDB/CARI eşleştirmesi yap           |
| `/pricing → Referans Fiyatlar` | TDB, CARI fiyatlarını görüntüle ve düzenle             |
| `/pricing → Fiyat Kuralları` | "THY'ye %20 indirim" gibi kurallar tanımla               |
| `/pricing → Şube Ayarları`   | MULTI katsayısını şubeye göre ayarla                     |
| Hasta Kartı                  | Anlaşmalı kurum ve ÖSS seç                               |
| Muayene / Plan Builder       | Tedavi eklerken fiyat otomatik hesaplanır                |
