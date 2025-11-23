using System.Security.Cryptography;
using System.Text;

namespace FormCMS.Infrastructure.RelationDbDao;

public class ShardRouter(ShardGroup[] shards):IDisposable
{
    public int ShardCount => shards.Length;
    public IPrimaryDao  PrimaryDao(string key) => GetShard(key).Shard.PrimaryDao;
    public IReplicaDao ReplicaDao(string key) => GetShard(key).Shard.ReplicaDao;


    public Task ExecuteAll(Func<IPrimaryDao, Task> func) => Task.WhenAll(shards.Select(x =>
        func(x.PrimaryDao))
    );

    public async Task<T[]> FetchAll<T>(Func<IReplicaDao, Task<T[]>> func)
    {
        var tasks = shards.Select(x => func(x.ReplicaDao));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(x => x).ToArray();
    }

    public Task Execute<T>(
        IEnumerable<T> records,
        Func<T,string> getKeyFunc,
        Func<IPrimaryDao,T[] ,Task> func)
    {
        var dict = new Dictionary<int, (IPrimaryDao dao,List<T> list)>();
        foreach (var record in records)
        {
            var k = getKeyFunc(record);
            var (idx, shard) = GetShard(k);
            if (dict.TryGetValue(idx, out var val))
            {
                val.list.Add(record);
            }
            else
            {
                dict[idx] = (shard.PrimaryDao,[record]);
            }
        }
        var tasks = new List<Task>();
        foreach (var (_, val) in dict)
        {
            tasks.Add(func(val.dao, val.list.ToArray()));
        }
        return Task.WhenAll(tasks);
    }
    private (int Idx,ShardGroup Shard) GetShard(long value)
    {
        // Ensure non-negative
        if (value < 0) value = ~value;

        // Use the total range (last shard's End) for consistent hashing
        var totalRange = shards.Last().End;
        var idx = (int)(value % totalRange);

        for (var i = 0; i < shards.Length; i++)
        {
            if (shards[i].InRange(idx)) return (i, shards[i]);
        }
        throw new InvalidOperationException($"Invalid shard index: {idx} (total range: {totalRange})");
    }       
    
    private (int Idx,ShardGroup Shard) GetShard(string key)
    {
        if (shards.Length == 1)
        {
            return (0, shards[0]);
        }
        // Use stable hash (MD5) so same key -> same shard
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(key));

        // Turn first 4 bytes into an int
        var value = BitConverter.ToInt32(hash, 0);
        return GetShard(value);
    }

    public void Dispose()
    {
        foreach (var shardGroup in shards)
        {
            shardGroup.Dispose();
        }
    }
}