using StackExchange.Redis;

namespace FormCMS.Infrastructure.Buffers;

public class RedisCountBuffer(IConnectionMultiplexer redis, BufferSettings settings) : ICountBuffer
{
    private readonly RedisTrackingBuffer<long> _buffer = new(redis, settings, "count-buffer:");

    public Task<(string, long)[]> GetAfterLastFlush(DateTime lastFlushTime)
        => _buffer.GetAfterLastFlush(lastFlushTime);

    public Task<long> Get(string recordId, Func<Task<long>> getCountAsync)
        => _buffer.SafeGet(recordId, getCountAsync);

    public async Task<long> Increase(string recordId, long delta, Func<Task<long>> getCountAsync)
    {
        var result = await _buffer.Increase(recordId, delta);
        if (!result.IsNull)
        {
            await _buffer.SetFlushKey(recordId);
            return (long)result;
        }

        return await _buffer.DoTaskInLock(recordId, async () =>
        {
            //another thread might have set the cache
            result = await _buffer.Increase(recordId, delta);
            if (!result.IsNull)
            {
                await _buffer.SetFlushKey(recordId);
                return (long)result;
            }

            var count = await getCountAsync();
            var newCount = count + delta;
            await _buffer.SetValue(recordId, newCount);
            await _buffer.SetFlushKey(recordId);
            return newCount;
        });

    }
}