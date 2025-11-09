using FormCMS.Core.Messaging;
using FormCMS.Engagements.Models;
using FormCMS.Engagements.Services;
using FormCMS.Infrastructure.EventStreaming;

namespace FormCMS.Engagements.Workers;

public class ActivityEventHandler(
    IServiceScopeFactory scopeFactory,
    EngagementSettings settings,
    IStringMessageConsumer consumer,
    ILogger<ActivityEventHandler> logger
)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await consumer.Subscribe(
            CmsTopics.CmsActivity,
            "ActivityEventHandler",
            async s =>
            {
                logger.LogInformation("Got an activity message, {msg}", s);
                var message = ActivityMessageExtensions.ParseJson(s);
                if (settings.EventRecordActivities.Contains(message.Activity))
                {
                    try
                    {
                        await HandleMessage(message,  ct);
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

    private  async Task HandleMessage(ActivityMessage message,  CancellationToken ct)
    {
        if (message.Operation != CmsOperations.Create && message.Operation != CmsOperations.Delete) return;
        using var scope = scopeFactory.CreateScope();
        var activityCollectService = scope.ServiceProvider.GetRequiredService<IEngagementCollectService>();
        await activityCollectService.MarkEngaged(message.UserId,message.EntityName,message.RecordId,[message.Activity],ct);
    }
}