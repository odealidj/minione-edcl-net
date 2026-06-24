using EDCL.Shared.Kernel.Ports;
using StackExchange.Redis;
using System.Text.Json;

namespace EDCL.Shared.Infrastructure.Cache;

/// <summary>
/// Redis Sentinel-backed implementation of ICachePort.
/// Uses System.Text.Json for serialization.
/// All Redis errors are swallowed (cache is non-critical infrastructure).
/// </summary>
public sealed class RedisCacheAdapter(IConnectionMultiplexer redis) : ICachePort
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private IDatabase Db => redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var value = await Db.StringGetAsync(key);
            if (!value.HasValue) return default;

            var json = (string?)value;
            if (json is null) return default;

            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default; // Non-critical: proceed without cache
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value, JsonOptions);
            await Db.StringSetAsync(key, serialized, ttl);
        }
        catch
        {
            // Non-critical: cache miss will occur on next read
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try { await Db.KeyDeleteAsync(key); }
        catch { /* Non-critical */ }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken ct = default)
    {
        try
        {
            var server = redis.GetServer(redis.GetEndPoints().First());
            var keys   = server.Keys(pattern: pattern).ToArray();
            if (keys.Length > 0)
                await Db.KeyDeleteAsync(keys);
        }
        catch { /* Non-critical */ }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try { return await Db.KeyExistsAsync(key); }
        catch { return false; }
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        var result = await factory(ct);
        await SetAsync(key, result, ttl, ct);
        return result;
    }
}
