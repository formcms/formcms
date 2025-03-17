using FormCMS.Cms.Builders;
using FormCMS.Infrastructure.FileStore;

namespace FormCMS.Course;

public  class Worker(string databaseProvider, string databaseConnectionString, 
    AzureBlobStoreOptions? azureBlobStoreOptions, 
    TaskTimingSeconds? taskTimingSeconds)
{
    public IHost Build()
    {
        var builder = Host.CreateApplicationBuilder();
        _ = databaseProvider switch
        {
            Constants.Sqlite => builder.Services.AddSqliteCmsWorker(databaseConnectionString,taskTimingSeconds),
            Constants.Postgres => builder.Services.AddPostgresCmsWorker(databaseConnectionString,taskTimingSeconds),
            Constants.SqlServer => builder.Services.AddSqlServerCmsWorker(databaseConnectionString,taskTimingSeconds),
            _ => throw new Exception("Database provider not found")
        };

        if (azureBlobStoreOptions == null) return builder.Build();
        builder.Services.AddSingleton(azureBlobStoreOptions);
        builder.Services.AddSingleton<IFileStore, AzureBlobStore>();

        return builder.Build();
    }
}