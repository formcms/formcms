using FormCMS.Activities.Models;
using FormCMS.Core.Messaging;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.RelationDbDao;

namespace FormCMS.Activities.Workers;

public class ActivityEventHandler(
    IServiceScopeFactory scopeFactory,
    ActivitySettings settings,
    IStringMessageConsumer consumer,
    ILogger<ActivityEventHandler> logger
)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await consumer.Subscribe(
            Topics.CmsActivity,
            "ActivityEventHandler",
            async s =>
            {
                var message = ActivityMessageExtensions.ParseJson(s);
                if (settings.EventRecordActivities.Contains(message.Activity))
                {
                    logger.LogInformation("Got an activity message, {msg}", message);
                    using var scope = scopeFactory.CreateScope();
                    var dao = scope.ServiceProvider.GetRequiredService<IRelationDbDao>();
                    try
                    {
                        await HandleMessage(message, dao, ct);
                    }
                    catch (Exception e)
                    {
                        logger.LogError("Fail to handle message {msg}, err={err}", message, e.Message);
                    }
                }
            },
            ct
        );
    }

    private static async Task HandleMessage(ActivityMessage message, IRelationDbDao dao, CancellationToken ct)
    {
        if (message.Operation != Operations.Create && message.Operation != Operations.Delete) return;

        var count = new ActivityCount(message.EntityName, message.RecordId, message.Activity);
        var delta = message.Operation == Operations.Create ? 1 : -1;
        await dao.Increase(
            ActivityCounts.TableName,
            count.Condition(true),
            ActivityCounts.CountField,
            0,
            delta,
            ct);
    }
}