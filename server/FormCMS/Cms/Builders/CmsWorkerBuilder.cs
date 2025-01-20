using FormCMS.Cms.Workers;
using FormCMS.Infrastructure.RelationDbDao;

namespace FormCMS.Cms.Builders;

public static class CmsWorkerBuilder
{
    public static IServiceCollection AddWorker(
        IServiceCollection services,
        DatabaseProvider databaseProvider,
        string connectionString,
        int delaySeconds,
        int queryTimeoutSeconds
        )
    {
        Console.WriteLine(
            $"""
             *********************************************************
             Adding CMS Publishing Worker
             Delay Seconds: {delaySeconds}
             Query Timeout: {queryTimeoutSeconds}
             *********************************************************
             """);

        services.AddDao(databaseProvider, connectionString );
        
        services.AddSingleton(new KateQueryExecutorOption(queryTimeoutSeconds));
        services.AddScoped<KateQueryExecutor>();
        
        services.AddSingleton(new DataPublishingWorkerOptions(delaySeconds));
        services.AddHostedService<DataPublishingWorker>();
        return services;
    }
    
}