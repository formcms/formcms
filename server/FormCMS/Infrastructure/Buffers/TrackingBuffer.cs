using System.Collections.Concurrent;
using FormCMS.Utils.DateTimeExt;
using Microsoft.Extensions.Caching.Memory;

namespace FormCMS.Infrastructure.Buffers;

public class TrackingBuffer<T>(BufferSettings settings) where T : struct
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly IMemoryCache _locks = new MemoryCache(new MemoryCacheOptions());
    private readonly ConcurrentDictionary<string, bool> _keys = [];

    private record CountEntry(T Value, DateTime NextFlushTime);

    internal (string, T)[] GetAfterLastFlush(DateTime lastFlushTime)
    {
        var cacheEntries = new List<(string, T)>();
        var keys = _keys.Keys.ToArray();
        Console.WriteLine($"Getting after last flush time {lastFlushTime}, keys = {string.Join(',', keys)}");
        foreach (var key in keys)
        {
            if (!_cache.TryGetValue(key, out CountEntry? entry)
                || entry == null
                || entry.NextFlushTime == DateTime.MinValue
                || entry.NextFlushTime < lastFlushTime // already flushed last time
               )
                continue;

            cacheEntries.Add((key, entry.Value));
        }
        return cacheEntries.ToArray();
    }
    
    internal async Task<T> SafeGet(string recordId, Func<Task<T>> getAsync)
    {
        var semaphore = GetSemaphore(recordId);
        await semaphore.WaitAsync();
        try
        {
            var count = await Get(recordId, getAsync);
            _keys.TryAdd(recordId, true);
            return count;
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
        var updatedEntry = new CountEntry(value, GetNextFlushTime(DateTime.UtcNow));
        _cache.Set(recordId, updatedEntry);
        // _keys.TryAdd(recordId, true);
        // Console.WriteLine($"added to cache { new {recordId, updatedEntry}}");
        // Console.WriteLine(string.Join(",",_keys.Keys));
    }
    internal async Task<T> Get(string recordId, Func<Task<T>> getAsync)
    {
        var entry = await _cache.GetOrCreateAsync(recordId, async entry =>
        {
            entry.SetSlidingExpiration(settings.Expiration);
            entry.RegisterPostEvictionCallback((k, value, reason, _) =>
            {
                // Console.WriteLine($" {new {k,value, reason}}");
                if (reason == EvictionReason.Expired)
                {
                    _keys.TryRemove(recordId, out var _);
                }
            });
            var v = await getAsync();
            return new CountEntry(v, DateTime.MinValue);
        });
       
        return entry?.Value ?? default;
    }

    private static DateTime GetNextFlushTime(DateTime now)
    {
        var currentMinute = now.TruncateToMinute();
        return now.Second >= 30 ? currentMinute.AddMinutes(1).AddSeconds(30) : currentMinute.AddSeconds(30);
    }
}