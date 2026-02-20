using FormCMS.Cms.Workers;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Infrastructure.ImageUtil;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.Builders;

namespace FormCMS.Cms.Builders;

public record TaskTimingSeconds(
    int ExportDelay,
    int ImportDelay,
    int PublishDelay,
    int EmitMessageDelay
);

public static class CmsWorkerBuilder
{
    public static IServiceCollection AddWorker(
        IServiceCollection services,
        DatabaseProvider databaseProvider,
        string connectionString,
        TaskTimingSeconds? taskTimingSeconds
    )
    {
        taskTimingSeconds ??= new TaskTimingSeconds(60, 30, 30, 30);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }
        var parts = connectionString.Split(";").Where(x => !x.StartsWith("Password"));

        services.AddSingleton(new ResizeOptions(1200, 90));
        services.AddSingleton<SkiaSharpResizer>();
        services.AddSingleton<IFileStore, LocalFileStore>();

        services.AddSingleton(new ExportWorkerOptions(taskTimingSeconds.ExportDelay));
        services.AddHostedService<ExportWorker>();

        services.AddSingleton(new ImportWorkerOptions(taskTimingSeconds.ImportDelay));
        services.AddHostedService<ImportWorker>();


        services.AddScoped<ShardGroup>(sp => sp.CreateShard(new ShardConfig(databaseProvider, connectionString)));
  
        services.AddSingleton(new DataPublishingWorkerOptions(taskTimingSeconds.PublishDelay));
        services.AddHostedService<DataPublishingWorker>();
       
        
        Console.WriteLine(
            $"""
            *********************************************************
            Added CMS Workers
            Database : {databaseProvider} - {string.Join(";", parts)}
            TaskTimingConfig: {taskTimingSeconds}
            *********************************************************
            """
        );

        return services;
    }
}
