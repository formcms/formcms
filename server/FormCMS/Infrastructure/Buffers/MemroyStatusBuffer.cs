using FormCMS.Utils.LoggerExt;

namespace FormCMS.Infrastructure.Buffers;

public class MemoryStatusBuffer(BufferSettings settings, ILogger<MemoryStatusBuffer> logger) : IStatusBuffer
{
    private readonly MemoryTrackingBuffer<bool> _buffer = new(settings);

    private readonly object _ = logger.LogInformationEx(
        """
        ***********************************************
        Creating memory status buffer
        ************************************************
        """
    );

    public Task<Dictionary<string,bool>> Get(string[] keys, Func<string, Task<bool>> getStatus)
        => _buffer.SafeGet(keys, getStatus);

    public Task<Dictionary<string,bool>> GetAfterLastFlush(DateTime lastFlushTime)
        => Task.FromResult(_buffer.GetAfterLastFlush(lastFlushTime));

    public async Task<bool> Toggle(string key, bool status, Func<string, Task<bool>> getStatus)
    {
        var semaphore = _buffer.GetSemaphore(key);
        await semaphore.WaitAsync();
        try
        {
            var currentStatus = await _buffer.GetOrCreate(key, getStatus);
            if (currentStatus == status) return false;
            _buffer.Set(key, status);
            _buffer.SetFlushKey(key);
            return true;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public Task Set(string[] keys)
    {
        foreach (var key in keys)
        {
            _buffer.Set(key, true);
        }

        return Task.CompletedTask;
    }
}