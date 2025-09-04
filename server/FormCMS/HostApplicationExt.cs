using FormCMS.Cms.Builders;

namespace FormCMS;

public static class HostApplicationExt
{
    public static IServiceCollection AddPostgresCmsWorker(
        this IServiceCollection services, string connectionString, TaskTimingSeconds? taskTimingSeconds = null
    ) => CmsWorkerBuilder.AddWorker(services, DatabaseProvider.Postgres, connectionString, taskTimingSeconds);

    public static IServiceCollection AddSqliteCmsWorker(
        this IServiceCollection services, string connectionString, TaskTimingSeconds? taskTimingSeconds = null
    ) => CmsWorkerBuilder.AddWorker(services, DatabaseProvider.Sqlite,connectionString,taskTimingSeconds);

    public static IServiceCollection AddSqlServerCmsWorker(
        this IServiceCollection services, string connectionString, TaskTimingSeconds? taskTimingSeconds = null
    ) => CmsWorkerBuilder.AddWorker(services, DatabaseProvider.SqlServer,connectionString,taskTimingSeconds);
    
    public static IServiceCollection AddMySqlCmsWorker(
        this IServiceCollection services, string connectionString, TaskTimingSeconds? taskTimingSeconds = null
    ) => CmsWorkerBuilder.AddWorker(services, DatabaseProvider.Mysql,connectionString,taskTimingSeconds);
}
