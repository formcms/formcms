namespace FormCMS.Infrastructure.Cache;

public interface ICacheProvider
{
    ValueTask<T> GetOrCreate<T>(string key, 
        TimeSpan expiration, TimeSpan localExpiration, 
        Func<CancellationToken, ValueTask<T>> factory,  CancellationToken ct = default);
    ValueTask Remove(string key, CancellationToken ct = default);
}