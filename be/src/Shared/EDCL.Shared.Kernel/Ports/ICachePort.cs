namespace EDCL.Shared.Kernel.Ports;

/// <summary>
/// Abstraction over Redis cache. Implemented in EDCL.Shared.Infrastructure.
/// All modules depend on this interface, never on StackExchange.Redis directly.
/// </summary>
public interface ICachePort
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Get from cache, or execute factory and cache the result.
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken ct = default);
}

/// <summary>
/// Typed cache key constants — prevents magic strings across the codebase.
/// </summary>
public static class CacheKeys
{
    public static string DriverProfile(long driverId)           => $"driver:profile:{driverId}";
    public static string Dashboard(long driverId)               => $"dashboard:{driverId}";
    public static string JobStops(long pickupOrderId)           => $"job:{pickupOrderId}:stops";
    public static string StopManifests(long stopId)             => $"stop:{stopId}:manifests";
    public static string ManifestParts(string manifestNo)       => $"manifest:{manifestNo}:parts";
    public static string Supplier(long supplierId)              => $"supplier:{supplierId}";
    public static string SupplierByCode(string code)            => $"supplier:code:{code}";
    public static string Idempotency(long driverId, string key) => $"idempotency:{driverId}:{key}";
}

/// <summary>
/// Standard cache TTL values used consistently across all modules.
/// </summary>
public static class CacheTtl
{
    public static readonly TimeSpan DriverProfile  = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Dashboard      = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan JobStops       = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan StopManifests  = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan ManifestParts  = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan MasterData     = TimeSpan.FromHours(1);
    public static readonly TimeSpan Idempotency    = TimeSpan.FromHours(24);
}
