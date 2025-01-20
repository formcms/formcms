using FormCMS.Infrastructure.RelationDbDao;
using Microsoft.Data.Sqlite;
using Npgsql;
using Microsoft.Data.SqlClient;

namespace FormCMS.Cms.Builders;

public static class Utils
{
    public  static IServiceCollection  AddDao(this IServiceCollection services, DatabaseProvider databaseProvider, string connectionString)
    {
        _ = databaseProvider switch
        {
            DatabaseProvider.Sqlite => AddSqliteDbServices(),
            DatabaseProvider.Postgres => AddPostgresDbServices(),
            DatabaseProvider.SqlServer => AddSqlServerDbServices(),
            _ => throw new Exception("unsupported database provider")
        };




        IServiceCollection AddSqliteDbServices()
        {
            services.AddScoped(_ =>
            {
                var connection = new SqliteConnection(connectionString);
                connection.Open();
                return connection;
            });
            services.AddScoped<IRelationDbDao, SqliteDao>();
            return services;
        }

        IServiceCollection AddSqlServerDbServices()
        {
            services.AddScoped(_ =>
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                return connection;
            });
            services.AddScoped<IRelationDbDao, SqlServerIDao>();
            return services;
        }

        IServiceCollection AddPostgresDbServices()
        {
            services.AddScoped(_ =>
            {
                var connection = new NpgsqlConnection(connectionString);
                connection.Open();
                return connection;
            });

            services.AddScoped<IRelationDbDao, PostgresDao>();
            return services;
        }
        return services;
    }
}