using StackExchange.Redis;

namespace FormCMS.Infrastructure.Buffers;

using System.Threading.Tasks;

public class RedisStatusBuffer(IConnectionMultiplexer redis, BufferSettings settings) : IStatusBuffer
{
    private readonly RedisTrackingBuffer<bool> _buffer = new (redis, settings,"status-buffer");
    private static string GetKey(string userId, string recordId) => $"{userId}:{recordId}";


    public Task<bool> Get(string userId, string recordKey, Func<Task<bool>> getStatusAsync)
    {
        return _buffer.SafeGet(GetKey(userId, recordKey), getStatusAsync);
    }

    public Task<bool> Toggle(string userId, string recordKey, bool isActive, Func<Task<bool>> getStatusAsync)
    {
        var key = GetKey(userId, recordKey);
        //the key is for per user, lock won't affect system performance
        return _buffer.DoTaskInLock(key, async () =>
        {
            var res = await _buffer.GetAndParse(key);
            var currentStatus = res.IsSuccess ? res.Value : await getStatusAsync();
            if (currentStatus == isActive)
            {
                if (res.IsFailed) await _buffer.SetValue(recordKey, isActive);
                return false;
            }

            await _buffer.SetValue(recordKey, isActive);
            await _buffer.SetFlushKey(key);
            return true;
        });
    }

    public async Task Set(string userId, string recordKey)
    {
        await _buffer.SetValue(GetKey(userId, recordKey), true);
        await _buffer.SetFlushKey(recordKey);
    }

    public async Task<(string, string, bool)[]> GetAfterLastFlush(DateTime lastFlushTime)
    {
        var values = await _buffer.GetAfterLastFlush(lastFlushTime);
        return values.Select(k =>
        {
            var parts = k.Item1.Split(':');
            return (parts[0], parts[1], k.Item2);
        }).ToArray();
    }
}