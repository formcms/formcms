using FormCMS.Cms.Workers;
using FormCMS.Infrastructure.LocalFileStore;
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
        var parts = connectionString.Split(";").Where(x => !x.StartsWith("Password"));
        Console.WriteLine(
            $"""
             *********************************************************
             Adding CMS Workers
             Database : {databaseProvider} - {string.Join(";", parts)}
             Delay Seconds: {delaySeconds}
             Query Timeout: {queryTimeoutSeconds}
             *********************************************************
             """);

        services.AddSingleton(new LocalFileStoreOptions(
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files"), "/files",0, 0));
        services.AddSingleton<IFileStore,LocalFileStore>();
        
        //scoped services
        services.AddDao(databaseProvider, connectionString );
        services.AddSingleton(new KateQueryExecutorOption(queryTimeoutSeconds));
        services.AddScoped<KateQueryExecutor>();
        services.AddScoped<DatabaseMigrator>();
        
        services.AddHostedService<ExportWorker>();
        services.AddHostedService<ImportWorker>();
        services.AddHostedService<DataPublishingWorker>();
        
        return services;
    }
    
}