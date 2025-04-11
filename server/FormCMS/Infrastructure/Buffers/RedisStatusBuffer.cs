namespace FormCMS.Infrastructure.Buffers;
using StackExchange.Redis;
using System.Threading.Tasks;


public class RedisStatusBuffer : IStatusBuffer
{
    private readonly IDatabase _redisDb;
    private readonly BufferSettings _settings;

    public RedisStatusBuffer(IConnectionMultiplexer redis, BufferSettings settings)
    {
        _redisDb = redis.GetDatabase();
        _settings = settings;
    }

    private static string StatusKey(string userId, string recordId) => $"{userId}:{recordId}:status";
    private static string LockKey(string key) => $"{key}:lock";

    public async Task<bool> Get(string userId, string recordKey, Func<Task<bool>> getStatusAsync)
    {
        var key = StatusKey(userId, recordKey);
        var lockKey = LockKey(key);

        bool lockAcquired = await _redisDb.StringSetAsync(lockKey, "locked", _settings.LockTimeout, When.NotExists);
        if (lockAcquired)
        {
            try
            {
                var value = await _redisDb.StringGetAsync(key);
                if (!value.HasValue)
                {
                    var status = await getStatusAsync();
                    await _redisDb.StringSetAsync(key, status, _settings.Expiration);
                    return status;
                }
                return (bool)value;
            }
            finally
            {
                await _redisDb.KeyDeleteAsync(lockKey);
            }
        }
        else
        {
            await Task.Delay(50);
            var value = await _redisDb.StringGetAsync(key);
            if (!value.HasValue)
            {
                var status = await getStatusAsync();
                await _redisDb.StringSetAsync(key, status, _settings.Expiration, When.NotExists);
                return status;
            }
            return (bool)value;
        }
    }

    public async Task<bool> Toggle(string userId, string recordKey, bool isActive, Func<Task<bool>> getStatusAsync)
    {
        var key = StatusKey(userId, recordKey);
        var lockKey = LockKey(key);

        bool lockAcquired = await _redisDb.StringSetAsync(lockKey, "locked", _settings.LockTimeout, When.NotExists);
        if (lockAcquired)
        {
            try
            {
                var value = await _redisDb.StringGetAsync(key);
                bool currentStatus;
                if (!value.HasValue)
                {
                    currentStatus = await getStatusAsync();
                    await _redisDb.StringSetAsync(key, currentStatus, _settings.Expiration);
                }
                else
                {
                    currentStatus = (bool)value;
                }

                if (currentStatus == isActive)
                {
                    return false;
                }

                await _redisDb.StringSetAsync(key, isActive, _settings.Expiration);
                return true;
            }
            finally
            {
                await _redisDb.KeyDeleteAsync(lockKey);
            }
        }
        else
        {
            await Task.Delay(50);
            var value = await _redisDb.StringGetAsync(key);
            bool currentStatus;
            if (!value.HasValue)
            {
                currentStatus = await getStatusAsync();
                await _redisDb.StringSetAsync(key, currentStatus, _settings.Expiration, When.NotExists);
            }
            else
            {
                currentStatus = (bool)value;
            }

            if (currentStatus == isActive)
            {
                return false;
            }

            await _redisDb.StringSetAsync(key, isActive, _settings.Expiration);
            return true;
        }
    }

    public async Task Set(string userId, string recordKey)
    {
        var key = StatusKey(userId, recordKey);
        await _redisDb.StringSetAsync(key, true, _settings.Expiration);
    }

    public Task<(string, string, bool)[]> GetAfterLastFlush(DateTime lastFlushTime)
    {
        throw new NotImplementedException();
    }
}