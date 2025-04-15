using System.Collections.Concurrent;
using FormCMS.Utils.DateTimeExt;
using Microsoft.Extensions.Caching.Memory;

namespace FormCMS.Infrastructure.Buffers;

public class MemoryTrackingBuffer<T>(BufferSettings settings) where T : struct
{
    private readonly MemoryCache _cache = new (new MemoryCacheOptions());
    private readonly MemoryCache _locks = new (new MemoryCacheOptions());
    private readonly MemoryCache _flush = new (new MemoryCacheOptions());
    
    internal Dictionary<string,T> GetAfterLastFlush(DateTime lastFlushTime)
    {
        var now = DateTime.UtcNow;
       
        var ret = new Dictionary<string, T>();
        for (var t = lastFlushTime ; t < now; t = t.AddMinutes(1))
        {
            var key = GetNextFlushTimeKey(t);
            if (!_flush.TryGetValue(key, out var value) ||
                value is not ConcurrentDictionary<string, bool> dict) continue;
            
            foreach (var k in dict.Keys)
            {
                if (_cache.TryGetValue(k, out var cachedValue) && cachedValue is T v)
                {
                    ret[k] = v;
                }
            }
            _flush.Remove(key);
        }

        return ret;
    }
    
    internal async Task<Dictionary<string,T>> SafeGet(string[] keys, Func<string,Task<T>> getAsync)
    {
        var ret = new Dictionary<string, T>();
        foreach (var key in keys)
        {
            var semaphore = GetSemaphore(key);
            await semaphore.WaitAsync();
            try
            {
                var val = await GetOrCreate(key, getAsync);
                ret.Add(key, val);
            }
            finally
            {
                semaphore.Release();
            }
        }

        return ret;
    }
    
    internal SemaphoreSlim GetSemaphore(string recordId)
        => _locks.GetOrCreate(recordId + ":lock", entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(1));
            return new SemaphoreSlim(1, 1);
        })!;

    internal void Set(string key, T value)
    {
        _cache.Set(key, value);
    }

    internal void SetFlushKey(string key)
    {
        var flushKey = GetNextFlushTimeKey(DateTime.UtcNow);
        
        var dict = _flush.GetOrCreate(flushKey, entry =>
        {
            entry.SetSlidingExpiration(settings.Expiration);
            return new ConcurrentDictionary<string, bool>();
        })!;
        dict[key] = true;
    }
    internal async Task<T> GetOrCreate(string key, Func<string, Task<T>> getAsync)
    {
        var value = await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.SetSlidingExpiration(settings.Expiration);
            return await getAsync(key);
        });
        return value;
    }

    private static string GetNextFlushTimeKey(DateTime now)
    {
        var currentMinute = now.TruncateToMinute();
        var t = now.Second >= 30 ? currentMinute.AddMinutes(1).AddSeconds(30) : currentMinute.AddSeconds(30);
        return $"flush:{t}";
    }
}