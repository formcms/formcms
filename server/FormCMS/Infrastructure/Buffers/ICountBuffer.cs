namespace FormCMS.Infrastructure.Buffers;

public interface ICountBuffer
{
    Task<(string, long)[]> GetAfterLastFlush(DateTime lastFlushTime);
    Task<long> Get(string recordId, Func<Task<long>> getCountAsync);
    Task<long> Increase(string recordId, long delta, Func<Task<long>> getCountAsync);
}