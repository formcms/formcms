
namespace FormCMS.App;

public static class WorkerApp
{
    public static IHost? Build(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        if (builder.Configuration.GetValue<bool>(AppConstants.EnableHostApp) is not true)
        {
            return null;
        }
        
        Console.WriteLine($"""
                          ************************************************************
                          Start worker App
                          Nats: {AppConstants.Nats}
                          MongoCms: {AppConstants.MongoCms}
                          ************************************************************
                          """);
        builder.AddNatsClient(AppConstants.Nats);
        builder.AddMongoDBClient(AppConstants.MongoCms);
        return builder.Build();
    }
}