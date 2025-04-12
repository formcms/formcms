using FluentResults;
using FormCMS.Utils.DateTimeExt;
using FormCMS.Utils.ResultExt;
using StackExchange.Redis;

namespace FormCMS.Infrastructure.Buffers;

public static class RedisConstants
{
    internal const int MaxRetryAttempts = 3; // Configurable retry count
    internal const int RetryDelayMs = 100; // Delay between retries in milliseconds
}

public class RedisTrackingBuffer<T>(IConnectionMultiplexer redis,BufferSettings settings, string prefix) where T : struct
{
    private readonly IDatabase _db = redis.GetDatabase();

    internal async Task<(string, T)[]> GetAfterLastFlush(DateTime lastFlushTime)
    {
        var now = DateTime.UtcNow;
        var lockKey = $"{prefix}:flush-lock";
        var lockValue = Guid.NewGuid().ToString();
        var lockTimeout = TimeSpan.FromSeconds(30); 
        
        // only one worker need to do flush work, other threads are blocked in 30 seconds
        var acquired = await _db.StringSetAsync(lockKey, lockValue, lockTimeout, When.NotExists);
        if (!acquired)
        {
            return [];
        } 
        
        var ret = new List<(string, T)>();
        
        for (var t = lastFlushTime; t < now; t = t.AddMinutes(1))
        {
            var key = GetNextFlushTimeKey(t);
            var ids = await _db.SetMembersAsync(key);

            foreach (var id in ids)
            {
                var val = await GetAndParse(id!);
                if (val.IsSuccess)
                {
                    ret.Add((RemovePrefix(id!), val.Value));
                }
            }
            await _db.KeyDeleteAsync(key);
        }
        return ret.ToArray();
    }


    internal async Task<TReturn> DoTaskInLock<TReturn>(string recordId, Func<Task<TReturn>> action)
    {
        recordId = AddPrefix(recordId);
        var lockKey = $"lock:{recordId}";
        var lockValue = Guid.NewGuid().ToString();
        var lockTimeout = TimeSpan.FromSeconds(30);
        var attempts = 0;

        while (attempts < RedisConstants.MaxRetryAttempts)
        {
            // Try to acquire lock using SETNX
            var acquired = await _db.StringSetAsync(lockKey, lockValue, lockTimeout, When.NotExists);
            if (acquired)
            {
                try
                {
                    return await action();
                }
                finally
                {
                    // Only release the lock if we still own it
                    var storedValue = await _db.StringGetAsync(lockKey);
                    if (storedValue == lockValue)
                    {
                        await _db.KeyDeleteAsync(lockKey);
                    } 
                }
            }
            attempts++;
            if (attempts < RedisConstants.MaxRetryAttempts)
            {
                await Task.Delay(RedisConstants.RetryDelayMs);
            }
        }
        throw new ResultException("Failed to acquire lock");
    }

    internal async Task<T> SafeGet(string recordId, Func<Task<T>> getAsync)
    {
        recordId = AddPrefix(recordId);
        
        var res = await GetAndParse(recordId);
        if (res.IsSuccess) return res.Value;

        return await DoTaskInLock(recordId, async () =>
        {
            //another thread might have set cache
            res = await GetAndParse(recordId);
            if (res.IsSuccess) return res.Value;
            
            var value = await getAsync();
            await _db.StringSetAsync(recordId, value.ToString(), settings.Expiration);
            return value;
        });
    }

    internal async Task SetFlushKey(string recordId)
    {
        recordId = AddPrefix(recordId);
        var flushKey = GetNextFlushTimeKey(DateTime.UtcNow);
        await _db.SetAddAsync(flushKey, recordId);
        await _db.KeyExpireAsync(flushKey, settings.Expiration);
    }

    internal Task SetValue(string recordId, T value)
    {
        recordId = AddPrefix(recordId);
        return _db.StringSetAsync(recordId, value.ToString(), settings.Expiration);
    }
    

    internal async Task<Result<T>> GetAndParse(string recordId)
    {
        recordId = AddPrefix(recordId);
        var valStr = await _db.StringGetAsync(recordId);
        if (valStr.HasValue)
        {
            if (typeof(T) == typeof(long) && long.TryParse(valStr, out var longVal))
            {
                return Result.Ok((T)(object)longVal);
            }

            if (typeof(T) == typeof(bool) && bool.TryParse(valStr, out var boolVal))
            {
                return Result.Ok((T)(object)boolVal);
            }
        }
        return Result.Fail<T>($"Could not get record {recordId}");
    }

    internal  Task<RedisResult> Increase(string recordId, long delta)
    {
        const string luaScript = """
                                 if redis.call('EXISTS', KEYS[1]) == 1 then
                                    return redis.call('INCRBY', KEYS[1], ARGV[1])
                                 else
                                    return nil
                                 end
                                 """;
        recordId = AddPrefix(recordId);
        return _db.ScriptEvaluateAsync(luaScript, [recordId],[delta] );
    }
    
    private string AddPrefix(string k) => k.StartsWith(prefix) ? k:prefix + k;
    private string RemovePrefix (string k) => k.StartsWith(prefix) ?k[prefix.Length..]: k;

    private  string GetNextFlushTimeKey(DateTime now)
    {
        var currentMinute = now.TruncateToMinute();
        var t = now.Second >= 30 ? currentMinute.AddMinutes(1).AddSeconds(30) : currentMinute.AddSeconds(30);
        return AddPrefix($"{prefix}:flush:{t:yyyyMMddHHmmss}");
    }
}