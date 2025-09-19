using System.Security.Cryptography;
using System.Text;

namespace FormCMS.Infrastructure.RelationDbDao
{
    public record ShardManagerConfig(DatabaseProvider DatabaseProvider, ShardConfig[] ShardConfigs );
    
    public class ShardManager(Shard[] shards)
    {
        public async Task<T[]> Fetch<T>(
            string[] keys,
            Func<IRelationDbDao, string[],Task<T[]>> func
            )
        {
            var dict = new Dictionary<int, (IRelationDbDao dao,List<string>list)>();
            foreach (var key in keys)
            {
                var (idx, dao) = Follower(key);
                if (dict.TryGetValue(idx, out var value))
                {
                    value.list.Add(key);
                }
                else
                {
                    dict[idx] = (dao!, [key]);
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

        public Task Execute( Func<IRelationDbDao, Task> func) => Task.WhenAll(shards.Select(x => func(x.Leader)));

        public Task Execute<T>(
            IEnumerable<T> records,
            Func<T,string> getKeyFunc,
            Func<IRelationDbDao,T[] ,Task> func)
        {
            var dict = new Dictionary<int, (IRelationDbDao dao,List<T> list)>();
            foreach (var record in records)
            {
                var k = getKeyFunc(record);
                var (idx,dao) = Leader(k);
                if (dict.TryGetValue(idx, out var val))
                {
                    val.list.Add(record);
                }
                else
                {
                    dict[idx] = (dao!,[record]);
                }
            }
            var tasks = new List<Task>();
            foreach (var (_, val) in dict)
            {
                tasks.Add(func(val.dao, val.list.ToArray()));
            }
            return Task.WhenAll(tasks);
        }
        
        public  (int Idx,IRelationDbDao Dao) Leader(string key)
        {
           var (idx, shard) = GetShard(key);
            return (idx, shard.Leader);
        }
        
        
        public KateQueryExecutor FollowExecutor(string key, KateQueryExecutorOption option)
        => new KateQueryExecutor(Follower(key).Dao, option);
        
        public KateQueryExecutor LeadExecutor(string key, KateQueryExecutorOption option)
        {
            var (_,dao) = Leader(key);
            return new KateQueryExecutor(dao, option);
        }

        public (int Idx,IRelationDbDao Dao) Follower(string key)
        {
           var (idx, shard) = GetShard(key);
            return (idx, shard.Follower);
        }

        private (int,Shard) GetShard(string key)
        {
            if (shards.Length == 1)
            {
                return (0,shards[0]);
            }
            // Use stable hash (MD5) so same key -> same shard
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key));

            // Turn first 4 bytes into an int
            var value = BitConverter.ToInt32(hash, 0);

            // Ensure non-negative
            if (value < 0) value = ~value;

            var idx = value % shards.Last().End;
            for (var i = 0; i < shards.Length; i++)
            {
                if (shards[i].InRange(idx)) return (i,shards[i]);
            }
            throw new InvalidOperationException("Invalid shard index.");
        }
    }
}