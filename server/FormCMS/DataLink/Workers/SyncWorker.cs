using System.Text.Json;
using FormCMS.Utils.ResultExt;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Messaging;
using FormCMS.Infrastructure.EventStreaming;

namespace FormCMS.DataLink.Workers;

public  abstract class SyncWorker(
    IServiceScopeFactory scopeFactory,
    IStringMessageConsumer consumer,
    ILogger<SyncWorker> logger,
    HashSet<string> entities
    ) : BackgroundService
{
    protected abstract string GetTaskName();
    protected abstract Task Upsert(IServiceScope scope, LoadedEntity entity, ContentTag tags, CancellationToken ct);
    protected abstract Task Delete(IServiceScope scope, string entityName, string id, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await consumer.Subscribe(CmsTopics.CmsCrud,
            GetTaskName() ,async s =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<RecordMessage>(s);
                if (message is null)
                {
                    logger.LogWarning("{task}:Could not deserialize message", GetTaskName());
                    return;
                }

                if (!entities.Contains(message.EntityName))
                {
                    logger.LogWarning("{task}:entity [{message.EntityName}] is not in Feed Dictionary, ignore the message",
                        GetTaskName(),
                        message.EntityName);
                }

                using var scope = scopeFactory.CreateScope();
                
                switch (message.Operation)
                {
                    case CmsOperations.Create:
                    case CmsOperations.Update:
                        await FetchSave(scope,message.EntityName, message.RecordId,ct);
                        break;
                    case CmsOperations.Delete:
                        await Delete(scope,message.EntityName, message.RecordId, ct);
                        break;
                    default:
                        logger.LogWarning("unknown operation {message.Operation}, ignore the message",
                            message.Operation);
                        break;
                }

                logger.LogInformation(
                    "consumed message successfully, entity={message.EntityName}, operation={message.Operation}, id = {message.Id}",
                    message.EntityName, message.Operation, message.RecordId);
            }
            catch (Exception e)
            {
                logger.LogError("Fail to handler message, err= {error}", e.Message);
            }

        }, ct);
    }
    
    private async Task FetchSave(IServiceScope scope, string entityName, string id, CancellationToken ct)
    {
        var entityService = scope.ServiceProvider.GetRequiredService<IEntityService>();
        var contentTagService = scope.ServiceProvider.GetRequiredService<IContentTagService>();
        
        var loadedEntity = await entityService.GetEntityAndValidateRecordId(entityName, int.Parse(id), ct).Ok();
        var tags = await contentTagService.GetContentTags(loadedEntity, [id], ct);
        if (tags.Length > 0)
        {
            await Upsert(scope,loadedEntity, tags.First(), ct);
        }
    }
}