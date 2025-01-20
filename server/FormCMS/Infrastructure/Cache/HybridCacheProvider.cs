using Microsoft.Extensions.Caching.Hybrid;

namespace FormCMS.Infrastructure.Cache;

public sealed class HybridCacheProvider(HybridCache hybridCache):ICacheProvider
{
    public ValueTask<T> GetOrCreate<T>(
        string key, 
        TimeSpan expiration, 
        TimeSpan localExpiration, 
        Func<CancellationToken, ValueTask<T>> factory,  CancellationToken ct = default)
    {
        var options = new HybridCacheEntryOptions
        {
            LocalCacheExpiration = localExpiration,
            Expiration = expiration,
        };
        return hybridCache.GetOrCreateAsync(key,factory,options,null,ct);
    }

    public ValueTask Remove(string key, CancellationToken ct = default) => hybridCache.RemoveAsync(key, ct); 
}