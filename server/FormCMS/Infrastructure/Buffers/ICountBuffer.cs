namespace FormCMS.Infrastructure.Buffers;

public interface ICountBuffer
{
    Task<Dictionary<string,long>> GetAfterLastFlush(DateTime lastFlushTime);
    Task<Dictionary<string,long>> Get(string[] keys, Func<string,Task<long>> getCountAsync);
    Task<long> Increase(string key, long delta, Func<string,Task<long>> getCountAsync);
}