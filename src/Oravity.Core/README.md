# Oravity.Core — Process A

**Tip:** ASP.NET Core 8 Web API  
**Port (dev):** 5001 (HTTPS) / 5000 (HTTP)

## Sorumluluk

Bu proje Oravity sisteminin birincil API sürecidir. Klinik çalışanlarının (hekimler, resepsiyonistler) kullandığı tüm operasyonel endpoint'leri barındırır.

## Modüller

| Modül | Açıklama |
|-------|----------|
| `Core` | Hasta, Kullanıcı, Şube, Klinik yönetimi |
| `Appointment` | Randevu oluşturma, güncelleme, iptal |
| `Treatment` | Tedavi planı ve prosedür yönetimi |
| `Finance` | Ödeme, fatura, hakedış |
| `Inventory` | Stok ve envanter takibi |
| `Laboratory` | Lab işi gönderme ve takip |
| `HR` | İnsan kaynakları (izin, vardiya) |
| `CRM` | Potansiyel hasta yönetimi |
| `Notification` | SMS, e-posta, push bildirim |
| `Survey` | Hasta anket ve şikayet yönetimi |
| `Reporting` | Operasyonel raporlar |
| `QDMS` | Kalite döküman yönetimi |
| `LMS` | Eğitim modülü |

## Modül İç Yapısı (Clean Architecture)

```
Modules/{ModuleName}/
├── Domain/
│   ├── Entities/         # Aggregate root'lar, entity'ler
│   ├── Events/           # Domain event tanımları
│   └── ValueObjects/     # Value object'ler
├── Application/
│   ├── Commands/         # MediatR IRequest<T> command'ları + handler'ları
│   ├── Queries/          # MediatR IRequest<T> query'leri + handler'ları
│   └── Validators/       # FluentValidation kural setleri
└── Infrastructure/       # Repository implementasyonları, dış servis adapter'ları
```

## Çalıştırma

```bash
dotnet run --project src/Oravity.Core --launch-profile Development
```

Swagger: `https://localhost:5001/swagger`
