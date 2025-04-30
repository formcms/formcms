using FormCMS.Cms.Builders;
using FormCMS.Infrastructure.FileStore;

namespace FormCMS.Course;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var databaseProvider = builder.Configuration.GetValue<string>(Constants.DatabaseProvider) ??
                               throw new Exception("DatabaseProvider not found");

        var databaseConnectionString = builder.Configuration.GetConnectionString(databaseProvider) ??
                                       throw new Exception($"Connection string {databaseProvider} not found");

        var redisConnectionString = builder.Configuration.GetConnectionString(Constants.Redis);
        var azureBlobStoreOptions = builder.Configuration
            .GetSection(nameof(AzureBlobStoreOptions)).Get<AzureBlobStoreOptions>();

        var taskTimingSeconds = builder.Configuration
            .GetSection(nameof(TaskTimingSeconds)).Get<TaskTimingSeconds>();

        var enableWorker = builder.Configuration.GetValue<bool>("enable-worker");
        var enableActivityBuffer = builder.Configuration.GetValue<bool>("EnableActivityBuffer");

        var webApp = await new WebApp(builder, databaseProvider, databaseConnectionString,enableActivityBuffer, redisConnectionString, azureBlobStoreOptions)
            .Build();

        var worker = enableWorker
            ? new Worker(databaseProvider, databaseConnectionString, azureBlobStoreOptions, taskTimingSeconds).Build()
            : null;

        await Task.WhenAll(
            webApp.RunAsync(),
            worker?.RunAsync() ?? Task.CompletedTask
        );
    }
}