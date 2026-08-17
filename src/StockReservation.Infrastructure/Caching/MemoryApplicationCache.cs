using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StockReservation.Application;

namespace StockReservation.Infrastructure.Caching;

public sealed class MemoryApplicationCache(
    IMemoryCache cache,
    ILogger<MemoryApplicationCache> logger) : IApplicationCache
{
    public bool TryGet<T>(string key, out T? value)
    {
        if (cache.TryGetValue(key, out T? cached))
        {
            value = cached;
            logger.LogDebug("Cache hit for {CacheKey}", key);
            return true;
        }

        value = default;
        logger.LogDebug("Cache miss for {CacheKey}", key);
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan lifetime)
    {
        cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = lifetime,
            Size = 1
        });

        logger.LogDebug("Cached {CacheKey} for {CacheLifetimeSeconds} seconds", key, lifetime.TotalSeconds);
    }

    public void Remove(string key)
    {
        cache.Remove(key);
        logger.LogDebug("Invalidated cache key {CacheKey}", key);
    }
}
