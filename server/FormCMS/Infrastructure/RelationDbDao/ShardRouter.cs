using System.Security.Cryptography;
using System.Text;

namespace FormCMS.Infrastructure.RelationDbDao;

public record ShardRouterConfig(ShardConfig[] ShardConfigs );
    
public class ShardRouter(ShardGroup[] shards):IDisposable
{
    public IRelationDbDao  PrimaryDao(string key) => GetShard(key).Shard.PrimaryDao;
    public IRelationDbDao ReplicaDao(string key) => GetShard(key).Shard.ReplicaDao;
        
    public async Task<T[]> Fetch<T>(
        string[] keys,
        Func<IRelationDbDao, string[],Task<T[]>> func
    )
    {
        var dict = new Dictionary<int, (IRelationDbDao dao,List<string>list)>();
        foreach (var key in keys)
        {
            var (idx, shard) = GetShard(key);
            if (dict.TryGetValue(idx, out var value))
            {
                value.list.Add(key);
            }
            else
            {
                dict[idx] = (shard.ReplicaDao, [key]);
            }
        }
        var tasks = new List<Task<T[]>>();
        foreach (var (_, val) in dict)
        {
            tasks.Add(func(val.dao, val.list.ToArray()));
        }
        var results = await Task.WhenAll(tasks);
        var allResults = results.SelectMany(r => r).ToArray();
        return allResults;
    }

    public Task ExecuteAll(Func<IRelationDbDao, Task> func) => Task.WhenAll(shards.Select(x =>
        func(x.PrimaryDao))
    );

    public Task Execute<T>(
        IEnumerable<T> records,
        Func<T,string> getKeyFunc,
        Func<IRelationDbDao,T[] ,Task> func)
    {
        var dict = new Dictionary<int, (IRelationDbDao dao,List<T> list)>();
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

        var idx = (int)value % shards.Last().End;
        for (var i = 0; i < shards.Length; i++)
        {
            if (shards[i].InRange(idx)) return (i, shards[i]);
        }
        throw new InvalidOperationException("Invalid shard index.");
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