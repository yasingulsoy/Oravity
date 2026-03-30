# Oravity.SharedKernel

**Tip:** .NET 8 Class Library

## Sorumluluk

Tüm projeler tarafından paylaşılan temel yapı taşlarını içerir. Modüllerin birbirinin domain katmanına doğrudan bağımlı olmadan haberleşmesini sağlar.

## İçerik

| Klasör | İçerik |
|--------|--------|
| `BaseEntities/` | `BaseEntity`, `AuditableEntity` — tüm entity'lerin kalıtım aldığı sınıflar |
| `Events/` | `IDomainEvent`, `DomainEventBase` — modüller arası event kontratı |
| `Interfaces/` | `ICurrentUser`, `ITenantContext`, `IRepository<T>`, `ICacheService`, `IEventBus` |
| `Extensions/` | `StringExtensions`, `DateTimeExtensions` |

## Bağımlılık Kuralı

- `SharedKernel` → hiçbir diğer Oravity projesine bağımlı değildir
- `Core`, `Backend`, `Infrastructure` → `SharedKernel`'a bağımlıdır
- Modüller birbirinin domain katmanına **doğrudan bağımlı olamaz**; sadece `SharedKernel`'daki event'ler üzerinden haberleşebilir
