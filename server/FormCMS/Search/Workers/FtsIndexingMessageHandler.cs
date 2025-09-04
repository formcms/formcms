using FormCMS.Core.Descriptors;
using FormCMS.DataLink.Workers;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.Fts;
using FormCMS.Search.Models;
using Humanizer;

namespace FormCMS.Search.Workers;

public record FtsIndexingSettings(HashSet<string> FtsEntities);
public class FtsIndexingMessageHandler(
    IServiceScopeFactory scopeFactory,
    IStringMessageConsumer consumer, 
    ILogger<SyncWorker> logger, 
    FtsIndexingSettings indexingSettings
    ) : 
    SyncWorker(scopeFactory, consumer, logger, indexingSettings.FtsEntities)
{
    protected override string GetTaskName()
    {
        return nameof(FtsIndexingMessageHandler);
    }

    protected override Task Upsert(IServiceScope scope,LoadedEntity entity, ContentTag tag, CancellationToken ct)
    {
        var record = new Dictionary<string, object>
        {
            [nameof(SearchDocument.EntityName).Camelize()] = entity.Name,
            [nameof(SearchDocument.RecordId).Camelize()] = tag.RecordId,
            [nameof(SearchDocument.Image).Camelize()] = tag.Image,
            [nameof(SearchDocument.Url).Camelize()] = tag.Url,
            [nameof(SearchDocument.PublishedAt).Camelize()] = tag.PublishedAt ?? DateTime.Now,
            [nameof(SearchDocument.Title).Camelize()] = tag.Title,
            [nameof(SearchDocument.Subtitle).Camelize()] = tag.Subtitle,
            [nameof(SearchDocument.Content).Camelize()] = tag.Content,
        };
        return scope.ServiceProvider.GetRequiredService<IFullTextSearch>()
            .IndexAsync(SearchConstant.TableName, SearchDocumentHelper.UniqKeyFields, record);
    }

    protected override Task Delete(IServiceScope scope,string entityName,  string recordId, CancellationToken ct)
    {
        var record = new Dictionary<string, object>
        {
            [nameof(SearchDocument.EntityName).Camelize()] = entityName,
            [nameof(SearchDocument.RecordId).Camelize()] = recordId
        };
        return  scope.ServiceProvider.GetRequiredService<IFullTextSearch>().RemoveAsync(SearchConstant.TableName, record);
    }
}