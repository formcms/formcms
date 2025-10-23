using FormCMS.Infrastructure.Fts;
using FormCMS.Infrastructure.RelationDbDao;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;

namespace FormCMS.Utils.ServiceCollectionExt;

public static class ServiceProviderExtensions
{

    public static ShardGroup CreateShard(
        this IServiceProvider sp, 
        DatabaseProvider? databaseProvider,
        ShardConfig? shardConfig)
    {
        if (databaseProvider == null || shardConfig == null)
        {
            return sp.GetRequiredService<ShardGroup>();
        }

        var leader = sp.CreateDao(databaseProvider.Value, shardConfig.LeadConnStr);
        var follower = (shardConfig.FollowConnStrings??[])
            .Select(x => sp.CreateDao(databaseProvider.Value, x))
            .ToArray();
        return new ShardGroup(leader, follower, shardConfig.Start, shardConfig.End);
    }

    public static ShardRouter CreateShardManager(this IServiceProvider sp,  ShardRouterConfig? configs = null)
    {
        if (configs == null)
        {
            return new ShardRouter([sp.GetRequiredService<ShardGroup>()]);
        }

        var shards = configs.ShardConfigs.Select(config => sp.CreateShard(configs.DatabaseProvider, config));
        return new ShardRouter(shards.ToArray());
    }

    public static IFullTextSearch CreateFullTextSearch(this IServiceProvider sp, 
        FtsProvider provider, string primaryConnString, string[] replicaConnStrings )
        => provider switch
        {
            FtsProvider.Mysql => new MysqlFts(
                new MySqlConnection(primaryConnString),
                replicaConnStrings.Select(x => new MySqlConnection(x)).ToArray()),
            FtsProvider.Sqlite => new SqliteFts(
                new SqliteConnection(primaryConnString),
                replicaConnStrings.Select(x => new SqliteConnection(x)).ToArray()),
            FtsProvider.Postgres => new PostgresFts(
                new NpgsqlConnection(primaryConnString),
                replicaConnStrings.Select(x => new NpgsqlConnection(x)).ToArray()),
            FtsProvider.SqlServer => new SqlServerFts(
                new SqlConnection(primaryConnString),
                replicaConnStrings.Select(x => new SqlConnection(x)).ToArray()),
            _ => throw new NotImplementedException(),
        };

    private static IRelationDbDao CreateDao(this IServiceProvider sp, DatabaseProvider databaseProvider, string connectionString)
        => databaseProvider switch
        {
            DatabaseProvider.Mysql => new MySqlDao(
                new MySqlConnection(connectionString),
                sp.GetRequiredService<ILogger<MySqlDao>>()),
            DatabaseProvider.Sqlite => new SqliteDao(
                new SqliteConnection(connectionString),
                sp.GetRequiredService<ILogger<SqliteDao>>()),
            DatabaseProvider.Postgres => new PostgresDao(
                new NpgsqlConnection(connectionString),
                sp.GetRequiredService<ILogger<PostgresDao>>()),
            DatabaseProvider.SqlServer => new SqlServerDao(
                new SqlConnection(connectionString),
                sp.GetRequiredService<ILogger<SqlServerDao>>()),
            _ => throw new NotImplementedException(),
        };
}