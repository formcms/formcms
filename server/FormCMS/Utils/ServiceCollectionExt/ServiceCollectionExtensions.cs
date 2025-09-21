using FormCMS.Infrastructure.RelationDbDao;
using Microsoft.Data.Sqlite;
using Npgsql;
using Microsoft.Data.SqlClient;
using MySqlConnector;

namespace FormCMS.Utils.ServiceCollectionExt;


public static class ServiceCollectionExtensions
{


    public  static IServiceCollection  AddDao(this IServiceCollection services, DatabaseProvider databaseProvider, string connectionString)
    {
        _ = databaseProvider switch
        {
            DatabaseProvider.Sqlite => AddSqliteDbServices(),
            DatabaseProvider.Postgres => AddPostgresDbServices(),
            DatabaseProvider.SqlServer => AddSqlServerDbServices(),
            DatabaseProvider.Mysql => AddMysqlDbServices(),
            _ => throw new Exception("unsupported database provider")
        };

        IServiceCollection AddSqliteDbServices()
        {
            services.AddScoped(_ => new SqliteConnection(connectionString));
            services.AddScoped<IRelationDbDao, SqliteDao>();
            return services;
        }

        IServiceCollection AddSqlServerDbServices()
        {
            services.AddScoped(_ =>  new SqlConnection(connectionString));
            services.AddScoped<IRelationDbDao, SqlServerDao>();
            return services;
        }

        IServiceCollection AddPostgresDbServices()
        {
            services.AddScoped(_ => new NpgsqlConnection(connectionString));
            services.AddScoped<IRelationDbDao, PostgresDao>();
            return services;
        }
        
        IServiceCollection AddMysqlDbServices()
        {
            services.AddScoped(_ => new MySqlConnection(connectionString));
            services.AddScoped<IRelationDbDao, MySqlDao>();
            return services;
        }

        return services;
    }
}