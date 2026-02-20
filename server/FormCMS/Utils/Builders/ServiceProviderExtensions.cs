using FormCMS.Infrastructure.Fts;
using FormCMS.Infrastructure.RelationDbDao;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;

namespace FormCMS.Utils.Builders;

public static class ServiceProviderExtensions
{
    public static ShardGroup CreateShard(
        this IServiceProvider sp, 
        ShardConfig shardConfig)
    {
        var leader = sp.CreateDao(shardConfig.DatabaseProvider, shardConfig.LeadConnStr);
        var follower = (shardConfig.FollowConnStrings??[])
            .Select(x => sp.CreateDao(shardConfig.DatabaseProvider, x))
            .ToArray();
        return new ShardGroup(leader, follower, shardConfig.Start, shardConfig.End);
    }

    public static ShardRouter CreateShardRouter(this IServiceProvider sp, ShardConfig[] configs)
        => new (configs.Select(sp.CreateShard).ToArray());
    
    public static IFullTextSearch CreateFullTextSearch(this IServiceProvider sp, 
        FtsProvider provider, string primaryConnString, string[] replicaConnStrings )
        => provider switch
        {
            FtsProvider.Mysql => new MysqlFts(
                new MySqlConnection(primaryConnString),
                replicaConnStrings.Select(x => new MySqlConnection(x)).ToArray(),
                sp.GetRequiredService<ILogger<MysqlFts>>()
                ),
            FtsProvider.Sqlite => new SqliteFts(
                new SqliteConnection(primaryConnString),
                replicaConnStrings.Select(x => new SqliteConnection(x)).ToArray(),
            sp.GetRequiredService<ILogger<SqliteFts>>()
                ),
            FtsProvider.Postgres => new PostgresFts(
                new NpgsqlConnection(primaryConnString),
                replicaConnStrings.Select(x => new NpgsqlConnection(x)).ToArray(),
                sp.GetRequiredService<ILogger<PostgresFts>>()
                ),
            FtsProvider.SqlServer => new SqlServerFts(
                new SqlConnection(primaryConnString),
                replicaConnStrings.Select(x => new SqlConnection(x)).ToArray(),
                sp.GetRequiredService<ILogger<SqlServerFts>>()
                ),
            _ => throw new NotImplementedException(),
        };

    public static IPrimaryDao CreateDao(this IServiceProvider sp, DatabaseProvider databaseProvider, string connectionString)
        => databaseProvider switch
        {
           
            DatabaseProvider.Sqlite => new SqliteDao(
                new SqliteConnection(connectionString),
                sp.GetRequiredService<ILogger<SqliteDao>>()),
            DatabaseProvider.Postgres => new PostgresDao(
                new NpgsqlConnection(connectionString),
                sp.GetRequiredService<ILogger<PostgresDao>>()),
            DatabaseProvider.SqlServer => new SqlServerDao(
                new SqlConnection(connectionString),
                sp.GetRequiredService<ILogger<SqlServerDao>>()),
            DatabaseProvider.Mysql =>
            new MySqlDao(
               new MySqlConnection(connectionString),
               sp.GetRequiredService<ILogger<MySqlDao>>()),
            _ => throw new NotImplementedException(),
        };
}