namespace FormCMS.Infrastructure.Buffers;

public class MemoryStatusBuffer(BufferSettings settings): IStatusBuffer
{
    private readonly TrackingBuffer<bool> _buffer = new (settings);
    private static string GetKey (string userId, string recordId) => $"{userId}:{recordId}";

    public Task<(string, string, bool)[]> GetAfterLastFlush(DateTime lastFlushTime)
    {
        var values = _buffer.GetAfterLastFlush(lastFlushTime).Select(k =>
        {
            var parts = k.Item1.Split(':');
            return (parts[0], parts[1], k.Item2);
        }).ToArray();
        return Task.FromResult(values);
    } 

    public Task<bool> Get(string userId,string recordKey, Func<Task<bool>> getAsync)
        => _buffer.SafeGet(GetKey(userId , recordKey), getAsync);
    
    // Set or update the activity status
    public async Task<bool> Toggle(string userId, string recordKey, bool status, Func<Task<bool>> getStatusAsync)
    {
        var statusKey =GetKey(userId, recordKey);
        var semaphore = _buffer.GetSemaphore(statusKey);
        await semaphore.WaitAsync();
        try
        {
            var currentStatus = await _buffer.Get(statusKey, getStatusAsync);
            if (currentStatus == status) return false;
            _buffer.Set(statusKey,status);
            return true;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public Task Set(string userId, string recordKey)
    {
        var statusKey = GetKey(userId, recordKey);
        _buffer.Set(statusKey, true);
        return Task.CompletedTask;
    }
}