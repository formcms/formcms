using System.Text.Json;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Messaging;
using FormCMS.Utils.RecordExt;

namespace FormCMS.Cms.Builders;

public record CmsCrudMessageProduceBuilderOptions(string[] Entities);

public class CmsCrudMessageProduceBuilder(ILogger<CmsCrudMessageProduceBuilder> logger, CmsCrudMessageProduceBuilderOptions options)
{
    public static IServiceCollection AddCrudMessageProducer(IServiceCollection services, string[] entities)
    {
        services.AddSingleton(new CmsCrudMessageProduceBuilderOptions(Entities:entities));
        services.AddSingleton<CmsCrudMessageProduceBuilder>();
        return services;
    }

    public WebApplication UseEventProducer(WebApplication app)
    {
        Print();
        RegisterHooks(app);
        return app;
    }
    
    private void Print()
    {
        var info = string.Join(",", options.Entities.Select(x => x.ToString()));
        logger.LogInformation(
            """
            *********************************************************
            Using Message Producer
            Produce message for these entities: {info}
            *********************************************************
            """,info); 
    }

    private void RegisterHooks(WebApplication app)
    {
        var registry = app.Services.GetRequiredService<HookRegistry>();
        var messageProducer = app.Services.GetRequiredService<IStringMessageProducer>();
        foreach (var entity in options.Entities)
        {
            registry.EntityPostAdd.RegisterAsync(entity, async parameter =>
            {
                await messageProducer.Produce(
                    CmsTopics.CmsCrud,
                    EncodeMessage(CmsOperations.Create, parameter.Name, parameter.Record.StrOrEmpty(parameter.Entity.PrimaryKey), parameter.Record));
                return parameter;
            });

            registry.EntityPostUpdate.RegisterAsync(entity, async parameter =>
            {
                await messageProducer.Produce(
                    CmsTopics.CmsCrud,
                    EncodeMessage(CmsOperations.Create, parameter.Name, parameter.Record.StrOrEmpty(parameter.Entity.PrimaryKey), parameter.Record)
                );
                return parameter;
            });
            registry.EntityPostDel.RegisterAsync(entity, async parameter =>
            {
                await messageProducer.Produce(
                    CmsTopics.CmsCrud,
                    EncodeMessage(CmsOperations.Create, parameter.Name, parameter.Record.StrOrEmpty(parameter.Entity.PrimaryKey), parameter.Record));
                return parameter;
            });
        }
    }

    private static string EncodeMessage(string operation, string entity, string id, Record data
    ) => JsonSerializer.Serialize(new RecordMessage(operation, entity, id, data));
}
    