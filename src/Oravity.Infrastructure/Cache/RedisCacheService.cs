using System.Text.Json;
using Oravity.SharedKernel.Interfaces;
using StackExchange.Redis;

namespace Oravity.Infrastructure.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception)
        {
            return default; // Redis erişilemez → cache miss olarak davran
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(30));
        }
        catch (Exception)
        {
            // Redis erişilemez → cache yazımını atla, uygulama çalışmaya devam etsin
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try { await _db.KeyDeleteAsync(key); }
        catch (Exception) { /* Redis unavailable */ }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken ct = default)
    {
        try
        {
            var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern).ToArray();
            if (keys.Length > 0)
                await _db.KeyDeleteAsync(keys);
        }
        catch (Exception) { /* Redis unavailable */ }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try { return await _db.KeyExistsAsync(key); }
        catch (Exception) { return false; }
    }
}
