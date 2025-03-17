using FormCMS.Cms.Builders;
using FormCMS.Course;
using FormCMS.Infrastructure.FileStore;

var builder = WebApplication.CreateBuilder(args);

var databaseProvider = builder.Configuration.GetValue<string>(Constants.DatabaseProvider) ??
               throw new Exception("DatabaseProvider not found");

var databaseConnectionString = builder.Configuration.GetConnectionString(databaseProvider) ??
           throw new Exception($"Connection string {databaseProvider} not found");

var redisConnectionString = builder.Configuration.GetConnectionString(Constants.Redis);
var azureBlobStoreOptions = builder.Configuration.GetSection(nameof(AzureBlobStoreOptions)).Get<AzureBlobStoreOptions>();
var taskTimingSeconds = builder.Configuration.GetSection(nameof(TaskTimingSeconds)).Get<TaskTimingSeconds>();
var enableWorker = builder.Configuration.GetValue<bool>("enable-worker");

var webApp = await new WebApp(builder,databaseProvider,databaseConnectionString,redisConnectionString,azureBlobStoreOptions).Build();
var worker = enableWorker? new Worker(databaseProvider, databaseConnectionString,azureBlobStoreOptions,taskTimingSeconds).Build():null;
await Task.WhenAll(webApp.RunAsync(), worker?.RunAsync() ?? Task.CompletedTask);