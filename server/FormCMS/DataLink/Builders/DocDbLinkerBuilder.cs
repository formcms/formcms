using FormCMS.DataLink.Types;
using FormCMS.DataLink.Workers;
using FormCMS.Infrastructure.DocumentDbDao;
using FormCMS.Infrastructure.EventStreaming;

namespace FormCMS.DataLink.Builders;

public static class DocDbLinkerBuilder
{
    public static IServiceCollection AddNatsMongoLink(
        IServiceCollection services,
        ApiLinks[] apiLinksArray
    )
    {

        var arr = string.Join(",", [..apiLinksArray]);
        Console.WriteLine(
            $"""
             *********************************************************
             Adding Nats Mongo Link 
             apiLinksArray = {arr}
             *********************************************************
             """);
        services.AddSingleton<IStringMessageConsumer, NatsMessageBus>();
        services.AddScoped<IDocumentDbDao, MongoDao>();


        services.AddSingleton(apiLinksArray);
        services.AddHostedService<SyncWorker>();
        services.AddHostedService<MigrateWorker>();
        return services;
    }
}