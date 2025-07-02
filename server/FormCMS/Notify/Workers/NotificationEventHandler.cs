using FormCMS.Core.Messaging;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Notify.Models;

namespace FormCMS.Notify.Workers;

public class NotificationEventHandler(
    IServiceScopeFactory scopeFactory,
    IStringMessageConsumer consumer,
    NotifySettings  notifySettings,
    ILogger<NotificationEventHandler> logger
):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await consumer.Subscribe(
            CmsTopics.CmsActivity,
            "NotificationEventHandler",
            async s =>
            {
                var message = ActivityMessageExtensions.ParseJson(s);
                if (notifySettings.NotifyActivities.Contains(message.Activity))
                {
                    logger.LogInformation("Got an notification message, {msg}", message);
                    try
                    {
                        var notification = new Notification(
                            UserId: message.TargetUserId,
                            SenderId: message.UserId,
                            ActionType: message.Activity,
                            MessageType:message.EntityName,
                            Message: message.Message,
                            Url:message.Url
                        );

                        using var scope = scopeFactory.CreateScope();
                        await scope.ServiceProvider.GetRequiredService<KateQueryExecutor>()
                            .Exec(notification.Insert(),false,ct);
                        
                        
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
}