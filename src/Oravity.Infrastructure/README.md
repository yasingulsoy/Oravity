# Oravity.Infrastructure

**Tip:** .NET 8 Class Library

## Sorumluluk

Tüm cross-cutting (kesişen) altyapı kaygılarını tek bir kütüphanede toplar. Hem `Oravity.Core` hem `Oravity.Backend` bu kütüphaneyi kullanır.

## İçerik

| Klasör | Teknoloji | Açıklama |
|--------|-----------|----------|
| `Database/` | EF Core 8 + Npgsql | `AppDbContext`, Migration'lar, Soft Delete filtresi, Audit interceptor |
| `Cache/` | StackExchange.Redis | `RedisCacheService` — `ICacheService` implementasyonu |
| `Messaging/` | MediatR 12 | `MediatREventBus` — `IEventBus` implementasyonu |
| `Storage/` | - | `IFileStorageService` — STL/fotoğraf dosya depolama arayüzü |
| `Notifications/` | - | `INotificationSender` — SMS, e-posta, push bildirim arayüzü |
| `Security/` | JWT | `JwtSettings` konfigürasyonu |

## Kayıt

```csharp
// Program.cs içinde
builder.Services.AddInfrastructure(builder.Configuration);
```

Bu tek çağrı şunları kayıt eder: EF Core, Redis, MediatR Event Bus, Hangfire.

## Bağımlılıklar

- `Oravity.SharedKernel` — interface kontratları için
- NuGet: EF Core 8, Npgsql, StackExchange.Redis, MediatR 12, Serilog, Polly, Hangfire
