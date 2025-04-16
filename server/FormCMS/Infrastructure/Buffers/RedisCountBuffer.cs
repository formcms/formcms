using FormCMS.Utils.LoggerExt;
using StackExchange.Redis;

namespace FormCMS.Infrastructure.Buffers;

public class RedisCountBuffer(IConnectionMultiplexer redis, BufferSettings settings,ILogger<RedisCountBuffer>logger) : ICountBuffer
{
    private readonly object _ = logger.LogInformationEx(
        """
        ***********************************************
        Creating redis count buffer
        ************************************************
        """);
    private readonly RedisTrackingBuffer<long> _buffer = new(
        redis, 
        settings, 
        "count-buffer:", 
        l =>l,
        l=>(long)l);

    public Task<Dictionary<string,long>> GetAfterLastFlush(DateTime lastFlushTime)
        => _buffer.GetAfterLastFlush(lastFlushTime);

    public Task<Dictionary<string, long>> Get(string[] keys, Func<string, Task<long>> getCountAsync)
        => _buffer.SafeGet(keys, getCountAsync);

    public async Task<long> Increase(string recordId, long delta, Func<string,Task<long>> getCountAsync)
    {
        var result = await _buffer.Increase(recordId, delta);
        if (!result.IsNull)
        {
            await _buffer.SetFlushKey([recordId]);
            return (long)result;
        }

        return await _buffer.DoTaskInLock(recordId, async () =>
        {
            //try again, another thread might have set the cache
            result = await _buffer.Increase(recordId, delta);
            if (!result.IsNull)
            {
                await _buffer.SetFlushKey([recordId]);
                return (long)result;
            }

            var count = await getCountAsync(recordId);
            var newCount = count + delta;
            await _buffer.SetValue(recordId, newCount);
            await _buffer.SetFlushKey([recordId]);
            return newCount;
        });

    }
}