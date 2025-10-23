using System.Text.Json;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Messaging;
using FormCMS.Core.Tasks;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Cms.Workers;
public record EmitMessageWorkerOptions(int DelaySeconds);

public class EmitMessageHandler(
    EmitMessageWorkerOptions options,
    IServiceScopeFactory scopeFactory,
    IStringMessageProducer producer,
    ILogger<EmitMessageHandler> logger
) : TaskWorker(serviceScopeFactory: scopeFactory, logger: logger,delaySeconds: options.DelaySeconds)
{
    protected override async Task DoTask(
        IServiceScope serviceScope, IRelationDbDao sourceDao,
        SystemTask task, CancellationToken ct)
    {
        var setting = JsonSerializer.Deserialize<EmitMessageSetting>(task.TaskSettings)!;
        var entityName = setting.EntityName;
        
        var record = await sourceDao.Single(SchemaHelper.ByNameAndType(SchemaType.Entity, [entityName], null),ct);
        var entity = SchemaHelper.RecordToSchema(record).Ok().Settings.Entity!;
        await sourceDao.HandlePageData(entity.TableName,entity.PrimaryKey,[entity.PrimaryKey], async records =>
        {
            foreach (var record in records)
            {
                var id = record[entity.PrimaryKey].ToString()!;
                var msg = new RecordMessage(CmsOperations.Update, entityName, id, record);
                var payload = JsonSerializer.Serialize(msg);
                await producer.Produce(CmsTopics.CmsCrud, payload);
            }
        },ct);
    }
    
    protected override TaskType GetTaskType() => TaskType.EmitMessage;
    
    private static string EncodeMessage(string operation, string entity, string id, Record data
    ) => JsonSerializer.Serialize(new RecordMessage(operation, entity, id, data));
}