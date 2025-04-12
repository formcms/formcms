using System.Collections.Concurrent;
using FormCMS.Utils.DateTimeExt;
using Microsoft.Extensions.Caching.Memory;

namespace FormCMS.Infrastructure.Buffers;

public class MemoryTrackingBuffer<T>(BufferSettings settings) where T : struct
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly IMemoryCache _locks = new MemoryCache(new MemoryCacheOptions());
    private readonly IMemoryCache _flush = new MemoryCache(new MemoryCacheOptions());
    
    internal (string, T)[] GetAfterLastFlush(DateTime lastFlushTime)
    {
        var now = DateTime.UtcNow;
       
        var ret = new List<(string, T)>();
        for (var t = lastFlushTime ; t < now; t = t.AddMinutes(1))
        {
            var key = GetNextFlushTimeKey(t);
            if (!_flush.TryGetValue(key, out var value) ||
                value is not ConcurrentDictionary<string, bool> dict) continue;
            
            foreach (var k in dict.Keys)
            {
                if (_cache.TryGetValue(k, out var cachedValue) && cachedValue is T v)
                {
                    ret.Add((k, v));
                }
            }
            _flush.Remove(key);
        }
        return ret.ToArray();
    }
    
    internal async Task<T> SafeGet(string recordId, Func<Task<T>> getAsync)
    {
        var semaphore = GetSemaphore(recordId);
        await semaphore.WaitAsync();
        try
        {
            return await Get(recordId, getAsync);
        }
        finally
        {
            semaphore.Release();
        }
    }
    
    internal SemaphoreSlim GetSemaphore(string recordId)
        => _locks.GetOrCreate(recordId + ":lock", entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(1));
            return new SemaphoreSlim(1, 1);
        })!;

    internal void Set(string recordId, T value)
    {
        _cache.Set(recordId, value);
        var flushKey = GetNextFlushTimeKey(DateTime.UtcNow);
        
        var dict = _flush.GetOrCreate(flushKey, entry =>
        {
            entry.SetSlidingExpiration(settings.Expiration);
            return new ConcurrentDictionary<string, bool>();
        })!;
        dict[recordId] = true;
    }
    internal async Task<T> Get(string recordId, Func<Task<T>> getAsync)
    {
        var value = await _cache.GetOrCreateAsync(recordId, async entry =>
        {
            entry.SetSlidingExpiration(settings.Expiration);
            return await getAsync();
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