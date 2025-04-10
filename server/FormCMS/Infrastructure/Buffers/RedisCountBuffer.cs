using Microsoft.EntityFrameworkCore.Storage;
using StackExchange.Redis;
using IDatabase = StackExchange.Redis.IDatabase;

namespace FormCMS.Infrastructure.Buffers;

public class RedisCountBuffer(IConnectionMultiplexer redis, BufferSettings settings) : ICountBuffer
{
    private readonly IDatabase _redisDb = redis.GetDatabase();

    private static string CountKey(string k) => $"{k}:count";
    private static string LockKey(string k) => $"{k}:lock";

    public Task<(string, long)[]> GetAfterLastFlush(DateTime lastFlushTime)
    {
        throw new NotImplementedException();
    }

    public async Task<long> Get(string recordId, Func<Task<long>> getCountAsync)
    {
        var key = CountKey(recordId);
        var lockKey = LockKey(key);

        // Try to acquire lock
        bool lockAcquired = await _redisDb.StringSetAsync(lockKey, "locked", settings.LockTimeout, When.NotExists);
        if (lockAcquired)
        {
            try
            {
                var value = await _redisDb.StringGetAsync(key);
                if (!value.HasValue)
                {
                    var count = await getCountAsync();
                    await _redisDb.StringSetAsync(key, count, settings.Expiration);
                    return count;
                }
                return (long)value;
            }
            finally
            {
                await _redisDb.KeyDeleteAsync(lockKey);
            }
        }
        else
        {
            // Lock not acquired, wait and retry
            await Task.Delay(50);
            var value = await _redisDb.StringGetAsync(key);
            if (!value.HasValue)
            {
                // Fallback to DB if still missing
                var count = await getCountAsync();
                await _redisDb.StringSetAsync(key, count, settings.Expiration, When.NotExists); // Avoid overwriting
                return count;
            }
            return (long)value;
        }
    }

    public async Task<long> Increase(string recordId, long delta, Func<Task<long>> getCountAsync)
    {
        var key = CountKey(recordId);
        var lockKey = LockKey(key);

        // Try to acquire lock
        bool lockAcquired = await _redisDb.StringSetAsync(lockKey, "locked", settings.LockTimeout, When.NotExists);
        if (lockAcquired)
        {
            try
            {
                var value = await _redisDb.StringGetAsync(key);
                if (!value.HasValue)
                {
                    var dbCount = await getCountAsync();
                    await _redisDb.StringSetAsync(key, dbCount, settings.Expiration);
                    value = dbCount;
                }
                var newCount = await _redisDb.StringIncrementAsync(key, delta);
                await _redisDb.KeyExpireAsync(key, settings.Expiration);
                return newCount;
            }
            finally
            {
                await _redisDb.KeyDeleteAsync(lockKey);
            }
        }
        else
        {
            // Lock not acquired, wait and increment
            await Task.Delay(50);
            var newCount = await _redisDb.StringIncrementAsync(key, delta);
            await _redisDb.KeyExpireAsync(key, settings.Expiration);
            return newCount;
        }
    }
}