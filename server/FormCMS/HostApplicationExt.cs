
using FormCMS.Cms.Builders;
using FormCMS.DataLink.Types;
using FormCMS.DataLink.Builders;
using FormCMS.Utils.ServiceCollectionExt;
using FormCMS.Video.Workers;

namespace FormCMS;

public static class HostApplicationExt
{
    public static IServiceCollection AddNatsMongoLink(
        this IServiceCollection collection,
        ApiLinks[] apiLinksArray
    ) => DocDbLinkerBuilder.AddNatsMongoLink(collection, apiLinksArray);
    
    public static IServiceCollection AddPostgresCmsWorker(
        this IServiceCollection services, string connectionString, TaskTimingSeconds? taskTimingSeconds = null
    ) => CmsWorkerBuilder.AddWorker(services, DatabaseProvider.Postgres, connectionString, taskTimingSeconds);

    public static IServiceCollection AddSqliteCmsWorker(
        this IServiceCollection services, string connectionString, TaskTimingSeconds? taskTimingSeconds = null
    ) => CmsWorkerBuilder.AddWorker(services, DatabaseProvider.Sqlite,connectionString,taskTimingSeconds);

    public static IServiceCollection AddSqlServerCmsWorker(
        this IServiceCollection services, string connectionString, TaskTimingSeconds? taskTimingSeconds = null
    ) => CmsWorkerBuilder.AddWorker(services, DatabaseProvider.SqlServer,connectionString,taskTimingSeconds);

    public static IServiceCollection WithNats(this IServiceCollection services, string natsConnectionString
        ) => services.AddMsg( MessagingProvider.Nats, natsConnectionString);

    public static IServiceCollection AddVideoWorker(this IServiceCollection services, int ffmpegDelay)
        => services.AddHostedService<FFMpegWorker>();

}
