namespace FormCMS.Infrastructure.Buffers;

public class MemoryCountBuffer(BufferSettings settings) : ICountBuffer
{
    private readonly MemoryTrackingBuffer<long> _buffer = new (settings);

    public Task<(string, long)[]> GetAfterLastFlush(DateTime lastFlushTime)
        => Task.FromResult(_buffer.GetAfterLastFlush(lastFlushTime));

    public Task<long> Get(string recordId, Func<Task<long>> getCountAsync)
        => _buffer.SafeGet(recordId, getCountAsync);

    public async Task<long> Increase(string recordId, long delta, Func<Task<long>> getCountAsync)
    {
        var semaphore = _buffer.GetSemaphore(recordId);
        await semaphore!.WaitAsync();
        try
        {
            var count = await _buffer.Get(recordId, getCountAsync);
            var newCount = count + delta;
            _buffer.Set(recordId, newCount);
            return newCount;
        }
        finally
        {
            semaphore.Release();
        }
    }
}