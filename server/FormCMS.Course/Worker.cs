using FormCMS.Cms.Builders;

namespace FormCMS.Course;

public static class Worker
{
    public static IHost? Build(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        if (builder.Configuration.GetValue<bool>("enable-worker") is not true)
        {
            return null;
        }

        var provider = builder.Configuration.GetValue<string>(Constants.DatabaseProvider) ??
                       throw new Exception("DatabaseProvider not found");
        var conn = builder.Configuration.GetConnectionString(provider) ??
                   throw new Exception($"Connection string {provider} not found");

        var taskTimingSeconds = new TaskTimingSeconds(
            builder.Configuration.GetValue<int>("QueryTimeout"),
            builder.Configuration.GetValue<int>("ExportDelay"),
            builder.Configuration.GetValue<int>("ImportDelay"),
            builder.Configuration.GetValue<int>("PublishDelay")
            );
        _ = provider switch
        {
            Constants.Sqlite => builder.Services.AddSqliteCmsWorker(conn,taskTimingSeconds),
            Constants.Postgres => builder.Services.AddPostgresCmsWorker(conn,taskTimingSeconds),
            Constants.SqlServer => builder.Services.AddSqlServerCmsWorker(conn,taskTimingSeconds),
            _ => throw new Exception("Database provider not found")
        };
        
        return builder.Build();
    }
}