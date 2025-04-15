using FormCMS.Utils.LoggerExt;
using StackExchange.Redis;

namespace FormCMS.Infrastructure.Buffers;

using System.Threading.Tasks;

public class RedisStatusBuffer(IConnectionMultiplexer redis, BufferSettings settings, ILogger<RedisStatusBuffer> logger) : IStatusBuffer
{
    private readonly object _ = logger.LogInformationEx(
        """
        ***********************************************
        Creating redis status buffer
        ************************************************
        """);

    private readonly RedisTrackingBuffer<bool> _buffer = new(
        redis,
        settings,
        "status-buffer:", b => b,
        b => (bool)b
    );
    
    public Task<bool> Toggle(string key, bool isActive, Func<string,Task<bool>> getStatusAsync)
    {
        //the key is for per user, lock won't affect system performance
        return _buffer.DoTaskInLock(key, async () =>
        {
            var (hits,_) = await _buffer.GetAndParse([key]);
            var currentStatus =  hits.Count > 0? hits.First().Value : await getStatusAsync(key);
            if (currentStatus == isActive)
            {
                if (hits.Count == 0) await _buffer.SetValue(key, isActive);
                return false;
            }

            await _buffer.SetValue(key, isActive);
            await _buffer.SetFlushKey([key]);
            return true;
        });
    }

    public Task<Dictionary<string,bool>> Get(string[] keys, Func<string,Task<bool>> getStatusAsync)
    {
        return _buffer.SafeGet(keys,getStatusAsync);
    }

    public async Task Set(string[] keys)
    {
        if (keys.Length == 0) return;
        var records = keys.Select(x => (x, true)).ToArray();
        await _buffer.SetValues(records);
        await _buffer.SetFlushKey(keys);
    }

    public Task<Dictionary<string,bool>> GetAfterLastFlush(DateTime lastFlushTime)
        => _buffer.GetAfterLastFlush(lastFlushTime);
}