using FormCMS.Utils.LoggerExt;

namespace FormCMS.Infrastructure.Buffers;

public class MemoryCountBuffer(BufferSettings settings,ILogger<MemoryCountBuffer> logger) : ICountBuffer
{
    private readonly MemoryTrackingBuffer<long> _buffer = new (settings);

    private readonly object _ = logger.LogInformationEx(
        """
        ***********************************************
        Creating memory count buffer
        ************************************************
        """
    );

    public Task<Dictionary<string, long>> GetAfterLastFlush(DateTime lastFlushTime)
        => Task.FromResult(_buffer.GetAfterLastFlush(lastFlushTime));

    public Task<Dictionary<string,long>> Get(string[] keys, Func<string, Task<long>> getCountAsync)
        => _buffer.SafeGet(keys, getCountAsync);

    public async Task<long> Increase(string key, long delta, Func<string,Task<long>> getCountAsync)
    {
        var semaphore = _buffer.GetSemaphore(key);
        await semaphore.WaitAsync();
        try
        {
            var count = await _buffer.GetOrCreate(key, getCountAsync);
            var newCount = count + delta;
            _buffer.Set(key, newCount);
            _buffer.SetFlushKey(key);
            return newCount;
        }
        finally
        {
            semaphore.Release();
        }
    }
}