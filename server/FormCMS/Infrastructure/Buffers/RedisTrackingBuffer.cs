using FormCMS.Utils.DateTimeExt;
using FormCMS.Utils.ResultExt;
using StackExchange.Redis;

namespace FormCMS.Infrastructure.Buffers;

public static class RedisConstants
{
    internal const int MaxRetryAttempts = 3; // Configurable retry count
    internal const int RetryDelayMs = 100; // Delay between retries in milliseconds
}

public class RedisTrackingBuffer<T>(
    IConnectionMultiplexer redis,
    BufferSettings settings, 
    string prefix,
    Func<T,RedisValue> toRedisValue,
    Func<RedisValue,T> toT
    ) 
{
    private readonly IDatabase _db = redis.GetDatabase();

    internal async Task<Dictionary<string,T>> GetAfterLastFlush(DateTime lastFlushTime)
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
        
        var ret = new Dictionary<string,T>();
        for (var t = lastFlushTime; t < now; t = t.AddMinutes(1))
        {
            var key = GetNextFlushTimeKey(t);
            var ids = await _db.SetMembersAsync(key);
            if (ids.Length > 0)
            {
                var (hits, _) = await GetAndParse(ids.Select(x => x.ToString()).ToArray());
                foreach (var (s, value) in hits)
                {
                    ret[RemovePrefix(s)] = value;
                }
            }

            await _db.KeyDeleteAsync(key);
        }
        return ret;
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

    internal async Task<Dictionary<string,T>> SafeGet(string[] keys, Func<string,Task<T>> getAsync)
    {
        var (hits, misses) = await GetAndParse(keys);
        
        var ret = new Dictionary<string, T>(hits);
        foreach (var key in misses)
        {
            var value = await DoTaskInLock(key, async () =>
            {
                var newValue = await getAsync(RemovePrefix(key));
                await _db.StringSetAsync(AddPrefix(key), toRedisValue(newValue), settings.Expiration);
                return newValue;
            });
            ret.Add(RemovePrefix(key), value);
        }

        return ret;
    }

    internal async Task SetFlushKey(string[] keys)
    {
        if (keys.Length == 0) return;
        
        var flushKey = GetNextFlushTimeKey(DateTime.UtcNow);
        var redisValues = keys.Select(AddPrefix).Select(k => (RedisValue)k).ToArray();

        await _db.SetAddAsync(flushKey, redisValues);
        await _db.KeyExpireAsync(flushKey, settings.Expiration);
    }

    private const string SetValuesLuaScript = """
                                     for i = 1, #KEYS do
                                         redis.call("SET", KEYS[i], ARGV[i], "EX", ARGV[#KEYS + 1])
                                     end
                                     return true
                                     """;

    //StringSetAsync doesn't support setting expiry per key directly in batch mode.
    internal async Task SetValues((string Key, T Value)[] records)
    {
        var keys = records.Select(r => (RedisKey)AddPrefix(r.Key)).ToArray();
        var values = records.Select(r => toRedisValue(r.Value)).ToArray();

        // expiration in seconds
        var expirationSeconds = (int)settings.Expiration.TotalSeconds;

        var allArgs = values.Concat([expirationSeconds]).ToArray();


        await _db.ScriptEvaluateAsync(SetValuesLuaScript, keys, allArgs);
    }

    internal Task SetValue(string recordId, T value)
    {
        recordId = AddPrefix(recordId);
        return _db.StringSetAsync(recordId, toRedisValue(value), settings.Expiration);
    }
    

    internal async Task<(Dictionary<string,T> Hits, string[]Misses)> GetAndParse(string[] keys)
    {
        var redisKeys = keys.Select(k => (RedisKey)AddPrefix(k)).ToArray();
        var redisValues = await _db.StringGetAsync(redisKeys);

        var hits = new Dictionary<string, T>();
        var misses = new List<string>();
        for (var i = 0; i < redisValues.Length; i++)
        {
            if (redisValues[i].HasValue)
            {
                var val = toT(redisValues[i]);
                hits.Add(keys[i], val);
            }
            else
            {
                misses.Add(keys[i]);
            }
        }
        return (hits, misses.ToArray());
    }

    private const string IncreaseLuaScript =
        """
        if redis.call('EXISTS', KEYS[1]) == 1 then
           return redis.call('INCRBY', KEYS[1], ARGV[1])
        else
           return nil
        end
        """;
 
    internal  Task<RedisResult> Increase(string recordId, long delta)
    {
        recordId = AddPrefix(recordId);
        return _db.ScriptEvaluateAsync(IncreaseLuaScript, [recordId],[delta] );
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