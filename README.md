# Oravity

Dental klinik zinciri yönetim sistemi — .NET 8 Modüler Monolit, Clean Architecture.

## Çözüm Yapısı

```
Oravity.sln
│
├── src/
│   ├── Oravity.Core/           # Process A — Hasta, Kullanıcı, Şube, Randevu, Tedavi...
│   ├── Oravity.Backend/        # Process B — Admin panel, Raporlama, Arka plan işleri
│   ├── Oravity.SharedKernel/   # Paylaşılan domain event'leri, interface'ler, base entity'ler
│   └── Oravity.Infrastructure/ # EF Core, Redis, Hangfire, Serilog, Polly
│
├── tests/
│   ├── Oravity.UnitTests/
│   └── Oravity.IntegrationTests/
│
└── docker/
    └── docker-compose.dev.yml
```

## Geliştirme Ortamını Başlatma

### 1. Bağımlılıkları Başlat (Docker)

```bash
docker compose -f docker/docker-compose.dev.yml up -d
```

Bu komut şunları başlatır:
- PostgreSQL 16 → `localhost:5432`
- Redis 7 → `localhost:6379`
- pgAdmin 4 → `http://localhost:5050`

### 2. Migration Uygula

```bash
cd src/Oravity.Core
dotnet ef database update
```

### 3. Projeleri Çalıştır

```bash
# Process A
dotnet run --project src/Oravity.Core

# Process B
dotnet run --project src/Oravity.Backend
```

### Swagger UI

- Core: `https://localhost:5001/swagger`
- Backend: `https://localhost:5003/swagger`
- Hangfire Dashboard: `https://localhost:5003/hangfire`

## Teknoloji Stack

| Katman | Teknoloji |
|--------|-----------|
| Framework | ASP.NET Core 8 |
| ORM | Entity Framework Core 8 + Npgsql |
| Veritabanı | PostgreSQL 16 |
| Cache | Redis 7 + StackExchange.Redis |
| Mesajlaşma | MediatR 12 |
| Validasyon | FluentValidation 11 |
| Logging | Serilog → Console / File / Seq |
| Resiliency | Polly 8 |
| Background Jobs | Hangfire 1.8 + PostgreSQL storage |
| API Docs | Swagger / Swashbuckle |

## Mimari Kararlar

- **Modüler Monolit**: Tek deployment, modüller arası sıkı bağımlılık yok
- **Clean Architecture**: Domain → Application → Infrastructure → API katmanları
- **Event-Driven**: Modüller MediatR domain event'leri üzerinden haberleşir
- **Multi-Tenant**: Tüm entity'ler `TenantId` taşır
- **Soft Delete**: Entity'ler fiziksel olarak silinmez, `IsDeleted` flag'i set edilir
