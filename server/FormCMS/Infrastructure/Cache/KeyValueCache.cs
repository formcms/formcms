using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;

namespace FormCMS.Infrastructure.Cache;
public sealed class KeyValueCache<T>
{
    private string CacheKey(string key) => _prefix + key;

    private readonly ICacheProvider _cacheProvider;

    private readonly string _prefix;
    private readonly TimeSpan _expiration;
    private readonly TimeSpan _localCacheExpiration;

    public KeyValueCache(IServiceProvider provider, string prefix, TimeSpan expiration)
    {
        _prefix = prefix;
        _expiration = expiration;
        _localCacheExpiration = expiration / 3;

        var logger = provider.GetRequiredService<ILogger<KeyValueCache<T>>>();
        if (provider.GetService<HybridCache>() is { } hybridCache)
        {
            logger.LogInformation(
                $"""
                 *************************************************************************************************************
                 Prefix: {prefix}, Type: Hybrid cache, Expiration: {_expiration}, LocalCacheExpiration: {_localCacheExpiration}
                 *************************************************************************************************************
                 """);
            _cacheProvider = new HybridCacheProvider(hybridCache);
            return;
        }

        if (provider.GetService<IMemoryCache>() is { } memoryCache)
        {
            logger.LogInformation(
                $"""
                 ************************************************************************************************************
                 Prefix = {prefix}, Type : Memory cache, Expiration: {_expiration}
                 ************************************************************************************************************
                 """);
             
            _cacheProvider = new MemoryCacheProvider(memoryCache);
            return;
        }

        throw new Exception("failed to get cache provider");
    }

    public ValueTask Remove(string key,CancellationToken ct = default) => _cacheProvider.Remove(CacheKey(key), ct);

    public  ValueTask<T> GetOrSet(string key, Func<CancellationToken,ValueTask<T>> factory, CancellationToken ct = default)
        => _cacheProvider.GetOrCreate(CacheKey(key),_expiration,_localCacheExpiration,factory, ct);
}
