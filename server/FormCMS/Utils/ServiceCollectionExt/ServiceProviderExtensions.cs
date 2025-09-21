using FormCMS.Infrastructure.RelationDbDao;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;

namespace FormCMS.Utils.ServiceCollectionExt;

public static class ServiceProviderExtensions
{
   
    public static ShardManager CreateShardManager(this IServiceProvider? sp, ShardManagerConfig? shardConfig)
    {
        if (shardConfig == null)
        {
            return new ShardManager([new Shard(sp.GetRequiredService<IRelationDbDao>())]);
        }

        var shards = shardConfig.ShardConfigs.Select(config =>
        {
            var leader = CreateDao(config.leaderConnStr);
            var follower = config.followConnStrings.Select(CreateDao).ToArray();
            return new Shard(leader, follower, config.Start, config.End);
        });
        return new ShardManager(shards.ToArray());

        IRelationDbDao CreateDao(string connectionString)
            => shardConfig.DatabaseProvider switch
            {
                DatabaseProvider.Mysql => new MySqlDao(new MySqlConnection(connectionString),
                    sp.GetRequiredService<ILogger<MySqlDao>>()),
                DatabaseProvider.Sqlite => new SqliteDao(new SqliteConnection(connectionString),
                    sp.GetRequiredService<ILogger<SqliteDao>>()),
                DatabaseProvider.Postgres => new PostgresDao(new NpgsqlConnection(connectionString),
                    sp.GetRequiredService<ILogger<PostgresDao>>()),
                DatabaseProvider.SqlServer => new SqlServerDao(new SqlConnection(connectionString),
                    sp.GetRequiredService<ILogger<SqlServerDao>>()),
                _ => throw new NotImplementedException(),
            };
    } 
}