# Oravity.Backend — Process B

**Tip:** ASP.NET Core 8 Web API  
**Port (dev):** 5003 (HTTPS) / 5002 (HTTP)

## Sorumluluk

Bu proje Oravity sisteminin ikincil API sürecidir. Yönetici paneli, raporlama, arka plan işleri (Hangfire) ve audit log endpoint'lerini barındırır. Klinik çalışanlarının değil, sistem yöneticilerinin ve raporlama araçlarının tükettiği servislerdir.

## Modüller

| Modül | Açıklama |
|-------|----------|
| `Admin` | Sistem ayarları, kullanıcı yönetimi, yetki matrisi |
| `Reporting` | Yönetimsel raporlar, dashboard metrikleri, Excel/PDF export |
| `Jobs` | Hangfire arka plan işleri tanımları (hakedış hesaplama, toplu SMS, stok uyarı vb.) |
| `Audit` | Sistem geneli audit log sorguları |

## Hangfire

Arka plan işleri `/hangfire` dashboard üzerinden izlenebilir.

```
https://localhost:5003/hangfire
```

### Tanımlı Kuyruklar

| Kuyruk | Açıklama |
|--------|----------|
| `critical` | Anlık bildirimler, kritik uyarılar |
| `default` | Standart işler (hakedış, rapor) |
| `low` | Toplu SMS, e-posta gönderim |

## Çalıştırma

```bash
dotnet run --project src/Oravity.Backend --launch-profile Development
```

Swagger: `https://localhost:5003/swagger`  
Hangfire: `https://localhost:5003/hangfire`
