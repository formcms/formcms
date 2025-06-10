using FormCMS.Infrastructure.RelationDbDao;
using Microsoft.Data.Sqlite;
using Npgsql;
using Microsoft.Data.SqlClient;
using NATS.Client.Core;

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
        return services;
    }
    public static IServiceCollection AddMsg(
        this IServiceCollection services,
        MessagingProvider msgProvider,
        string connectionString
    )
    {
        _ = msgProvider switch
        {
            MessagingProvider.Nats => AddNatsMessing(),
            _ => throw new Exception("unsupported Messing Provider"),
        };
        IServiceCollection AddNatsMessing()
        {
            services.AddScoped(_ => new NatsConnection(new NatsOpts() { Url = connectionString }));

            return services;
        }
        return services;
    }
}